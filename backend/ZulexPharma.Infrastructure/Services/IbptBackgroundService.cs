using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Verifica periodicamente se a tabela IBPTax está expirada.
/// Se token e CNPJ estiverem configurados, sincroniza automaticamente via API.
/// </summary>
public class IbptBackgroundService : BackgroundService
{
    private readonly IServiceProvider _provider;

    // Status compartilhado para o frontend consultar
    public static bool TabelaExpirada { get; private set; }
    public static DateTime? VigenciaFim { get; private set; }
    public static DateTime? UltimaVerificacao { get; private set; }
    public static DateTime? UltimaSincronizacao { get; private set; }
    public static string? VersaoAtual { get; private set; }
    public static int TotalRegistros { get; private set; }
    public static bool Sincronizando { get; private set; }

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
                    await VerificarEAtualizar(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    Log.Error(ex, "IBPTax: Erro ao verificar vigência.");
                }

                // Verificar a cada 24 horas
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown normal
        }
    }

    private async Task VerificarEAtualizar(CancellationToken ct)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var versao = (await db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.versao", ct))?.Valor;
        var vigFim = (await db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.vigencia.fim", ct))?.Valor;
        var total = (await db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.total.registros", ct))?.Valor;

        VersaoAtual = versao;
        TotalRegistros = int.TryParse(total, out var t) ? t : 0;
        UltimaVerificacao = DateTime.UtcNow;

        if (string.IsNullOrEmpty(vigFim) || !DateTime.TryParse(vigFim, out var dataFim))
        {
            TabelaExpirada = true;
            VigenciaFim = null;
        }
        else
        {
            VigenciaFim = dataFim;
            TabelaExpirada = dataFim < DateTime.UtcNow;
        }

        // Se expirada ou próxima de expirar (30 dias), tentar auto-sync
        var deveAtualizar = TabelaExpirada || TotalRegistros == 0 ||
                            (VigenciaFim.HasValue && (VigenciaFim.Value - DateTime.UtcNow).Days <= 30);

        if (deveAtualizar)
        {
            var token = (await db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.token", ct))?.Valor;
            var cnpj = (await db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.cnpj", ct))?.Valor;

            if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(cnpj))
            {
                Log.Information("IBPTax: Iniciando atualização automática via API...");
                try
                {
                    Sincronizando = true;
                    var ibptService = scope.ServiceProvider.GetRequiredService<IbptService>();
                    var result = await ibptService.SincronizarViaApiAsync(ct);

                    UltimaSincronizacao = DateTime.UtcNow;
                    VersaoAtual = result.Versao;
                    VigenciaFim = result.VigenciaFim;
                    TotalRegistros = result.TotalSincronizado;
                    TabelaExpirada = result.VigenciaFim.HasValue && result.VigenciaFim.Value < DateTime.UtcNow;

                    Log.Information("IBPTax: Atualização automática concluída. {Total} registros sincronizados.", result.TotalSincronizado);
                }
                catch (Exception ex)
                {
                    Log.Warning("IBPTax: Falha na atualização automática: {Msg}", ex.Message);
                }
                finally
                {
                    Sincronizando = false;
                }
            }
            else
            {
                if (TabelaExpirada)
                    Log.Warning("IBPTax: Tabela expirada. Configure Token e CNPJ em Configurações > Fiscal para atualização automática.");
                else if (TotalRegistros == 0)
                    Log.Warning("IBPTax: Tabela não importada. Configure Token e CNPJ em Configurações > Fiscal.");
            }
        }
        else
        {
            var diasRestantes = (VigenciaFim!.Value - DateTime.UtcNow).Days;
            Log.Information("IBPTax: Tabela versão {Versao}, vigente até {VigenciaFim} ({Dias} dias restantes).",
                versao, VigenciaFim.Value.ToString("dd/MM/yyyy"), diasRestantes);
        }
    }
}
