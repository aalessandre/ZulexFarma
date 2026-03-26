using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Background service that periodically logs sync status.
/// In production (local pharmacy), this would push/pull data to/from the central server.
/// For now, it just monitors and updates SyncControle.
/// </summary>
public class SyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly int _intervaloSegundos;
    private readonly bool _habilitado;

    public SyncBackgroundService(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _intervaloSegundos = int.TryParse(config["Sync:IntervaloSegundos"], out var i) ? i : 60;
        _habilitado = config["Sync:Habilitado"]?.ToLower() == "true";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_habilitado)
        {
            Log.Information("Serviço de sync desabilitado. Configure Sync:Habilitado=true para ativar.");
            return;
        }

        Log.Information("Serviço de sync iniciado. Intervalo: {Intervalo}s", _intervaloSegundos);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecutarSync(stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro no ciclo de sync");
            }

            await Task.Delay(_intervaloSegundos * 1000, stoppingToken);
        }
    }

    private async Task ExecutarSync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();
        var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();

        // Get the local filial ID from configuration
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var filialIdStr = config["Sync:FilialLocalId"];
        if (!long.TryParse(filialIdStr, out var filialId) || filialId == 0)
            return; // No local filial configured

        var urlCentral = config["Sync:UrlCentral"];
        if (string.IsNullOrEmpty(urlCentral))
        {
            // No central server configured - just update status locally
            foreach (var tabela in SyncService.TabelasSyncaveis)
            {
                await syncService.AtualizarControle(filialId, tabela, status: "LOCAL");
            }
            return;
        }

        // In a real implementation, this would:
        // 1. For each table, get local changes (VersaoSync > ultimaVersaoEnviada)
        // 2. POST them to urlCentral/api/sync/enviar
        // 3. GET from urlCentral/api/sync/receber?tabela=X&versaoDesde=Y&filialId=localId
        // 4. Apply received changes locally
        // 5. Update SyncControle

        Log.Debug("Sync cycle completed for filial {FilialId}", filialId);
    }
}
