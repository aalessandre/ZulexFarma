using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Verifica periodicamente se a tabela IBPTax está expirada e loga aviso.
/// A atualização automática via API do IBPT requer token de acesso (plano pago).
/// Para o fluxo gratuito, o sistema avisa e o usuário faz upload manual do CSV.
/// </summary>
public class IbptBackgroundService : BackgroundService
{
    private readonly IServiceProvider _provider;

    // Status compartilhado para o frontend consultar
    public static bool TabelaExpirada { get; private set; }
    public static DateTime? VigenciaFim { get; private set; }
    public static DateTime? UltimaVerificacao { get; private set; }
    public static string? VersaoAtual { get; private set; }
    public static int TotalRegistros { get; private set; }

    public IbptBackgroundService(IServiceProvider provider) => _provider = provider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("IBPTax: Serviço de verificação de vigência iniciado.");

        try
        {
            await Task.Delay(15000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await VerificarVigencia();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "IBPTax: Erro ao verificar vigência.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown normal — ignorar
        }
    }

    private async Task VerificarVigencia()
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var versao = (await db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.versao"))?.Valor;
        var vigFim = (await db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.vigencia.fim"))?.Valor;
        var total = (await db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.total.registros"))?.Valor;

        VersaoAtual = versao;
        TotalRegistros = int.TryParse(total, out var t) ? t : 0;
        UltimaVerificacao = DateTime.UtcNow;

        if (string.IsNullOrEmpty(vigFim) || !DateTime.TryParse(vigFim, out var dataFim))
        {
            TabelaExpirada = true;
            VigenciaFim = null;
            if (TotalRegistros == 0)
                Log.Warning("IBPTax: Tabela não importada. Faça upload do CSV em Configurações > Fiscal.");
            return;
        }

        VigenciaFim = dataFim;
        TabelaExpirada = dataFim < DateTime.UtcNow;

        if (TabelaExpirada)
        {
            Log.Warning("IBPTax: Tabela expirada em {VigenciaFim}. Atualize em Configurações > Fiscal.", dataFim.ToString("dd/MM/yyyy"));
        }
        else
        {
            var diasRestantes = (dataFim - DateTime.UtcNow).Days;
            if (diasRestantes <= 30)
            {
                Log.Warning("IBPTax: Tabela expira em {Dias} dias ({VigenciaFim}). Prepare a atualização.", diasRestantes, dataFim.ToString("dd/MM/yyyy"));
            }
            else
            {
                Log.Information("IBPTax: Tabela versão {Versao}, vigente até {VigenciaFim} ({Dias} dias restantes).",
                    versao, dataFim.ToString("dd/MM/yyyy"), diasRestantes);
            }
        }
    }
}
