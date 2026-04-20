using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.FarmaciaPopular;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Orquestra as 3 fases (+ estorno) do Farmácia Popular. Toda lógica de negócio
/// fica aqui — o SoapClient só monta/parseia XML. Persistência em transações
/// atômicas para manter consistência entre XMLs salvos e Status.
/// </summary>
public class FarmaciaPopularService : IFarmaciaPopularService
{
    private readonly AppDbContext _db;
    private readonly IFarmaciaPopularSoapClient _soap;

    public FarmaciaPopularService(AppDbContext db, IFarmaciaPopularSoapClient soap)
    {
        _db = db;
        _soap = soap;
    }

    public async Task<SolicitacaoRetornoDto> SolicitarAsync(long vendaId, CancellationToken ct = default)
    {
        var fp = await CarregarFpCompletoAsync(vendaId, ct);

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor, ct);
        var caminhoExe = configs.GetValueOrDefault("pbm.fp.caminho.gbasmsb", "");
        var cred = await MontarCredenciaisAsync(configs, fp.Venda!.ColaboradorId, ct);

        // 1) gbasmsb → dnaEstacao
        string dna;
        try
        {
            dna = await GbasmsbRunner.ExecutarSolicitacaoAsync(
                caminhoExe, fp.CpfPaciente, fp.CnpjEstabelecimento, fp.CrmMedico, fp.UfCrm, fp.DtEmissaoReceita, ct);
        }
        catch (Exception ex)
        {
            fp.Status = StatusFarmaciaPopular.Erro;
            fp.MensagemRetornoAtual = ex.Message;
            await _db.SaveChangesAsync(ct);
            return new SolicitacaoRetornoDto { Sucesso = false, CodigoRetorno = "ERRGBAS", MensagemRetorno = ex.Message };
        }
        fp.DnaEstacao = dna;

        // 2) Monta request
        var req = new SolicitacaoRequest
        {
            CoSolicitacaoFarmacia = fp.CoSolicitacaoFarmacia,
            NuCnpj = fp.CnpjEstabelecimento,
            NuCpf = fp.CpfPaciente,
            NuCrm = fp.CrmMedico,
            SgUfCrm = fp.UfCrm,
            DtEmissaoReceita = fp.DtEmissaoReceita,
            DnaEstacao = dna,
            Medicamentos = fp.Itens.Select(i => new MedicamentoDto
            {
                CoCodigoBarra = i.CodigoBarraEAN,
                QtSolicitada = i.QtSolicitada,
                VlPrecoVenda = i.VlPrecoVenda,
                QtPrescrita = i.QtPrescrita
            }).ToList()
        };

        // 3) SOAP
        var ret = await _soap.ExecutarSolicitacaoAsync(req, cred, ct);

        // 4) Persistir
        fp.Fase1RequestXml = ret.RequestXml;
        fp.Fase1ResponseXml = ret.ResponseXml;
        fp.Fase1DataHora = DateTime.UtcNow;
        fp.CodigoRetornoAtual = ret.CodigoRetorno;
        fp.MensagemRetornoAtual = ret.MensagemRetorno;
        fp.FaseAtual = FaseFarmaciaPopular.Solicitacao;

        if (ret.Sucesso)
        {
            fp.NuAutorizacao = ret.NuAutorizacao;
            fp.NoPaciente = ret.NoPaciente;
            fp.Status = ret.CodigoRetorno.StartsWith("01S")
                ? StatusFarmaciaPopular.PreAutorizadaParcial
                : StatusFarmaciaPopular.PreAutorizada;

            // Aplica retorno por item (match por EAN)
            foreach (var rItem in ret.Itens)
            {
                var local = fp.Itens.FirstOrDefault(i => i.CodigoBarraEAN == rItem.CodigoBarraEAN);
                if (local == null) continue;
                local.QtAutorizada = rItem.QtAutorizada;
                local.VlPrecoSubsidiadoMS = rItem.VlPrecoSubsidiadoMS;
                local.VlPrecoSubsidiadoPaciente = rItem.VlPrecoSubsidiadoPaciente;
                local.CodigoRetornoItem = rItem.CodigoRetornoItem;
                local.MensagemRetornoItem = rItem.MensagemRetornoItem;
                local.InAutorizacaoMedicamento = rItem.InAutorizacaoMedicamento;
            }
        }
        else
        {
            fp.Status = StatusFarmaciaPopular.Rejeitada;
        }

