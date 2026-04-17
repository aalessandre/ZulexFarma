using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Fidelidade;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class PremioFidelidadeService : IPremioFidelidadeService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Fidelidade — Prêmios";
    private const string ENTIDADE = "PremioFidelidade";

    public PremioFidelidadeService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<PremioFidelidadeListDto>> ListarAsync()
    {
        try
        {
            return await _db.PremiosFidelidade
                .OrderByDescending(p => p.Ativo).ThenBy(p => p.Nome)
                .Select(p => ToList(p))
                .ToListAsync();
        }
        catch (Exception ex) { Log.Error(ex, "Erro em PremioFidelidadeService.ListarAsync"); throw; }
    }

    public async Task<PremioFidelidadeListDto?> ObterAsync(long id)
    {
        try { var p = await _db.PremiosFidelidade.FindAsync(id); return p == null ? null : ToList(p); }
        catch (Exception ex) { Log.Error(ex, "Erro em PremioFidelidadeService.ObterAsync | Id: {Id}", id); throw; }
    }

    public async Task<PremioFidelidadeListDto> CriarAsync(PremioFidelidadeFormDto dto)
    {
        try
        {
            Validar(dto);
            var p = new PremioFidelidade();
            Aplicar(p, dto);
            _db.PremiosFidelidade.Add(p);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, p.Id, novo: ToDict(p));
            return ToList(p);
        }
        catch (Exception ex) when (ex is not ArgumentException) { Log.Error(ex, "Erro em PremioFidelidadeService.CriarAsync"); throw; }
    }

    public async Task AtualizarAsync(long id, PremioFidelidadeFormDto dto)
    {
        try
        {
            Validar(dto);
            var p = await _db.PremiosFidelidade.FindAsync(id)
                ?? throw new KeyNotFoundException($"Prêmio {id} não encontrado.");
            var anterior = ToDict(p);
            Aplicar(p, dto);
            await _db.SaveChangesAsync();
            var novo = ToDict(p);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException) { Log.Error(ex, "Erro em PremioFidelidadeService.AtualizarAsync | Id: {Id}", id); throw; }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var p = await _db.PremiosFidelidade.FindAsync(id)
                ?? throw new KeyNotFoundException($"Prêmio {id} não encontrado.");
            var dados = ToDict(p);
            _db.PremiosFidelidade.Remove(p);
            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.PremiosFidelidade.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException) { Log.Error(ex, "Erro em PremioFidelidadeService.ExcluirAsync | Id: {Id}", id); throw; }
    }

    private static void Validar(PremioFidelidadeFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        if (dto.PontosNecessarios <= 0) throw new ArgumentException("Pontos necessários deve ser maior que zero.");
    }

    private static void Aplicar(PremioFidelidade p, PremioFidelidadeFormDto dto)
    {
        p.Nome = dto.Nome.Trim().ToUpper();
        p.Descricao = dto.Descricao?.Trim();
        p.Categoria = dto.Categoria?.Trim().ToUpper();
        p.PontosNecessarios = dto.PontosNecessarios;
        p.ImagemUrl = dto.ImagemUrl?.Trim();
        p.Estoque = dto.Estoque;
        p.Ativo = dto.Ativo;
    }

    private static PremioFidelidadeListDto ToList(PremioFidelidade p) => new()
    {
        Id = p.Id, Codigo = p.Codigo, Nome = p.Nome, Descricao = p.Descricao,
        Categoria = p.Categoria, PontosNecessarios = p.PontosNecessarios,
        ImagemUrl = p.ImagemUrl, Estoque = p.Estoque, Ativo = p.Ativo, CriadoEm = p.CriadoEm
    };

    private static Dictionary<string, string?> ToDict(PremioFidelidade p) => new()
    {
        ["Nome"] = p.Nome, ["Categoria"] = p.Categoria,
        ["Pontos"] = p.PontosNecessarios.ToString(),
        ["Estoque"] = p.Estoque?.ToString(),
        ["Ativo"] = p.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b)
        => a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
