using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ZulexPharma.Infrastructure.Services;

namespace ZulexPharma.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SistemaController : ControllerBase
{
    private readonly IConfiguration _config;

    public SistemaController(IConfiguration config) => _config = config;

    /// <summary>
    /// Retorna a versão atual do sistema.
    /// </summary>
    [HttpGet("versao")]
    [AllowAnonymous]
    public IActionResult Versao()
    {
        return Ok(new
        {
            success = true,
            versao = _config["Sistema:Versao"] ?? "0.0.0",
            build = _config["Sistema:Build"] ?? "unknown",
            ambiente = _config["ASPNETCORE_ENVIRONMENT"] ?? "Production"
        });
    }

    /// <summary>
    /// Verifica se há atualização disponível.
    /// Compara a versão local com o manifest remoto.
    /// </summary>
    [Authorize]
    [HttpGet("atualizacao/verificar")]
    public async Task<IActionResult> VerificarAtualizacao()
    {
        try
        {
            var urlManifest = _config["Atualizacao:UrlManifest"];
            var versaoAtual = _config["Sistema:Versao"] ?? "0.0.0";

            if (string.IsNullOrEmpty(urlManifest))
            {
                return Ok(new
                {
                    success = true,
                    atualizado = true,
                    versaoAtual,
                    versaoDisponivel = versaoAtual,
                    mensagem = "URL de atualização não configurada."
                });
            }

            // Fetch manifest from remote server
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);
            var response = await httpClient.GetStringAsync(urlManifest);
            var manifest = System.Text.Json.JsonSerializer.Deserialize<ManifestDto>(response,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (manifest == null)
            {
                return Ok(new { success = false, message = "Manifest inválido." });
            }

            var atualizado = CompararVersoes(versaoAtual, manifest.Versao) >= 0;

            return Ok(new
            {
                success = true,
                atualizado,
                versaoAtual,
                versaoDisponivel = manifest.Versao,
                buildDisponivel = manifest.Build,
                descricao = manifest.Descricao,
                urlDownload = manifest.UrlDownload,
                tamanhoMb = manifest.TamanhoMb,
                dataPublicacao = manifest.DataPublicacao
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao verificar atualização");
            return Ok(new
            {
                success = true,
                atualizado = true,
                versaoAtual = _config["Sistema:Versao"] ?? "0.0.0",
                mensagem = "Não foi possível verificar atualizações. Verifique a conexão."
            });
        }
    }

    /// <summary>
    /// Retorna informações completas do sistema.
    /// </summary>
    [Authorize]
    [HttpGet("info")]
    public IActionResult Info()
    {
        return Ok(new
        {
            success = true,
            versao = _config["Sistema:Versao"] ?? "0.0.0",
            build = _config["Sistema:Build"] ?? "unknown",
            dotnet = Environment.Version.ToString(),
            os = Environment.OSVersion.ToString(),
            maquina = Environment.MachineName,
            processadores = Environment.ProcessorCount,
            memoriaAtual = GC.GetTotalMemory(false) / 1024 / 1024,
            uptime = (DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).ToString(@"d\.hh\:mm\:ss"),
            syncHabilitado = _config["Sync:Habilitado"]?.ToLower() == "true",
            atualizacaoHabilitada = _config["Atualizacao:Habilitado"]?.ToLower() == "true"
        });
    }

    /// <summary>
    /// Status da verificação de atualização do serviço background.
    /// </summary>
    [Authorize]
    [HttpGet("atualizacao/status")]
    public IActionResult StatusAtualizacao()
    {
        return Ok(new
        {
            success = true,
            atualizacaoDisponivel = UpdateBackgroundService.AtualizacaoDisponivel,
            versaoDisponivel = UpdateBackgroundService.VersaoDisponivel,
            descricao = UpdateBackgroundService.DescricaoAtualizacao,
            ultimaVerificacao = UpdateBackgroundService.UltimaVerificacao,
            versaoAtual = _config["Sistema:Versao"] ?? "0.0.0"
        });
    }

    private static int CompararVersoes(string v1, string v2)
    {
        var p1 = v1.Split('.').Select(int.Parse).ToArray();
        var p2 = v2.Split('.').Select(int.Parse).ToArray();
        for (int i = 0; i < Math.Max(p1.Length, p2.Length); i++)
        {
            var a = i < p1.Length ? p1[i] : 0;
            var b = i < p2.Length ? p2[i] : 0;
            if (a != b) return a.CompareTo(b);
        }
        return 0;
    }
}

public class ManifestDto
{
    public string Versao { get; set; } = string.Empty;
    public string? Build { get; set; }
    public string? Descricao { get; set; }
    public string? UrlDownload { get; set; }
    public double? TamanhoMb { get; set; }
    public string? DataPublicacao { get; set; }
}
