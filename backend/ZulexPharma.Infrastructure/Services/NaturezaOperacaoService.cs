using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Nfe;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class NaturezaOperacaoService : INaturezaOperacaoService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Natureza de Operação";
    private const string ENTIDADE = "NaturezaOperacao";

    public NaturezaOperacaoService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<NaturezaOperacaoListDto>> ListarAsync()
    {
        try
        {
            return await _db.NaturezasOperacao.OrderBy(n => n.Descricao)
                .Select(n => new NaturezaOperacaoListDto
                {
                    Id = n.Id, Codigo = n.Codigo, Descricao = n.Descricao,
                    TipoNf = n.TipoNf, FinalidadeNfe = n.FinalidadeNfe,
                    MovimentaEstoque = n.MovimentaEstoque, Ativo = n.Ativo, CriadoEm = n.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em NaturezaOperacaoService.ListarAsync"); throw; }
    }

    public async Task<NaturezaOperacaoDetalheDto> ObterAsync(long id)
    {
        var nat = await _db.NaturezasOperacao
            .Include(n => n.Regras)
            .FirstOrDefaultAsync(n => n.Id == id)
            ?? throw new KeyNotFoundException($"Natureza de operação {id} não encontrada.");

        return MapToDetalhe(nat);
    }

    public async Task<NaturezaOperacaoListDto> CriarAsync(NaturezaOperacaoFormDto dto)
    {
        try
        {
            Validar(dto);
            var nat = new NaturezaOperacao();
            MapFromDto(nat, dto);
            MapearRegras(nat, dto.Regras);
            _db.NaturezasOperacao.Add(nat);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, nat.Id, novo: ParaDict(nat));
            return new NaturezaOperacaoListDto
            {
                Id = nat.Id, Codigo = nat.Codigo, Descricao = nat.Descricao,
                TipoNf = nat.TipoNf, FinalidadeNfe = nat.FinalidadeNfe,
                MovimentaEstoque = nat.MovimentaEstoque, Ativo = nat.Ativo, CriadoEm = nat.CriadoEm
            };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em NaturezaOperacaoService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, NaturezaOperacaoFormDto dto)
    {
        try
        {
            Validar(dto);
            var nat = await _db.NaturezasOperacao
                .Include(n => n.Regras)
                .FirstOrDefaultAsync(n => n.Id == id)
                ?? throw new KeyNotFoundException($"Natureza de operação {id} não encontrada.");

            var anterior = ParaDict(nat);
            MapFromDto(nat, dto);

            // Recria regras (replace strategy)
            _db.Set<NaturezaOperacaoRegra>().RemoveRange(nat.Regras);
            nat.Regras.Clear();
            MapearRegras(nat, dto.Regras);

            await _db.SaveChangesAsync();
            var novo = ParaDict(nat);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em NaturezaOperacaoService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var nat = await _db.NaturezasOperacao
                .Include(n => n.Regras)
                .FirstOrDefaultAsync(n => n.Id == id)
                ?? throw new KeyNotFoundException($"Natureza de operação {id} não encontrada.");
            var dados = ParaDict(nat);
            _db.Set<NaturezaOperacaoRegra>().RemoveRange(nat.Regras);
            _db.NaturezasOperacao.Remove(nat);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.NaturezasOperacao.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em NaturezaOperacaoService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static void MapFromDto(NaturezaOperacao nat, NaturezaOperacaoFormDto dto)
    {
        nat.Descricao = dto.Descricao.Trim().ToUpper();
        nat.TipoNf = dto.TipoNf;
        nat.FinalidadeNfe = dto.FinalidadeNfe;
        nat.IdentificadorDestino = dto.IdentificadorDestino;
        nat.RelacionarDocumentoFiscal = dto.RelacionarDocumentoFiscal;
        nat.UtilizarPrecoCusto = dto.UtilizarPrecoCusto;
        nat.ReajustarCustoMedio = dto.ReajustarCustoMedio;
        nat.GeraFinanceiro = dto.GeraFinanceiro;
        nat.MovimentaEstoque = dto.MovimentaEstoque;
        nat.TipoMovimentoEstoque = dto.TipoMovimentoEstoque;
        nat.CstPisPadrao = dto.CstPisPadrao;
        nat.CstCofinsPadrao = dto.CstCofinsPadrao;
        nat.CstIpiPadrao = dto.CstIpiPadrao;
        nat.EnquadramentoIpiPadrao = dto.EnquadramentoIpiPadrao;
        nat.IndicadorPresenca = dto.IndicadorPresenca;
        nat.IndicadorFinalidade = dto.IndicadorFinalidade;
        nat.Observacao = dto.Observacao;
        nat.Ativo = dto.Ativo;
    }

    private static void MapearRegras(NaturezaOperacao nat, List<NaturezaOperacaoRegraDto> regras)
    {
        foreach (var r in regras)
        {
            nat.Regras.Add(new NaturezaOperacaoRegra
            {
                CenarioTributario = r.CenarioTributario,
                CfopInterno = r.CfopInterno?.Trim(),
                CfopInterestadual = r.CfopInterestadual?.Trim(),
                CstIcmsInterno = r.CstIcmsInterno?.Trim(),
                CstIcmsInterestadual = r.CstIcmsInterestadual?.Trim(),
                CodigoBeneficioInterno = r.CodigoBeneficioInterno?.Trim(),
                CodigoBeneficioInterestadual = r.CodigoBeneficioInterestadual?.Trim()
            });
        }
    }

    private static NaturezaOperacaoDetalheDto MapToDetalhe(NaturezaOperacao nat) => new()
    {
        Id = nat.Id, Codigo = nat.Codigo, Descricao = nat.Descricao,
        TipoNf = nat.TipoNf, FinalidadeNfe = nat.FinalidadeNfe,
        IdentificadorDestino = nat.IdentificadorDestino,
        RelacionarDocumentoFiscal = nat.RelacionarDocumentoFiscal,
        UtilizarPrecoCusto = nat.UtilizarPrecoCusto,
        ReajustarCustoMedio = nat.ReajustarCustoMedio,
        GeraFinanceiro = nat.GeraFinanceiro,
        MovimentaEstoque = nat.MovimentaEstoque,
        TipoMovimentoEstoque = nat.TipoMovimentoEstoque,
        CstPisPadrao = nat.CstPisPadrao,
        CstCofinsPadrao = nat.CstCofinsPadrao,
        CstIpiPadrao = nat.CstIpiPadrao,
        EnquadramentoIpiPadrao = nat.EnquadramentoIpiPadrao,
        IndicadorPresenca = nat.IndicadorPresenca,
        IndicadorFinalidade = nat.IndicadorFinalidade,
        Observacao = nat.Observacao,
        Ativo = nat.Ativo, CriadoEm = nat.CriadoEm,
        Regras = nat.Regras.OrderBy(r => r.CenarioTributario).Select(r => new NaturezaOperacaoRegraDto
        {
            Id = r.Id,
            CenarioTributario = r.CenarioTributario,
            CfopInterno = r.CfopInterno,
            CfopInterestadual = r.CfopInterestadual,
            CstIcmsInterno = r.CstIcmsInterno,
            CstIcmsInterestadual = r.CstIcmsInterestadual,
            CodigoBeneficioInterno = r.CodigoBeneficioInterno,
            CodigoBeneficioInterestadual = r.CodigoBeneficioInterestadual
        }).ToList()
    };

    private static void Validar(NaturezaOperacaoFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descricao)) throw new ArgumentException("Descrição é obrigatória.");
    }

    private static Dictionary<string, string?> ParaDict(NaturezaOperacao n) => new()
    {
        ["Descricao"] = n.Descricao,
        ["TipoNf"] = n.TipoNf == 0 ? "Entrada" : "Saída",
        ["FinalidadeNfe"] = n.FinalidadeNfe.ToString(),
        ["RelacionarDocFiscal"] = n.RelacionarDocumentoFiscal ? "Sim" : "Não",
        ["UtilizarPrecoCusto"] = n.UtilizarPrecoCusto ? "Sim" : "Não",
        ["ReajustarCustoMedio"] = n.ReajustarCustoMedio ? "Sim" : "Não",
        ["GeraFinanceiro"] = n.GeraFinanceiro ? "Sim" : "Não",
        ["MovimentaEstoque"] = n.MovimentaEstoque ? "Sim" : "Não",
        ["Ativo"] = n.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
