using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.Infrastructure.Services;

public class SenhaDiaService : ISenhaDiaService
{
    private readonly IConfiguration _config;

    public SenhaDiaService(IConfiguration config) => _config = config;

    public string Gerar()
    {
        // Chave vem do config: env var SistemaKey no Railway (prod) sobrescreve o
        // appsettings (dev). Sem fallback hardcoded — falha alto se não configurada.
        var chave = _config["SistemaKey"]
            ?? throw new InvalidOperationException("SistemaKey não configurada (env var no Railway em prod, appsettings em dev).");
        var data = DateTime.UtcNow.ToString("yyyyMMdd");
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data + chave));
        return Convert.ToHexString(hash)[..8].ToLower();
    }

    public bool Validar(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha)) return false;
        return string.Equals(senha.Trim().ToLower(), Gerar(), StringComparison.Ordinal);
    }
}
