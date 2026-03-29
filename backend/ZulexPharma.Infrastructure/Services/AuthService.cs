using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZulexPharma.Application.DTOs.Auth;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        try
        {
            // SISTEMA user - daily rotating password
            if (request.Login.Equals("SISTEMA", StringComparison.OrdinalIgnoreCase))
            {
                var senhaEsperada = GerarSenhaSistema();
                if (request.Senha != senhaEsperada)
                    return null;

                var token = GerarTokenSistema();
                var jwtSettingsSistema = _config.GetSection("JwtSettings");
                var expiracaoSistema = DateTime.UtcNow.AddHours(int.Parse(jwtSettingsSistema["ExpiracaoHoras"]!));

                return new LoginResponseDto(
                    Token: token,
                    Nome: "SISTEMA",
                    Login: "SISTEMA",
                    IsAdministrador: true,
                    NomeGrupo: "SISTEMA",
                    NomeFilial: "TODAS",
                    FilialId: 0,
                    Expiracao: expiracaoSistema,
                    FiliaisAcesso: new List<FilialAcessoDto>()
                );
            }

            var usuario = await _db.Usuarios
                .Include(u => u.GrupoUsuario)
                .Include(u => u.Filial)
                .FirstOrDefaultAsync(u => u.Login == request.Login && u.Ativo);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
                return null;

            usuario.UltimoAcesso = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Build aggregated permissions
            var permissoesJson = "{}";
            if (!usuario.IsAdministrador)
            {
                // Get all grupo IDs for this user (from UsuarioFilialGrupo)
                var grupoIds = await _db.UsuarioFilialGrupos
                    .Where(ufg => ufg.UsuarioId == usuario.Id)
                    .Select(ufg => ufg.GrupoUsuarioId)
                    .Distinct()
                    .ToListAsync();

                // Also include the direct GrupoUsuarioId (backward compat)
                if (!grupoIds.Contains(usuario.GrupoUsuarioId))
                    grupoIds.Add(usuario.GrupoUsuarioId);

                // Load all permissions for these groups
                var perms = await _db.UsuariosGruposPermissao
                    .Where(p => grupoIds.Contains(p.GrupoUsuarioId))
                    .ToListAsync();

                // Aggregate: for each tela, OR all permissions
                var aggregated = new Dictionary<string, string>();
                foreach (var group in perms.GroupBy(p => p.CodigoTela))
                {
                    var letters = "";
                    if (group.Any(p => p.PodeConsultar)) letters += "c";
                    if (group.Any(p => p.PodeIncluir))   letters += "i";
                    if (group.Any(p => p.PodeAlterar))   letters += "a";
                    if (group.Any(p => p.PodeExcluir))   letters += "e";
                    if (letters.Length > 0)
                        aggregated[group.Key] = letters;
                }

                permissoesJson = System.Text.Json.JsonSerializer.Serialize(aggregated);
            }

            // Determine session times (user overrides > system defaults)
            var sessaoMaxima = usuario.SessaoMaximaMinutos;
            var inatividade = usuario.InatividadeMinutos;

            if (sessaoMaxima == 0)
            {
                var cfg = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "sessao.maxima.minutos");
                sessaoMaxima = cfg != null && int.TryParse(cfg.Valor, out var sm) ? sm : 480;
            }
            if (inatividade == 0)
            {
                var cfg = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == "sessao.inatividade.minutos");
                inatividade = cfg != null && int.TryParse(cfg.Valor, out var im) ? im : 10;
            }

            // Get filiais the user has access to
            var filiaisAcesso = await _db.UsuarioFilialGrupos
                .Where(ufg => ufg.UsuarioId == usuario.Id)
                .Select(ufg => ufg.Filial)
                .Distinct()
                .Select(f => new FilialAcessoDto(f.Id, f.NomeFantasia))
                .ToListAsync();

            // If no UsuarioFilialGrupo entries, at least include the user's default filial
            if (filiaisAcesso.Count == 0)
                filiaisAcesso.Add(new FilialAcessoDto(usuario.FilialId, usuario.Filial.NomeFantasia));

            var tokenStr = GerarToken(usuario, permissoesJson, sessaoMaxima, inatividade);
            var jwtSettings = _config.GetSection("JwtSettings");
            var expiracao = DateTime.UtcNow.AddHours(int.Parse(jwtSettings["ExpiracaoHoras"]!));

            return new LoginResponseDto(
                Token: tokenStr,
                Nome: usuario.Nome,
                Login: usuario.Login,
                IsAdministrador: usuario.IsAdministrador,
                NomeGrupo: usuario.GrupoUsuario.Nome,
                NomeFilial: usuario.Filial.NomeFantasia,
                FilialId: usuario.FilialId,
                Expiracao: expiracao,
                FiliaisAcesso: filiaisAcesso
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro no AuthService.LoginAsync | Login: {Login}", request.Login);
            throw;
        }
    }

    private string GerarToken(Domain.Entities.Usuario usuario, string permissoesJson, int sessaoMaxima, int inatividade)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Login),
            new Claim("nome", usuario.Nome),
            new Claim("filialId", usuario.FilialId.ToString()),
            new Claim("grupoId", usuario.GrupoUsuarioId.ToString()),
            new Claim("isAdmin", usuario.IsAdministrador.ToString()),
            new Claim("permissoes", permissoesJson),
            new Claim("sessaoMaxima", sessaoMaxima.ToString()),
            new Claim("inatividade", inatividade.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(int.Parse(jwtSettings["ExpiracaoHoras"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GerarSenhaSistema()
    {
        var chave = _config["SistemaKey"] ?? "ZulexPharma2026!";
        var data = DateTime.UtcNow.ToString("yyyyMMdd");
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data + chave));
        // Return first 8 chars of hex hash as password
        return Convert.ToHexString(hash).Substring(0, 8).ToLower();
    }

    private string GerarTokenSistema()
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "0"),
            new Claim(ClaimTypes.Name, "SISTEMA"),
            new Claim("nome", "SISTEMA"),
            new Claim("filialId", "0"),
            new Claim("grupoId", "0"),
            new Claim("isAdmin", "True"),
            new Claim("permissoes", "{}"),
            new Claim("sessaoMaxima", "0"),
            new Claim("inatividade", "0")
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(int.Parse(jwtSettings["ExpiracaoHoras"]!)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
