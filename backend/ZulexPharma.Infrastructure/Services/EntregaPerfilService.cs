using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Entregas;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class EntregaPerfilService : IEntregaPerfilService
{
    private readonly AppDbContext _db;
    private readonly ILogAcaoService _log;
    private const string TELA = "Entregas - Faixas e Regras";
    private const string ENTIDADE = "EntregaPerfil";

    public EntregaPerfilService(AppDbContext db, ILogAcaoService log) { _db = db; _log = log; }

    public async Task<List<EntregaPerfilDto>> ListarAsync(long filialId)
    {
        return await _db.EntregaPerfis
            .Where(p => p.FilialId == filialId)
            .OrderBy(p => p.Nome)
            .Select(p => new EntregaPerfilDto
            {
                Id = p.Id,
                FilialId = p.FilialId,
                Nome = p.Nome,
                Ativo = p.Ativo,
                Faixas = p.Faixas.OrderBy(f => f.Ordem).Select(f => new EntregaFaixaDto
                {
                    Id = f.Id, PerfilId = f.PerfilId,
                    RaioMaxKm = f.RaioMaxKm, Valor = f.Valor, Ordem = f.Ordem
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<EntregaPerfilDto?> ObterAsync(long id)
    {
        var p = await _db.EntregaPerfis
            .Include(x => x.Faixas)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return null;
        return new EntregaPerfilDto
        {
            Id = p.Id, FilialId = p.FilialId, Nome = p.Nome, Ativo = p.Ativo,
            Faixas = p.Faixas.OrderBy(f => f.Ordem).Select(f => new EntregaFaixaDto
            {
                Id = f.Id, PerfilId = f.PerfilId,
                RaioMaxKm = f.RaioMaxKm, Valor = f.Valor, Ordem = f.Ordem
            }).ToList()
        };
    }

    public async Task<EntregaPerfilDto> CriarAsync(EntregaPerfilFormDto dto)
    {
        ValidarForm(dto);
        var nome = dto.Nome.Trim().ToUpper();
        if (await _db.EntregaPerfis.AnyAsync(p => p.FilialId == dto.FilialId && p.Nome == nome))
            throw new ArgumentException($"Já existe perfil \"{nome}\" nessa filial.");

        var p = new EntregaPerfil { FilialId = dto.FilialId, Nome = nome, Ativo = dto.Ativo };
        foreach (var f in dto.Faixas.OrderBy(x => x.Ordem))
            p.Faixas.Add(new EntregaFaixa { RaioMaxKm = f.RaioMaxKm, Valor = f.Valor, Ordem = f.Ordem });

        _db.EntregaPerfis.Add(p);
        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "CRIAÇÃO", ENTIDADE, p.Id);
        return (await ObterAsync(p.Id))!;
    }

    public async Task AtualizarAsync(long id, EntregaPerfilFormDto dto)
    {
        ValidarForm(dto);
        var p = await _db.EntregaPerfis.Include(x => x.Faixas).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Perfil não encontrado.");

        var nome = dto.Nome.Trim().ToUpper();
        if (await _db.EntregaPerfis.AnyAsync(x => x.FilialId == p.FilialId && x.Nome == nome && x.Id != id))
            throw new ArgumentException($"Já existe perfil \"{nome}\" nessa filial.");

        p.Nome = nome;
        p.Ativo = dto.Ativo;

        // Estratégia simples: remove todas e recria (faixas são leves)
        _db.EntregaFaixas.RemoveRange(p.Faixas);
        p.Faixas.Clear();
        foreach (var f in dto.Faixas.OrderBy(x => x.Ordem))
            p.Faixas.Add(new EntregaFaixa { RaioMaxKm = f.RaioMaxKm, Valor = f.Valor, Ordem = f.Ordem });

        await _db.SaveChangesAsync();
        await _log.RegistrarAsync(TELA, "ALTERAÇÃO", ENTIDADE, id);
    }

    public async Task<string> ExcluirAsync(long id)
    {
        var p = await _db.EntregaPerfis.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Perfil não encontrado.");

        // RN-10: não permite excluir se em uso na agenda ou entregas em aberto
        var emUsoAgenda = await _db.EntregaAgendas.AnyAsync(a => a.PerfilId == id);
        if (emUsoAgenda)
            throw new ArgumentException("Perfil em uso na agenda. Troque os slots antes de excluir.");

        try
        {
            _db.EntregaPerfis.Remove(p);
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "EXCLUSÃO", ENTIDADE, id);
            return "excluido";
        }
        catch (DbUpdateException)
        {
            // Perfil referenciado por Entrega via EntregaFaixa — soft-delete
            p.Ativo = false;
            await _db.SaveChangesAsync();
            await _log.RegistrarAsync(TELA, "DESATIVAÇÃO", ENTIDADE, id);
            return "desativado";
        }
    }

    private static void ValidarForm(EntregaPerfilFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome do perfil é obrigatório.");
        if (dto.FilialId <= 0)
            throw new ArgumentException("Filial é obrigatória.");
        if (dto.Faixas == null || dto.Faixas.Count == 0)
            throw new ArgumentException("Perfil precisa ter ao menos uma faixa.");

        var raios = dto.Faixas.Select(f => f.RaioMaxKm).ToList();
        if (raios.Distinct().Count() != raios.Count)
            throw new ArgumentException("Há faixas com o mesmo raio máximo — valores devem ser únicos dentro do perfil.");

        if (dto.Faixas.Any(f => f.RaioMaxKm <= 0))
            throw new ArgumentException("Raio máximo deve ser maior que zero.");
        if (dto.Faixas.Any(f => f.Valor < 0))
            throw new ArgumentException("Valor não pode ser negativo.");
    }
}
