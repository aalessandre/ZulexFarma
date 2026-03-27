using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ZulexPharma.Application.DTOs.Auth;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public AuthController(IAuthService authService, IConfiguration config, AppDbContext db)
    {
        _authService = authService;
        _config = config;
        _db = db;
    }

    /// <summary>
    /// Retorna a filial padrão do usuário pelo login. Usado na tela de login.
    /// </summary>
    [HttpGet("filial-usuario/{login}")]
    public async Task<IActionResult> FilialUsuario(string login)
    {
        try
        {
            var usuario = await _db.Usuarios
                .Include(u => u.Filial)
                .FirstOrDefaultAsync(u => u.Login == login && u.Ativo);

            if (usuario == null)
                return Ok(new { success = true, filialId = 0, nomeFilial = "" });

            return Ok(new { success = true, filialId = usuario.FilialId, nomeFilial = usuario.Filial.NomeFantasia });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao buscar filial do usuario {Login}", login);
            return Ok(new { success = true, filialId = 0, nomeFilial = "" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            if (result == null)
                return Unauthorized(new { success = false, message = "Login ou senha inválidos." });

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro no AuthController.Login | Login: {Login}", request.Login);
            return StatusCode(500, new { success = false, message = "Erro interno ao realizar login." });
        }
    }

    /// <summary>
    /// Retorna a senha do dia do usuário SISTEMA.
    /// Protegido por uma API key configurada em appsettings.json (SistemaApiKey).
    /// Uso: GET /api/auth/senha-sistema?key=zulex-suporte-2026
    /// </summary>
    [HttpGet("senha-sistema")]
    public IActionResult SenhaSistema([FromQuery] string key)
    {
        var apiKey = _config["SistemaApiKey"] ?? "";
        if (string.IsNullOrEmpty(key) || key != apiKey)
            return Unauthorized(new { success = false, message = "Chave de acesso inválida." });

        var chave = _config["SistemaKey"] ?? "ZulexPharma2026!";
        var data = DateTime.UtcNow.ToString("yyyyMMdd");
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data + chave));
        var senha = Convert.ToHexString(hash)[..8].ToLower();

        return Ok(new
        {
            success = true,
            login = "SISTEMA",
            senha,
            data = DateTime.UtcNow.ToString("dd/MM/yyyy"),
            aviso = "Esta senha expira à meia-noite UTC."
        });
    }

    /// <summary>
    /// Libera uma ação por senha de supervisor.
    /// Valida as credenciais, verifica se o supervisor tem a permissão necessária,
    /// e registra a liberação no log do sistema.
    /// </summary>
    [Authorize]
    [HttpPost("liberar")]
    public async Task<IActionResult> Liberar([FromBody] LiberacaoRequestDto request)
    {
        try
        {
            // Validar credenciais do supervisor
            var supervisor = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Login == request.Login && u.Ativo);

            if (supervisor == null || !BCrypt.Net.BCrypt.Verify(request.Senha, supervisor.SenhaHash))
                return Ok(new { success = false, message = "Login ou senha do supervisor inválidos." });

            // Verificar se o supervisor tem a permissão necessária (ou é admin)
            if (!supervisor.IsAdministrador)
            {
                var grupoIds = await _db.UsuarioFilialGrupos
                    .Where(ufg => ufg.UsuarioId == supervisor.Id)
                    .Select(ufg => ufg.GrupoUsuarioId)
                    .Distinct()
                    .ToListAsync();

                if (!grupoIds.Contains(supervisor.GrupoUsuarioId))
                    grupoIds.Add(supervisor.GrupoUsuarioId);

                var temPermissao = await _db.UsuariosGruposPermissao
                    .AnyAsync(p => grupoIds.Contains(p.GrupoUsuarioId)
                        && p.CodigoTela == request.Tela
                        && (
                            (request.Acao == "c" && p.PodeConsultar) ||
                            (request.Acao == "i" && p.PodeIncluir) ||
                            (request.Acao == "a" && p.PodeAlterar) ||
                            (request.Acao == "e" && p.PodeExcluir)
                        ));

                if (!temPermissao)
                    return Ok(new { success = false, message = "O supervisor informado também não possui permissão para esta ação." });
            }

            // Registrar liberação no log
            var usuarioSolicitanteId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var nomeSolicitante = User.FindFirst("nome")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "?";
            var loginSolicitante = User.FindFirst(ClaimTypes.Name)?.Value ?? "?";

            var nomeAcao = request.Acao switch
            {
                "c" => "CONSULTAR", "i" => "INCLUIR",
                "a" => "ALTERAR", "e" => "EXCLUIR",
                _ => "AÇÃO"
            };

            _db.LogsAcao.Add(new LogAcao
            {
                RealizadoEm        = DateTime.UtcNow,
                UsuarioId           = usuarioSolicitanteId,
                Tela                = request.Tela,
                Acao                = $"LIBERAÇÃO POR SENHA ({nomeAcao})",
                Entidade            = request.Entidade,
                RegistroId          = request.RegistroId,
                ValoresAnteriores   = null,
                ValoresNovos        = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Tela = request.Tela,
                    AcaoSolicitada = nomeAcao,
                    SolicitadoPor = $"{nomeSolicitante} ({loginSolicitante})",
                    LiberadoPor = $"{supervisor.Nome} ({supervisor.Login})"
                }),
                LiberacaoPorSenha   = true,
                UsuarioLiberouId    = supervisor.Id
            });
            await _db.SaveChangesAsync();

            var tokenLiberacao = Filters.LiberacaoCache.GerarToken();
            return Ok(new { success = true, message = "Ação liberada com sucesso.", supervisorNome = supervisor.Nome, tokenLiberacao });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro no AuthController.Liberar");
            return StatusCode(500, new { success = false, message = "Erro ao processar liberação." });
        }
    }

    /// <summary>
    /// Altera a senha do usuário logado.
    /// </summary>
    [Authorize]
    [HttpPost("alterar-senha")]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaDto dto)
    {
        try
        {
            var userId = long.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0) return Unauthorized(new { success = false, message = "Usuário não identificado." });

            var usuario = await _db.Usuarios.FindAsync(userId);
            if (usuario == null) return NotFound(new { success = false, message = "Usuário não encontrado." });

            if (!BCrypt.Net.BCrypt.Verify(dto.SenhaAtual, usuario.SenhaHash))
                return Ok(new { success = false, message = "Senha atual incorreta." });

            if (dto.NovaSenha.Length < 4 || dto.NovaSenha.Length > 12)
                return Ok(new { success = false, message = "A nova senha deve ter entre 4 e 12 caracteres." });

            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Senha alterada com sucesso." });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao alterar senha");
            return StatusCode(500, new { success = false, message = "Erro ao alterar senha." });
        }
    }
}

public record LiberacaoRequestDto(
    string Login,
    string Senha,
    string Tela,
    string Acao,
    string? Entidade,
    string? RegistroId
);

public record AlterarSenhaDto(string SenhaAtual, string NovaSenha);
