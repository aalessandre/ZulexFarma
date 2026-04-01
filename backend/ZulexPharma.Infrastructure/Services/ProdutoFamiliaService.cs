using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ProdutoFamiliaService : IProdutoFamiliaService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Gerenciar Produtos";
    private const string ENTIDADE = "ProdutoFamilia";

    public ProdutoFamiliaService(AppDbContext db, ILogAcaoService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<ProdutoFamiliaListDto>> ListarAsync()
    {
        return await _db.ProdutoFamilias.OrderBy(f => f.Nome)
            .Select(f => new ProdutoFamiliaListDto
            {
                Id = f.Id,
                Nome = f.Nome,
                CriadoEm = f.CriadoEm,
                Ativo = f.Ativo
            })
            .ToListAsync();
    }

    public async Task<ProdutoFamiliaListDto> CriarAsync(ProdutoFamiliaFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome é obrigatório.");

        var e = new ProdutoFamilia
        {
            Nome = dto.Nome.Trim().ToUpper(),
            Ativo = dto.Ativo
        };

        _db.ProdutoFamilias.Add(e);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, e.Id, novo: ParaDict(e));

        return new ProdutoFamiliaListDto
        {
            Id = e.Id, Nome = e.Nome, CriadoEm = e.CriadoEm, Ativo = e.Ativo
        };
    }

    public async Task AtualizarAsync(long id, ProdutoFamiliaFormDto dto)
    {
        var e = await _db.ProdutoFamilias.FindAsync(id)
            ?? throw new KeyNotFoundException($"Família {id} não encontrada.");

        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome é obrigatório.");

        var anterior = ParaDict(e);
        e.Nome = dto.Nome.Trim().ToUpper();
        e.Ativo = dto.Ativo;
        await _db.SaveChangesAsync();

        var novo = ParaDict(e);
        if (!DictsIguais(anterior, novo))
            await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id, anterior: anterior, novo: novo);
    }

    public async Task<string> ExcluirAsync(long id)
    {
        var e = await _db.ProdutoFamilias.FindAsync(id)
            ?? throw new KeyNotFoundException($"Família {id} não encontrada.");

        var dados = ParaDict(e);
        _db.ProdutoFamilias.Remove(e);

        try
        {
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id, anterior: dados);
            return "excluido";
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var recarregado = await _db.ProdutoFamilias.FindAsync(id);
            recarregado!.Ativo = false;
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
            return "desativado";
        }
    }

    private static Dictionary<string, string?> ParaDict(ProdutoFamilia f) => new()
    {
        ["Nome"] = f.Nome,
        ["Ativo"] = f.Ativo ? "Sim" : "Não"
    };

    private static bool DictsIguais(Dictionary<string, string?> a, Dictionary<string, string?> b)
        => a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);
}
