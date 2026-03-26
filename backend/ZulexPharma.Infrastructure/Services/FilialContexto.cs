using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Provides the current filial context from the authenticated user's JWT.
/// Inject this service wherever you need to know which filial the user is operating in.
/// </summary>
public class FilialContexto
{
    private readonly IHttpContextAccessor _http;

    public FilialContexto(IHttpContextAccessor http)
    {
        _http = http;
    }

    /// <summary>Returns the FilialId from the JWT. 0 if not available (SISTEMA user or unauthenticated).</summary>
    public long FilialIdAtual
    {
        get
        {
            var claim = _http.HttpContext?.User.FindFirst("filialId")?.Value;
            return long.TryParse(claim, out var id) ? id : 0;
        }
    }

    /// <summary>Returns the UserId from the JWT.</summary>
    public long UsuarioIdAtual
    {
        get
        {
            var claim = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(claim, out var id) ? id : 0;
        }
    }

    /// <summary>Returns true if the current user is an admin (bypasses filial filter).</summary>
    public bool IsAdmin
    {
        get
        {
            return _http.HttpContext?.User.FindFirst("isAdmin")?.Value == "True";
        }
    }
}
