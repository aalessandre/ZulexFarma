using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.ContasBancarias;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ContaBancariaService : IContaBancariaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Contas Bancárias";
    private const string ENTIDADE = "ContaBancaria";

    public ContaBancariaService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<ContaBancariaListDto>> ListarAsync()
    {
        try
        {
            return await _db.ContasBancarias
                .Include(c => c.PlanoConta)
                .Include(c => c.Filial)
                .OrderBy(c => c.Descricao)
                .Select(c => new ContaBancariaListDto
                {
                    Id = c.Id,
                    Descricao = c.Descricao,
                    TipoConta = c.TipoConta,
                    TipoContaDescricao = TipoParaTexto(c.TipoConta),
                    Banco = c.Banco,
                    Agencia = c.Agencia,
                    AgenciaDigito = c.AgenciaDigito,
                    NumeroConta = c.NumeroConta,
                    ContaDigito = c.ContaDigito,
                    ChavePix = c.ChavePix,
                    SaldoInicial = c.SaldoInicial,
                    DataSaldoInicial = c.DataSaldoInicial,
                    PlanoContaId = c.PlanoContaId,
                    PlanoContaDescricao = c.PlanoConta != null ? c.PlanoConta.Descricao : null,
                    FilialId = c.FilialId,
                    FilialNome = c.Filial != null ? c.Filial.NomeFilial : null,
                    Observacao = c.Observacao,
                    Ativo = c.Ativo,
                    CriadoEm = c.CriadoEm
                })
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em ContaBancariaService.ListarAsync"); throw; }
    }

    public async Task<ContaBancariaListDto> CriarAsync(ContaBancariaFormDto dto)
    {
        try
        {
            Validar(dto);
            var cb = new ContaBancaria
            {
                Descricao = dto.Descricao.Trim().ToUpper(),
                TipoConta = dto.TipoConta,
                Banco = dto.Banco?.Trim(),
                Agencia = dto.Agencia?.Trim(),
                AgenciaDigito = dto.AgenciaDigito?.Trim(),
                NumeroConta = dto.NumeroConta?.Trim(),
                ContaDigito = dto.ContaDigito?.Trim(),
                ChavePix = dto.ChavePix?.Trim(),
                SaldoInicial = dto.SaldoInicial,
                DataSaldoInicial = dto.DataSaldoInicial,
                PlanoContaId = dto.PlanoContaId,
                FilialId = dto.FilialId,
                Observacao = dto.Observacao?.Trim(),
                Ativo = dto.Ativo
            };
            _db.ContasBancarias.Add(cb);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, cb.Id, novo: ParaDict(cb));

            return new ContaBancariaListDto
            {
                Id = cb.Id,
                Descricao = cb.Descricao,
                TipoConta = cb.TipoConta,
                TipoContaDescricao = TipoParaTexto(cb.TipoConta),
                Banco = cb.Banco,
                Agencia = cb.Agencia,
                AgenciaDigito = cb.AgenciaDigito,
                NumeroConta = cb.NumeroConta,
                ContaDigito = cb.ContaDigito,
                ChavePix = cb.ChavePix,
                SaldoInicial = cb.SaldoInicial,
                DataSaldoInicial = cb.DataSaldoInicial,
                PlanoContaId = cb.PlanoContaId,
                FilialId = cb.FilialId,
                Observacao = cb.Observacao,
                Ativo = cb.Ativo,
                CriadoEm = cb.CriadoEm
            };
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em ContaBancariaService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, ContaBancariaFormDto dto)
    {
        try
        {
            Validar(dto);
            var cb = await _db.ContasBancarias.FindAsync(id)
                ?? throw new KeyNotFoundException($"Conta bancária {id} não encontrada.");
            var anterior = ParaDict(cb);

            cb.Descricao = dto.Descricao.Trim().ToUpper();
            cb.TipoConta = dto.TipoConta;
            cb.Banco = dto.Banco?.Trim();
            cb.Agencia = dto.Agencia?.Trim();
            cb.AgenciaDigito = dto.AgenciaDigito?.Trim();
            cb.NumeroConta = dto.NumeroConta?.Trim();
            cb.ContaDigito = dto.ContaDigito?.Trim();
            cb.ChavePix = dto.ChavePix?.Trim();
            cb.SaldoInicial = dto.SaldoInicial;
            cb.DataSaldoInicial = dto.DataSaldoInicial;
            cb.PlanoContaId = dto.PlanoContaId;
            cb.FilialId = dto.FilialId;
            cb.Observacao = dto.Observacao?.Trim();
            cb.Ativo = dto.Ativo;
            await _db.SaveChangesAsync();

            var novo = ParaDict(cb);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em ContaBancariaService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var cb = await _db.ContasBancarias.FindAsync(id)
                ?? throw new KeyNotFoundException($"Conta bancária {id} não encontrada.");
            var dados = ParaDict(cb);
            _db.ContasBancarias.Remove(cb);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.ContasBancarias.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em ContaBancariaService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    private static void Validar(ContaBancariaFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descricao)) throw new ArgumentException("Descrição é obrigatória.");
        if (!Enum.IsDefined(dto.TipoConta)) throw new ArgumentException("Tipo de conta inválido.");
    }

    private static string TipoParaTexto(TipoConta tipo) => tipo switch
    {
        TipoConta.ContaCorrente => "Conta Corrente",
        TipoConta.Poupanca => "Poupança",
        TipoConta.CaixaInterno => "Caixa Interno",
        TipoConta.Investimento => "Investimento",
        TipoConta.CartaoCredito => "Cartão de Crédito",
        _ => ""
    };

    private static Dictionary<string, string?> ParaDict(ContaBancaria c) => new()
    {
        ["Descricao"] = c.Descricao,
        ["TipoConta"] = TipoParaTexto(c.TipoConta),
        ["Banco"] = c.Banco,
        ["Agencia"] = c.Agencia,
        ["AgenciaDigito"] = c.AgenciaDigito,
        ["NumeroConta"] = c.NumeroConta,
        ["ContaDigito"] = c.ContaDigito,
        ["ChavePix"] = c.ChavePix,
        ["SaldoInicial"] = c.SaldoInicial.ToString("N2"),
        ["DataSaldoInicial"] = c.DataSaldoInicial?.ToString("dd/MM/yyyy"),
        ["PlanoContaId"] = c.PlanoContaId?.ToString(),
        ["FilialId"] = c.FilialId?.ToString(),
        ["Observacao"] = c.Observacao,
        ["Ativo"] = c.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