        await _db.SaveChangesAsync(ct);
        return ret;
    }

    public async Task<ConfirmacaoRetornoDto> ConfirmarAsync(long vendaId, string nuCupomFiscal, CancellationToken ct = default)
    {
        var fp = await CarregarFpCompletoAsync(vendaId, ct);
        if (string.IsNullOrWhiteSpace(fp.NuAutorizacao))
            return new ConfirmacaoRetornoDto { Sucesso = false, CodigoRetorno = "NOAUT", MensagemRetorno = "NuAutorizacao ausente — rode Fase 1 primeiro." };

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor, ct);
        var cred = await MontarCredenciaisAsync(configs, fp.Venda!.ColaboradorId, ct);

        var ret = await _soap.ConfirmarSolicitacaoAsync(fp.CoSolicitacaoFarmacia, fp.NuAutorizacao, nuCupomFiscal, cred, ct);

        fp.Fase2RequestXml = ret.RequestXml;
        fp.Fase2ResponseXml = ret.ResponseXml;
        fp.Fase2DataHora = DateTime.UtcNow;
        fp.NuCupomFiscal = nuCupomFiscal;
        fp.CodigoRetornoAtual = ret.CodigoRetorno;
        fp.MensagemRetornoAtual = ret.MensagemRetorno;
        fp.FaseAtual = FaseFarmaciaPopular.Confirmacao;

        if (ret.Sucesso)
            fp.Status = ret.CodigoRetorno.StartsWith("01A")
                ? StatusFarmaciaPopular.ConfirmadaParcial
                : StatusFarmaciaPopular.Confirmada;
        else
            fp.Status = StatusFarmaciaPopular.Rejeitada;

        await _db.SaveChangesAsync(ct);
        return ret;
    }

    public async Task<ConfirmacaoRetornoDto> ReceberAsync(long vendaId, CancellationToken ct = default)
    {
        var fp = await CarregarFpCompletoAsync(vendaId, ct);
        if (string.IsNullOrWhiteSpace(fp.NuAutorizacao))
            return new ConfirmacaoRetornoDto { Sucesso = false, CodigoRetorno = "NOAUT", MensagemRetorno = "NuAutorizacao ausente." };

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor, ct);
        var cred = await MontarCredenciaisAsync(configs, fp.Venda!.ColaboradorId, ct);

        var ret = await _soap.ReceberMedicamentoAsync(fp.CoSolicitacaoFarmacia, fp.NuAutorizacao, cred, ct);

        fp.Fase3RequestXml = ret.RequestXml;
        fp.Fase3ResponseXml = ret.ResponseXml;
        fp.Fase3DataHora = DateTime.UtcNow;
        fp.CodigoRetornoAtual = ret.CodigoRetorno;
        fp.MensagemRetornoAtual = ret.MensagemRetorno;

        if (ret.Sucesso)
        {
            if (ret.CodigoRetorno.StartsWith("00RV"))
            {
                fp.Status = StatusFarmaciaPopular.Efetivada;
                fp.FaseAtual = FaseFarmaciaPopular.Concluida;
                foreach (var item in fp.Itens)
                    item.QtDispensada = item.QtAutorizada;
            }
            else
            {
                fp.Status = StatusFarmaciaPopular.Recebida;
                fp.FaseAtual = FaseFarmaciaPopular.Recebimento;
            }
        }
        else
        {
            fp.Status = StatusFarmaciaPopular.Rejeitada;
        }

        await _db.SaveChangesAsync(ct);
        return ret;
    }

    public async Task<ConfirmacaoRetornoDto> EstornarAsync(long vendaId, string motivo, CancellationToken ct = default)
    {
        var fp = await CarregarFpCompletoAsync(vendaId, ct);
        if (string.IsNullOrWhiteSpace(fp.NuAutorizacao))
            return new ConfirmacaoRetornoDto { Sucesso = false, CodigoRetorno = "NOAUT", MensagemRetorno = "NuAutorizacao ausente." };

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor, ct);
        var cred = await MontarCredenciaisAsync(configs, fp.Venda!.ColaboradorId, ct);

        var ret = await _soap.EstornarAsync(fp.CoSolicitacaoFarmacia, fp.NuAutorizacao, motivo, cred, ct);

        fp.EstornoRequestXml = ret.RequestXml;
        fp.EstornoResponseXml = ret.ResponseXml;
        fp.EstornoDataHora = DateTime.UtcNow;
        fp.CodigoRetornoAtual = ret.CodigoRetorno;
        fp.MensagemRetornoAtual = ret.MensagemRetorno;

        if (ret.Sucesso)
        {
            fp.Status = ret.CodigoRetorno.StartsWith("01E")
                ? StatusFarmaciaPopular.EstornadaParcial
                : StatusFarmaciaPopular.Estornada;
            fp.FaseAtual = FaseFarmaciaPopular.Estornada;
            fp.EstornoPendente = false;
            foreach (var item in fp.Itens)
                item.QtEstornada = item.QtDispensada ?? item.QtAutorizada;
        }
        else
        {
            fp.EstornoPendente = true;
        }

        await _db.SaveChangesAsync(ct);
        return ret;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private async Task<VendaFarmaciaPopular> CarregarFpCompletoAsync(long vendaId, CancellationToken ct)
    {
        var fp = await _db.Set<VendaFarmaciaPopular>()
            .Include(x => x.Venda)
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.VendaId == vendaId, ct)
            ?? throw new KeyNotFoundException($"VendaFarmaciaPopular da venda {vendaId} não encontrada.");
        return fp;
    }

    private async Task<CredenciaisFp> MontarCredenciaisAsync(Dictionary<string, string> configs, long? colaboradorId, CancellationToken ct)
    {
        var usuarioFarm = configs.GetValueOrDefault("pbm.fp.usuario.farmacia", "");
        var senhaFarm = configs.GetValueOrDefault("pbm.fp.senha.farmacia", "");

        var usuarioVend = "";
        var senhaVend = "";
        if (colaboradorId.HasValue)
        {
            var col = await _db.Set<Colaborador>()
                .Include(c => c.Pessoa)
                .FirstOrDefaultAsync(c => c.Id == colaboradorId.Value, ct);
            if (col != null)
            {
                usuarioVend = new string((col.Pessoa.CpfCnpj ?? "").Where(char.IsDigit).ToArray());
                senhaVend = CriptografiaHelper.Decrypt(col.SenhaFarmaciaPopularCripto) ?? "";
            }
        }

        if (string.IsNullOrEmpty(usuarioFarm) || string.IsNullOrEmpty(senhaFarm))
            Log.Warning("Credenciais FP da farmácia ausentes em Configurações (pbm.fp.usuario.farmacia/senha.farmacia).");
        if (string.IsNullOrEmpty(senhaVend))
            Log.Warning("Senha FP do colaborador {Id} ausente — venda FP vai falhar no DATASUS.", colaboradorId);

        return new CredenciaisFp
        {
            UsuarioFarmacia = usuarioFarm,
            SenhaFarmacia = senhaFarm,
            UsuarioVendedor = usuarioVend,
            SenhaVendedor = senhaVend
        };
    }
}
