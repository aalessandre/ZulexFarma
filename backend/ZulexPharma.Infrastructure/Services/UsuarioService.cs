using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Application.DTOs.Usuarios;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class UsuarioService : IUsuarioService
{
    private readonly AppDbContext _db;

    public UsuarioService(AppDbContext db) => _db = db;

    public async Task<List<UsuarioListDto>> ListarAsync()
    {
        try
        {
            return await _db.Usuarios
                .Include(u => u.GrupoUsuario)
                .Include(u => u.Filial)
                .OrderBy(u => u.Nome)
                .Select(u => new UsuarioListDto
                {
                    Id             = u.Id,
                    Nome           = u.Nome,
                    Login          = u.Login,
                    Email          = u.Email,
                    Telefone       = u.Telefone,
                    IsAdministrador = u.IsAdministrador,
                    Ativo          = u.Ativo,
                    GrupoUsuarioId = u.GrupoUsuarioId,
                    NomeGrupo      = u.GrupoUsuario.Nome,
                    FilialId       = u.FilialId,
                    NomeFilial     = u.Filial.NomeFantasia,
                    UltimoAcesso   = u.UltimoAcesso
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em UsuarioService.ListarAsync");
            throw;
        }
    }

    public async Task<UsuarioListDto> CriarAsync(UsuarioFormDto dto)
    {
        try
        {
            // Validate login length
            if (dto.Login.Trim().Length < 6 || dto.Login.Trim().Length > 24)
                throw new ArgumentException("O login deve ter entre 6 e 24 caracteres.");

            if (string.IsNullOrWhiteSpace(dto.Senha))
                throw new ArgumentException("Senha é obrigatória ao criar usuário.");

            // Validate password length
            if (dto.Senha.Length < 4 || dto.Senha.Length > 12)
                throw new ArgumentException("A senha deve ter entre 4 e 12 caracteres.");

            var usuario = new Usuario
            {
                Nome            = dto.Nome.Trim(),
                Login           = dto.Login.Trim(),
                SenhaHash       = BCrypt.Net.BCrypt.HashPassword(dto.Senha),
                Email           = dto.Email?.Trim(),
                Telefone        = dto.Telefone?.Trim(),
                IsAdministrador = dto.IsAdministrador,
                Ativo           = dto.Ativo,
                GrupoUsuarioId  = dto.GrupoUsuarioId,
                FilialId        = dto.FilialId
            };

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            var grupo  = await _db.UsuariosGrupos.FindAsync(usuario.GrupoUsuarioId);
            var filial = await _db.Filiais.FindAsync(usuario.FilialId);

            return new UsuarioListDto
            {
                Id             = usuario.Id,
                Nome           = usuario.Nome,
                Login          = usuario.Login,
                Email          = usuario.Email,
                Telefone       = usuario.Telefone,
                IsAdministrador = usuario.IsAdministrador,
                Ativo          = usuario.Ativo,
                GrupoUsuarioId = usuario.GrupoUsuarioId,
                NomeGrupo      = grupo?.Nome ?? string.Empty,
                FilialId       = usuario.FilialId,
                NomeFilial     = filial?.NomeFantasia ?? string.Empty,
                UltimoAcesso   = usuario.UltimoAcesso
            };
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            Log.Error(ex, "Erro em UsuarioService.CriarAsync | Login: {Login}", dto.Login);
            throw;
        }
    }

    public async Task AtualizarAsync(long id, UsuarioFormDto dto)
    {
        try
        {
            // Validate login length
            if (dto.Login.Trim().Length < 6 || dto.Login.Trim().Length > 24)
                throw new ArgumentException("O login deve ter entre 6 e 24 caracteres.");

            // Validate password length (only when changing password)
            if (!string.IsNullOrWhiteSpace(dto.Senha))
            {
                if (dto.Senha.Length < 4 || dto.Senha.Length > 12)
                    throw new ArgumentException("A senha deve ter entre 4 e 12 caracteres.");
            }

            var usuario = await _db.Usuarios.FindAsync(id)
                ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");

            usuario.Nome            = dto.Nome.Trim();
            usuario.Login           = dto.Login.Trim();
            usuario.Email           = dto.Email?.Trim();
            usuario.Telefone        = dto.Telefone?.Trim();
            usuario.IsAdministrador = dto.IsAdministrador;
            usuario.Ativo           = dto.Ativo;
            usuario.GrupoUsuarioId  = dto.GrupoUsuarioId;
            usuario.FilialId        = dto.FilialId;

            if (!string.IsNullOrWhiteSpace(dto.Senha))
                usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

            await _db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em UsuarioService.AtualizarAsync | Id: {Id}", id);
            throw;
        }
    }

    public async Task ExcluirAsync(long id)
    {
        try
        {
            var usuario = await _db.Usuarios.FindAsync(id)
                ?? throw new KeyNotFoundException($"Usuário {id} não encontrado.");

            usuario.Ativo = false;
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is not KeyNotFoundException)
        {
            Log.Error(ex, "Erro em UsuarioService.ExcluirAsync | Id: {Id}", id);
            throw;
        }
    }
}
