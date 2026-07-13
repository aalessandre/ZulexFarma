using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.ContasPagar;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ContaPagarService : IContaPagarService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Contas a Pagar";
    private const string ENTIDADE = "ContaPagar";

    public ContaPagarService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<ContaPagarListDto>> ListarAsync()
    {
        try
        {
            var hoje = DataHoraHelper.Agora().Date;
            return await _db.ContasPagar
                .Include(c => c.Pessoa)
                .Include(c => c.PlanoConta)
                .Include(c => c.Filial)
                .OrderByDescending(c => c.DataVencimento)
                .Select(c => new ContaPagarListDto
                {
                    Id = c.Id,
                    Descricao = c.Descricao,
                    PessoaId = c.PessoaId,
                    PessoaNome = c.Pessoa != null ? c.Pessoa.Nome : null,
                    PlanoContaId = c.PlanoContaId,
                    PlanoContaDescricao = c.PlanoConta != null ? c.PlanoConta.Descricao : null,
                    FilialId = c.FilialId,
                    FilialNome = c.Filial != null ? c.Filial.NomeFilial : null,
                    CompraId = c.CompraId,
                    Valor = c.Valor,
                    Desconto = c.Desconto,
                    Juros = c.Juros,
                    Multa = c.Multa,
                    ValorFinal = c.ValorFinal,
                    DataEmissao = c.DataEmissao,
                    DataVencimento = c.DataVencimento,
                    DataPagamento = c.DataPagamento,
                    NrDocumento = c.NrDocumento,
                    NrNotaFiscal = c.NrNotaFiscal,
                    Observacao = c.Observacao,
                    Status = c.Status,
                    StatusDescricao = StatusParaTexto(c.Status),
                    Vencido = c.Status == StatusConta.Aberto && c.DataVencimento < hoje,
                    RecorrenciaGrupo = c.RecorrenciaGrupo,
                    RecorrenciaParcela = c.RecorrenciaParcela,
                    Ativo = c.Ativo,
                    CriadoEm = c.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ContaPagarService.ListarAsync"); throw; }
    }

    public async Task<ContaPagarListDto> CriarAsync(ContaPagarFormDto dto)
    {
        try
        {
            Validar(dto);
            var cp = Mapear(dto);
            _db.ContasPagar.Add(cp);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, cp.Id, novo: ParaDict(cp));
            return ToListDto(cp);
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em ContaPagarService.CriarAsync"); throw; }
    }

    public async Task<List<ContaPagarListDto>> CriarRecorrenteAsync(ContaPagarRecorrenteDto dto)
    {
        try
        {
            if (dto.QuantidadeParcelas < 2 || dto.QuantidadeParcelas > 120)
                throw new ArgumentException("Quantidade de parcelas deve ser entre 2 e 120.");
            if (!Enum.IsDefined(dto.Periodicidade))
                throw new ArgumentException("Periodicidade invalida.");

            // Periodicidades em MESES repetem o DIA do 1o vencimento; as em DIAS somam dias sobre ele.
            var porMes = dto.Periodicidade is Periodicidade.Mensal or Periodicidade.Trimestral
                or Periodicidade.Semestral or Periodicidade.Anual or Periodicidade.PersonalizadoMeses;
            if ((dto.Periodicidade is Periodicidade.PersonalizadoDias or Periodicidade.PersonalizadoMeses)
                && (dto.IntervaloPersonalizado < 1 || dto.IntervaloPersonalizado > 365))
                throw new ArgumentException("Intervalo da periodicidade personalizada deve ser entre 1 e 365.");
            Validar(dto.Modelo);

            var passoMeses = dto.Periodicidade switch
            {
                Periodicidade.Mensal => 1,
                Periodicidade.Trimestral => 3,
                Periodicidade.Semestral => 6,
                Periodicidade.Anual => 12,
                Periodicidade.PersonalizadoMeses => dto.IntervaloPersonalizado,
                _ => 0
            };
            var passoDias = dto.Periodicidade switch
            {
                Periodicidade.Semanal => 7,
                Periodicidade.Quinzenal => 14,
                Periodicidade.PersonalizadoDias => dto.IntervaloPersonalizado,
                _ => 0
            };

            var grupo = Guid.NewGuid();
            var resultado = new List<ContaPagarListDto>();

            for (int i = 0; i < dto.QuantidadeParcelas; i++)
            {
                DateTime venc;
                if (porMes)
                {
                    // O usuario escolhe o dia pela data do 1o vencimento: a parcela i=0 cai EXATAMENTE
                    // nessa data (nunca retroativa) e as seguintes repetem o mesmo dia (clampado ao
                    // ultimo dia dos meses mais curtos).
                    var m = new DateTime(dto.Modelo.DataVencimento.Year, dto.Modelo.DataVencimento.Month, 1)
                        .AddMonths(passoMeses * i);
                    var dia = Math.Min(dto.Modelo.DataVencimento.Day, DateTime.DaysInMonth(m.Year, m.Month));
                    venc = new DateTime(m.Year, m.Month, dia);
                }
                else
                {
                    venc = dto.Modelo.DataVencimento.AddDays(passoDias * i);
                }

                var form = new ContaPagarFormDto
                {
                    Descricao = dto.Modelo.Descricao,
                    PessoaId = dto.Modelo.PessoaId,
                    PlanoContaId = dto.Modelo.PlanoContaId,
                    FilialId = dto.Modelo.FilialId,
                    Valor = dto.Modelo.Valor,
                    Desconto = 0,
                    Juros = 0,
                    Multa = 0,
                    DataEmissao = dto.Modelo.DataEmissao,
                    DataVencimento = venc,
                    NrDocumento = dto.Modelo.NrDocumento,
                    Observacao = dto.Modelo.Observacao,
                    Status = StatusConta.Aberto,
                    Ativo = true
                };

                var cp = Mapear(form);
                cp.RecorrenciaGrupo = grupo;
                cp.RecorrenciaParcela = $"{i + 1}/{dto.QuantidadeParcelas}";
                _db.ContasPagar.Add(cp);
            }

            await _db.SaveChangesAsync();

            var criados = await _db.ContasPagar
                .Where(c => c.RecorrenciaGrupo == grupo)
                .OrderBy(c => c.DataVencimento)
                .ToListAsync();

            foreach (var cp in criados)
            {
                await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, cp.Id, novo: ParaDict(cp));
                resultado.Add(ToListDto(cp));
            }

            return resultado;
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em ContaPagarService.CriarRecorrenteAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, ContaPagarFormDto dto)
    {
        try
        {
            Validar(dto);
            var cp = await _db.ContasPagar.FindAsync(id)
                ?? throw new KeyNotFoundException($"Conta a pagar {id} não encontrada.");
            var anterior = ParaDict(cp);

            cp.Descricao = dto.Descricao.Trim().ToUpper();
            cp.PessoaId = dto.PessoaId;
            cp.PlanoContaId = dto.PlanoContaId;
            cp.FilialId = dto.FilialId;
            cp.CompraId = dto.CompraId;
            cp.Valor = dto.Valor;
            cp.Desconto = dto.Desconto;
            cp.Juros = dto.Juros;
            cp.Multa = dto.Multa;
            cp.ValorFinal = dto.Valor - dto.Desconto + dto.Juros + dto.Multa;
            cp.DataEmissao = dto.DataEmissao;
            cp.DataVencimento = dto.DataVencimento;
            cp.DataPagamento = dto.DataPagamento;
            cp.NrDocumento = dto.NrDocumento?.Trim();
            cp.NrNotaFiscal = dto.NrNotaFiscal?.Trim();
            cp.Observacao = dto.Observacao?.Trim();
            cp.Status = dto.Status;
            cp.Ativo = dto.Ativo;
            await _db.SaveChangesAsync();

            var novo = ParaDict(cp);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em ContaPagarService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var cp = await _db.ContasPagar.FindAsync(id)
                ?? throw new KeyNotFoundException($"Conta a pagar {id} não encontrada.");
            if (cp.Status == StatusConta.Pago)
                throw new ArgumentException("Não é possível excluir uma conta já paga.");
            var dados = ParaDict(cp);
            _db.ContasPagar.Remove(cp);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.ContasPagar.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em ContaPagarService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    // ── Helpers ─────────────────────────────────────────────────────
    private static void Validar(ContaPagarFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descricao)) throw new ArgumentException("Descrição é obrigatória.");
        if (dto.Valor <= 0) throw new ArgumentException("Valor deve ser maior que zero.");
        if (dto.FilialId <= 0) throw new ArgumentException("Filial é obrigatória.");
    }

    private static ContaPagar Mapear(ContaPagarFormDto dto) => new()
    {
        Descricao = dto.Descricao.Trim().ToUpper(),
        PessoaId = dto.PessoaId,
        PlanoContaId = dto.PlanoContaId,
        FilialId = dto.FilialId,
        CompraId = dto.CompraId,
        Valor = dto.Valor,
        Desconto = dto.Desconto,
        Juros = dto.Juros,
        Multa = dto.Multa,
        ValorFinal = dto.Valor - dto.Desconto + dto.Juros + dto.Multa,
        DataEmissao = dto.DataEmissao,
        DataVencimento = dto.DataVencimento,
        DataPagamento = dto.DataPagamento,
        NrDocumento = dto.NrDocumento?.Trim(),
        NrNotaFiscal = dto.NrNotaFiscal?.Trim(),
        Observacao = dto.Observacao?.Trim(),
        Status = dto.Status,
        Ativo = dto.Ativo
    };

    private static ContaPagarListDto ToListDto(ContaPagar c) => new()
    {
        Id = c.Id,
        Descricao = c.Descricao,
        PessoaId = c.PessoaId,
        PlanoContaId = c.PlanoContaId,
        FilialId = c.FilialId,
        CompraId = c.CompraId,
        Valor = c.Valor,
        Desconto = c.Desconto,
        Juros = c.Juros,
        Multa = c.Multa,
        ValorFinal = c.ValorFinal,
        DataEmissao = c.DataEmissao,
        DataVencimento = c.DataVencimento,
        DataPagamento = c.DataPagamento,
        NrDocumento = c.NrDocumento,
        NrNotaFiscal = c.NrNotaFiscal,
        Observacao = c.Observacao,
        Status = c.Status,
        StatusDescricao = StatusParaTexto(c.Status),
        Vencido = c.Status == StatusConta.Aberto && c.DataVencimento < DataHoraHelper.Agora().Date,
        RecorrenciaGrupo = c.RecorrenciaGrupo,
        RecorrenciaParcela = c.RecorrenciaParcela,
        Ativo = c.Ativo,
        CriadoEm = c.CriadoEm
    };

    private static string StatusParaTexto(StatusConta s) => s switch
    {
        StatusConta.Aberto => "Aberto",
        StatusConta.Pago => "Pago",
        StatusConta.Cancelado => "Cancelado",
        _ => ""
    };

    private static Dictionary<string, string?> ParaDict(ContaPagar c) => new()
    {
        ["Descricao"] = c.Descricao,
        ["PessoaId"] = c.PessoaId?.ToString(),
        ["PlanoContaId"] = c.PlanoContaId?.ToString(),
        ["FilialId"] = c.FilialId.ToString(),
        ["Valor"] = c.Valor.ToString("N2"),
        ["Desconto"] = c.Desconto.ToString("N2"),
        ["Juros"] = c.Juros.ToString("N2"),
        ["Multa"] = c.Multa.ToString("N2"),
        ["ValorFinal"] = c.ValorFinal.ToString("N2"),
        ["DataEmissao"] = c.DataEmissao.ToString("dd/MM/yyyy"),
        ["DataVencimento"] = c.DataVencimento.ToString("dd/MM/yyyy"),
        ["DataPagamento"] = c.DataPagamento?.ToString("dd/MM/yyyy"),
        ["NrDocumento"] = c.NrDocumento,
        ["NrNotaFiscal"] = c.NrNotaFiscal,
        ["Status"] = StatusParaTexto(c.Status),
        ["Observacao"] = c.Observacao,
        ["Ativo"] = c.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
