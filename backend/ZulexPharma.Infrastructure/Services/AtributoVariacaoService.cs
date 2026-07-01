using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Grade;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// CRUD dos eixos de variação (Tamanho, Cor, …) e seus valores. Cadastro global
/// reusável usado pela grade de produtos. Ver docs/specs/multiramo-grade.md.
/// </summary>
public class AtributoVariacaoService : IAtributoVariacaoService
{
    private readonly AppDbContext _db;

    public AtributoVariacaoService(AppDbContext db) => _db = db;

    public async Task<List<AtributoVariacaoDto>> ListarAsync()
    {
        return await _db.AtributosVariacao
            .Include(a => a.Valores)
            .OrderBy(a => a.Ordem).ThenBy(a => a.Nome)
            .Select(a => MapDto(a))
            .ToListAsync();
    }

    public async Task<AtributoVariacaoDto?> ObterAsync(long id)
    {
        var a = await _db.AtributosVariacao.Include(x => x.Valores).FirstOrDefaultAsync(x => x.Id == id);
        return a == null ? null : MapDto(a);
    }

    public async Task<AtributoVariacaoDto> CriarAsync(AtributoVariacaoFormDto dto)
    {
        Validar(dto);

        var atr = new AtributoVariacao
        {
            Nome = dto.Nome.Trim(),
            Ordem = dto.Ordem,
            Ativo = dto.Ativo,
            Valores = dto.Valores.Select(v => new ValorAtributo
            {
                Valor = v.Valor.Trim(),
                Hex = string.IsNullOrWhiteSpace(v.Hex) ? null : v.Hex.Trim(),
                Ordem = v.Ordem
            }).ToList()
        };

        _db.AtributosVariacao.Add(atr);
        await _db.SaveChangesAsync();
        return MapDto(atr);
    }

    public async Task AtualizarAsync(long id, AtributoVariacaoFormDto dto)
    {
        Validar(dto);

        var atr = await _db.AtributosVariacao.Include(a => a.Valores).FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException($"Atributo {id} não encontrado.");

        atr.Nome = dto.Nome.Trim();
        atr.Ordem = dto.Ordem;
        atr.Ativo = dto.Ativo;
        atr.AtualizadoEm = DateTime.UtcNow;

        // Sincroniza os valores: remove os que sumiram, atualiza os existentes, adiciona os novos.
        var idsForm = dto.Valores.Where(v => v.Id.HasValue).Select(v => v.Id!.Value).ToHashSet();
        foreach (var removido in atr.Valores.Where(v => !idsForm.Contains(v.Id)).ToList())
            _db.ValoresAtributo.Remove(removido);

        foreach (var vf in dto.Valores)
        {
            if (vf.Id.HasValue)
            {
                var v = atr.Valores.FirstOrDefault(x => x.Id == vf.Id.Value);
                if (v == null) continue;
                v.Valor = vf.Valor.Trim();
                v.Hex = string.IsNullOrWhiteSpace(vf.Hex) ? null : vf.Hex.Trim();
                v.Ordem = vf.Ordem;
                v.AtualizadoEm = DateTime.UtcNow;
            }
            else
            {
                atr.Valores.Add(new ValorAtributo
                {
                    Valor = vf.Valor.Trim(),
                    Hex = string.IsNullOrWhiteSpace(vf.Hex) ? null : vf.Hex.Trim(),
                    Ordem = vf.Ordem
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<string> ExcluirAsync(long id)
    {
        var atr = await _db.AtributosVariacao.FindAsync(id)
            ?? throw new KeyNotFoundException($"Atributo {id} não encontrado.");

        _db.AtributosVariacao.Remove(atr);
        try
        {
            await _db.SaveChangesAsync();
            return "excluido";
        }
        catch (DbUpdateException)
        {
            // Em uso por algum produto (FK Restrict) — apenas desativa.
            _db.ChangeTracker.Clear();
            var recarregado = await _db.AtributosVariacao.FindAsync(id);
            recarregado!.Ativo = false;
            await _db.SaveChangesAsync();
            return "desativado";
        }
    }

    private static void Validar(AtributoVariacaoFormDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Informe o nome do atributo.");
        if (dto.Valores.Any(v => string.IsNullOrWhiteSpace(v.Valor)))
            throw new ArgumentException("Todo valor precisa de um texto.");
    }

    private static AtributoVariacaoDto MapDto(AtributoVariacao a) => new()
    {
        Id = a.Id,
        Nome = a.Nome,
        Ordem = a.Ordem,
        Ativo = a.Ativo,
        Valores = a.Valores
            .OrderBy(v => v.Ordem).ThenBy(v => v.Valor)
            .Select(v => new ValorAtributoDto { Id = v.Id, Valor = v.Valor, Hex = v.Hex, Ordem = v.Ordem })
            .ToList()
    };
}
