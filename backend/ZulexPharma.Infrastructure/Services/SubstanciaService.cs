using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Substancias;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class SubstanciaService : ISubstanciaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Substâncias";
    private const string ENTIDADE = "Substancia";

    public SubstanciaService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<SubstanciaListDto>> ListarAsync()
    {
        try
        {
            return await _db.Substancias.OrderBy(s => s.Nome)
                .Select(s => new SubstanciaListDto
                {
                    Id = s.Id,
                    Nome = s.Nome,
                    Dcb = s.Dcb,
                    Cas = s.Cas,
                    ControleEspecialSngpc = s.ControleEspecialSngpc,
                    ClasseTerapeutica = s.ClasseTerapeutica,
                    CriadoEm = s.CriadoEm,
                    Ativo = s.Ativo
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em SubstanciaService.ListarAsync");
            throw;
        }
    }

    public async Task<SubstanciaListDto> CriarAsync(SubstanciaFormDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Dcb))  throw new ArgumentException("DCB é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Cas))  throw new ArgumentException("CAS é obrigatório.");

            var s = new Substancia
            {
                Nome = dto.Nome.Trim().ToUpper(),
                Dcb  = dto.Dcb.Trim().ToUpper(),
                Cas  = dto.Cas.Trim().ToUpper(),
                ControleEspecialSngpc = dto.ControleEspecialSngpc,
                ClasseTerapeutica = dto.ClasseTerapeutica?.Trim(),
                Ativo = dto.Ativo
            };

            _db.Substancias.Add(s);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, s.Id, novo: ParaDict(s));

            return new SubstanciaListDto
            {
                Id = s.Id, Nome = s.Nome, Dcb = s.Dcb, Cas = s.Cas,
                ControleEspecialSngpc = s.ControleEspecialSngpc,
                ClasseTerapeutica = s.ClasseTerapeutica,
                CriadoEm = s.CriadoEm, Ativo = s.Ativo
            };
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            Log.Error(ex, "Erro em SubstanciaService.CriarAsync");
            throw;
        }
    }

    public async Task AtualizarAsync(long id, SubstanciaFormDto dto)
    {
        try
        {
            var s = await _db.Substancias.FindAsync(id)
                ?? throw new KeyNotFoundException($"Substância {id} não encontrada.");

            var anterior = ParaDict(s);
            s.Nome = dto.Nome.Trim().ToUpper();
            s.Dcb  = dto.Dcb.Trim().ToUpper();
            s.Cas  = dto.Cas.Trim().ToUpper();
            s.ControleEspecialSngpc = dto.ControleEspecialSngpc;
            s.ClasseTerapeutica = dto.ClasseTerapeutica?.Trim();
            s.Ativo = dto.Ativo;

            await _db.SaveChangesAsync();

            var novo = ParaDict(s);
            if (!DictsIguais(anterior, novo))
                await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException)
        {
            Log.Error(ex, "Erro em SubstanciaService.AtualizarAsync | Id: {Id}", id);
            throw;
        }
    }

    public async Task<string> ExcluirAsync(long id)
    {
        try
        {
            var s = await _db.Substancias.FindAsync(id)
                ?? throw new KeyNotFoundException($"Substância {id} não encontrada.");

            var dados = ParaDict(s);
            _db.Substancias.Remove(s);

            try
            {
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
                return "excluido";
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                var recarregado = await _db.Substancias.FindAsync(id);
                recarregado!.Ativo = false;
                await _db.SaveChangesAsync();
                await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
                return "desativado";
            }
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em SubstanciaService.ExcluirAsync | Id: {Id}", id);
            throw;
        }
    }

    private static Dictionary<string, string?> ParaDict(Substancia s) => new()
    {
        ["Nome"]                 = s.Nome,
        ["DCB"]                  = s.Dcb,
        ["CAS"]                  = s.Cas,
        ["Controle Especial"]    = s.ControleEspecialSngpc ? "Sim" : "Não",
        ["Classe Terapêutica"]   = s.ClasseTerapeutica,
        ["Ativo"]                = s.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b)
        => a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
