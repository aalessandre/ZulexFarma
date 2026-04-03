using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Produtos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class ProdutoLocalService : IProdutoLocalService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Gerenciar Produtos";
    private const string ENTIDADE = "ProdutoLocal";

    public ProdutoLocalService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<ProdutoLocalListDto>> ListarAsync()
    {
        return await _db.ProdutosLocais.OrderBy(l => l.Nome)
            .Select(l => new ProdutoLocalListDto
            {
                Id = l.Id, Nome = l.Nome, Ativo = l.Ativo, CriadoEm = l.CriadoEm
            })
            .ToListAsync();
    }

    public async Task<ProdutoLocalListDto> CriarAsync(ProdutoLocalFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        var e = new ProdutoLocal { Nome = dto.Nome.Trim().ToUpper(), Ativo = dto.Ativo };
        _db.ProdutosLocais.Add(e);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, e.Id,
            novo: new Dictionary<string, string?> { ["Nome"] = e.Nome });
        return new ProdutoLocalListDto { Id = e.Id, Nome = e.Nome, Ativo = e.Ativo, CriadoEm = e.CriadoEm };
    }

    public async Task AtualizarAsync(long id, ProdutoLocalFormDto dto)
    {
        var e = await _db.ProdutosLocais.FindAsync(id)
            ?? throw new KeyNotFoundException($"Local {id} não encontrado.");
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome é obrigatório.");
        e.Nome = dto.Nome.Trim().ToUpper();
        e.Ativo = dto.Ativo;
        await _db.SaveChangesAsync();
    }

    public async Task<string> ExcluirAsync(long id)
    {
        var e = await _db.ProdutosLocais.FindAsync(id)
            ?? throw new KeyNotFoundException($"Local {id} não encontrado.");
        _db.ProdutosLocais.Remove(e);
        try
        {
            await _db.SaveChangesAsync();
            return "excluido";
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var r = await _db.ProdutosLocais.FindAsync(id);
            r!.Ativo = false;
            await _db.SaveChangesAsync();
            return "desativado";
        }
    }
}
