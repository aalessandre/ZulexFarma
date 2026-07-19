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
        // Chave vem do config: env var SistemaKey (prod) ou appsettings.Development.json (dev).
        // Sem fallback hardcoded — falha alto se ausente OU vazia (o placeholder "" do
        // appsettings.json versionado NAO pode virar chave de derivacao).
        var chave = _config["SistemaKey"];
        if (string.IsNullOrWhiteSpace(chave))
            throw new InvalidOperationException("SistemaKey não configurada (env var em prod, appsettings.Development.json em dev).");
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
