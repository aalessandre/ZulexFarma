using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Text.Json;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Background service that periodically checks for system updates.
/// When a new version is found, it logs a warning.
/// In production, this would download and apply the update.
/// </summary>
public class UpdateBackgroundService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly int _intervaloMinutos;
    private readonly bool _habilitado;

    // Shared state for the frontend to query
    public static string? VersaoDisponivel { get; private set; }
    public static string? DescricaoAtualizacao { get; private set; }
    public static DateTime? UltimaVerificacao { get; private set; }
    public static bool AtualizacaoDisponivel { get; private set; }

    public UpdateBackgroundService(IConfiguration config)
    {
        _config = config;
        _intervaloMinutos = int.TryParse(config["Atualizacao:IntervaloVerificacaoMinutos"], out var i) ? i : 60;
        _habilitado = config["Atualizacao:Habilitado"]?.ToLower() == "true";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_habilitado)
        {
            Log.Information("Serviço de atualização desabilitado.");
            return;
        }

        Log.Information("Serviço de atualização iniciado. Intervalo: {Intervalo} min", _intervaloMinutos);

        // Wait a bit before first check
        await Task.Delay(30000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarAtualizacao();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao verificar atualização");
            }

            await Task.Delay(_intervaloMinutos * 60 * 1000, stoppingToken);
        }
    }

    private async Task VerificarAtualizacao()
    {
        var urlManifest = _config["Atualizacao:UrlManifest"];
        if (string.IsNullOrEmpty(urlManifest)) return;

        var versaoAtual = _config["Sistema:Versao"] ?? "0.0.0";

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        var json = await http.GetStringAsync(urlManifest);
        var manifest = JsonSerializer.Deserialize<ManifestInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        UltimaVerificacao = DateTime.UtcNow;

        if (manifest == null) return;

        var p1 = versaoAtual.Split('.').Select(int.Parse).ToArray();
        var p2 = manifest.Versao.Split('.').Select(int.Parse).ToArray();
        var novaDisponivel = false;
        for (int i = 0; i < Math.Max(p1.Length, p2.Length); i++)
        {
            var a = i < p1.Length ? p1[i] : 0;
            var b = i < p2.Length ? p2[i] : 0;
            if (b > a) { novaDisponivel = true; break; }
            if (a > b) break;
        }

        AtualizacaoDisponivel = novaDisponivel;
        VersaoDisponivel = manifest.Versao;
        DescricaoAtualizacao = manifest.Descricao;

        if (novaDisponivel)
        {
            Log.Warning("Nova versão disponível: {VersaoNova} (atual: {VersaoAtual})", manifest.Versao, versaoAtual);
        }
    }

    private class ManifestInfo
    {
        public string Versao { get; set; } = "";
        public string? Descricao { get; set; }
    }
}
