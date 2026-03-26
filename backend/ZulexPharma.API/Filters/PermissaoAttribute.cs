using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;

namespace ZulexPharma.API.Filters;

/// <summary>
/// Cache de liberações por senha. Cada liberação gera um token UUID válido por 60 segundos.
/// </summary>
public static class LiberacaoCache
{
    private static readonly ConcurrentDictionary<string, DateTime> _tokens = new();

    public static string GerarToken()
    {
        LimparExpirados();
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = DateTime.UtcNow.AddSeconds(60);
        return token;
    }

    public static bool Validar(string? token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        if (_tokens.TryRemove(token, out var expira))
            return DateTime.UtcNow < expira;
        return false;
    }

    private static void LimparExpirados()
    {
        var agora = DateTime.UtcNow;
        foreach (var kv in _tokens)
            if (kv.Value < agora) _tokens.TryRemove(kv.Key, out _);
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class PermissaoAttribute : TypeFilterAttribute
{
    public PermissaoAttribute(string tela, string acao) : base(typeof(PermissaoFilter))
    {
        Arguments = new object[] { tela, acao };
    }
}

public class PermissaoFilter : IActionFilter
{
    private readonly string _tela;
    private readonly string _acao;

    public PermissaoFilter(string tela, string acao)
    {
        _tela = tela;
        _acao = acao;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;

        // Admin bypasses
        var isAdmin = user.FindFirst("isAdmin")?.Value;
        if (isAdmin == "True") return;

        // Liberação por senha bypasses
        var tokenLiberacao = context.HttpContext.Request.Headers["X-Liberacao"].FirstOrDefault();
        if (LiberacaoCache.Validar(tokenLiberacao)) return;

        var permClaim = user.FindFirst("permissoes")?.Value;
        if (string.IsNullOrEmpty(permClaim))
        {
            context.Result = new JsonResult(new { success = false, message = "Você não tem permissão para acessar esta funcionalidade." })
            { StatusCode = 403 };
            return;
        }

        try
        {
            var perms = JsonSerializer.Deserialize<Dictionary<string, string>>(permClaim);
            if (perms == null || !perms.TryGetValue(_tela, out var acoes) || !acoes.Contains(_acao))
            {
                context.Result = new JsonResult(new { success = false, message = "Você não tem permissão para realizar esta ação." })
                { StatusCode = 403 };
            }
        }
        catch
        {
            context.Result = new JsonResult(new { success = false, message = "Erro ao verificar permissões." })
            { StatusCode = 403 };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
