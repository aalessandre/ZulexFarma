using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Grupos;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Enums;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class GrupoService : IGrupoService
{
    private readonly AppDbContext _db;

    public GrupoService(AppDbContext db) => _db = db;

    public async Task<List<GrupoListDto>> ListarAsync()
    {
        try
        {
            return await _db.UsuariosGrupos
                .OrderBy(g => g.Nome)
                .Select(g => new GrupoListDto
                {
                    Id            = g.Id,
                    Nome          = g.Nome,
                    Descricao     = g.Descricao,
                    TotalUsuarios = g.Usuarios.Count(u => u.Ativo),
                    CriadoEm      = g.CriadoEm,
                    Ativo         = g.Ativo
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em GrupoService.ListarAsync");
            throw;
        }
    }

    public async Task<GrupoListDto> CriarAsync(GrupoFormDto dto)
    {
        try
        {
            var grupo = new GrupoUsuario
            {
                Nome      = dto.Nome.Trim(),
                Descricao = dto.Descricao?.Trim(),
                Ativo     = dto.Ativo
            };

            _db.UsuariosGrupos.Add(grupo);
            await _db.SaveChangesAsync();

            return new GrupoListDto
            {
                Id            = grupo.Id,
                Nome          = grupo.Nome,
                Descricao     = grupo.Descricao,
                TotalUsuarios = 0,
                CriadoEm      = grupo.CriadoEm,
                Ativo         = grupo.Ativo
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em GrupoService.CriarAsync | Nome: {Nome}", dto.Nome);
            throw;
        }
    }

    public async Task AtualizarAsync(long id, GrupoFormDto dto)
    {
        try
        {
            var grupo = await _db.UsuariosGrupos.FindAsync(id)
                ?? throw new KeyNotFoundException($"Grupo {id} não encontrado.");

            grupo.Nome      = dto.Nome.Trim();
            grupo.Descricao = dto.Descricao?.Trim();
            grupo.Ativo     = dto.Ativo;

            await _db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em GrupoService.AtualizarAsync | Id: {Id}", id);
            throw;
        }
    }

    public async Task ExcluirAsync(long id)
    {
        try
        {
            var grupo = await _db.UsuariosGrupos.FindAsync(id)
                ?? throw new KeyNotFoundException($"Grupo {id} não encontrado.");

            grupo.Ativo = false;
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em GrupoService.ExcluirAsync | Id: {Id}", id);
            throw;
        }
    }

    public async Task<List<PermissaoDto>> ListarPermissoesAsync(long grupoId)
    {
        return await _db.UsuariosGruposPermissao
            .Where(p => p.GrupoUsuarioId == grupoId)
            .Select(p => new PermissaoDto
            {
                Id = p.Id,
                Bloco = (int)p.Bloco,
                CodigoTela = p.CodigoTela,
                NomeTela = p.NomeTela,
                PodeConsultar = p.PodeConsultar,
                PodeIncluir = p.PodeIncluir,
                PodeAlterar = p.PodeAlterar,
                PodeExcluir = p.PodeExcluir
            })
            .ToListAsync();
    }

    public async Task SalvarPermissoesAsync(long grupoId, SalvarPermissoesDto dto)
    {
        var grupo = await _db.UsuariosGrupos.FindAsync(grupoId)
            ?? throw new KeyNotFoundException($"Grupo {grupoId} não encontrado.");

        // Remove existing
        var existentes = await _db.UsuariosGruposPermissao
            .Where(p => p.GrupoUsuarioId == grupoId)
            .ToListAsync();
        _db.UsuariosGruposPermissao.RemoveRange(existentes);

        // Add new
        foreach (var p in dto.Permissoes)
        {
            if (!p.PodeConsultar && !p.PodeIncluir && !p.PodeAlterar && !p.PodeExcluir)
                continue; // Skip entries with no permissions

            _db.UsuariosGruposPermissao.Add(new GrupoPermissao
            {
                GrupoUsuarioId = grupoId,
                Bloco = (BlocoMenu)p.Bloco,
                CodigoTela = p.CodigoTela,
                NomeTela = p.NomeTela,
                PodeConsultar = p.PodeConsultar,
                PodeIncluir = p.PodeIncluir,
                PodeAlterar = p.PodeAlterar,
                PodeExcluir = p.PodeExcluir
            });
        }

        await _db.SaveChangesAsync();
    }
}
