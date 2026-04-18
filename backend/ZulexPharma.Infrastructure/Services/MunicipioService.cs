using Microsoft.EntityFrameworkCore;
using ZulexPharma.Application.DTOs.Municipios;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class MunicipioService : IMunicipioService
{
    private readonly AppDbContext _db;

    public MunicipioService(AppDbContext db) => _db = db;

    public async Task<List<MunicipioDto>> PesquisarAsync(string uf, string? termo, int limit = 20)
    {
        if (limit <= 0 || limit > 100) limit = 20;
        var ufNorm = (uf ?? "").Trim().ToUpperInvariant();
        var q = _db.Municipios.AsQueryable();
        if (ufNorm.Length == 2) q = q.Where(m => m.Uf == ufNorm);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var termoNorm = Normalizar(termo);
            q = q.Where(m => m.NomeNormalizado.StartsWith(termoNorm));
        }

        return await q.OrderBy(m => m.Nome)
            .Take(limit)
            .Select(m => new MunicipioDto
            {
                Id = m.Id,
                CodigoIbge = m.CodigoIbge,
                Nome = m.Nome,
                Uf = m.Uf
            })
            .ToListAsync();
    }

    public async Task<Municipio?> ObterPorCodigoIbgeAsync(string codigoIbge)
    {
        if (string.IsNullOrWhiteSpace(codigoIbge)) return null;
        return await _db.Municipios.FirstOrDefaultAsync(m => m.CodigoIbge == codigoIbge);
    }

    public async Task<Municipio?> ResolverAsync(string? nome, string? uf)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(uf)) return null;
        var nomeNorm = Normalizar(nome);
        var ufNorm = uf.Trim().ToUpperInvariant();
        return await _db.Municipios
            .FirstOrDefaultAsync(m => m.Uf == ufNorm && m.NomeNormalizado == nomeNorm);
    }

    private static string Normalizar(string s)
    {
        var decomposed = s.Trim().Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        return sb.ToString().ToUpperInvariant();
    }
}
