using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Background service that periodically syncs data with the central server (Railway).
/// Each local pharmacy runs this service to push local changes and pull remote changes.
/// Authentication uses the SISTEMA user with daily rotating password.
/// </summary>
public class SyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly int _intervaloSegundos;
    private readonly bool _habilitado;
    private readonly long _filialLocalId;
    private readonly string _urlCentral;

    private string? _tokenCache;
    private DateTime _tokenExpiracao = DateTime.MinValue;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Static properties for status monitoring from SistemaController/SyncController
    public static bool Rodando { get; private set; }
    public static DateTime? UltimaExecucao { get; private set; }
    public static string? UltimoStatus { get; private set; }
    public static string? UltimoErro { get; private set; }

    public SyncBackgroundService(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _config = config;
        _intervaloSegundos = int.TryParse(config["Sync:IntervaloSegundos"], out var i) ? i : 60;
        _habilitado = config["Sync:Habilitado"]?.ToLower() == "true";
        _filialLocalId = long.TryParse(config["Sync:FilialLocalId"], out var f) ? f : 0;
        _urlCentral = config["Sync:UrlCentral"]?.TrimEnd('/') ?? "";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_habilitado)
        {
            Log.Information("Serviço de sync desabilitado. Configure Sync:Habilitado=true para ativar.");
            return;
        }

        if (_filialLocalId == 0)
        {
            Log.Warning("Sync habilitado mas FilialLocalId não configurado. Serviço não iniciará.");
            return;
        }

        if (string.IsNullOrEmpty(_urlCentral))
        {
            Log.Warning("Sync habilitado mas UrlCentral não configurada. Serviço não iniciará.");
            return;
        }

        Log.Information("Serviço de sync iniciado. Filial: {FilialId} | Central: {Url} | Intervalo: {Intervalo}s",
            _filialLocalId, _urlCentral, _intervaloSegundos);

        Rodando = true;

        // Aguarda 10s antes do primeiro ciclo para o app inicializar
        await Task.Delay(10_000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecutarCicloSync(stoppingToken);
                UltimaExecucao = DateTime.UtcNow;
                UltimoStatus = "OK";
                UltimoErro = null;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro no ciclo de sync");
                UltimaExecucao = DateTime.UtcNow;
                UltimoStatus = "ERRO";
                UltimoErro = ex.Message;
            }

            await Task.Delay(_intervaloSegundos * 1000, stoppingToken);
        }

        Rodando = false;
        Log.Information("Serviço de sync encerrado.");
    }

    /// <summary>
    /// Executes a full sync cycle: for each table, push local changes then pull remote changes.
    /// </summary>
    private async Task ExecutarCicloSync(CancellationToken ct)
    {
        using var httpClient = CriarHttpClient();

        // Authenticate with central server
        var token = await ObterToken(httpClient, ct);
        if (string.IsNullOrEmpty(token))
        {
            Log.Warning("Não foi possível autenticar no servidor central. Ciclo de sync abortado.");
            return;
        }
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var tabelas = SyncService.TabelasSyncaveis;
        var totalEnviados = 0;
        var totalRecebidos = 0;

        foreach (var tabela in tabelas)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // PUSH: send local changes to central
                var enviados = await EnviarAlteracoes(httpClient, tabela, ct);
                totalEnviados += enviados;

                // PULL: receive remote changes from central
                var recebidos = await ReceberAlteracoes(httpClient, tabela, ct);
                totalRecebidos += recebidos;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Erro ao sincronizar tabela {Tabela}", tabela);

                // Update control with error status
                using var scope = _services.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();
                await syncService.AtualizarControle(_filialLocalId, tabela, status: "ERRO", erro: ex.Message);
            }
        }

        if (totalEnviados > 0 || totalRecebidos > 0)
            Log.Information("Sync completo. Enviados: {Enviados} | Recebidos: {Recebidos}", totalEnviados, totalRecebidos);
        else
            Log.Debug("Sync completo. Sem alterações pendentes.");
    }

    /// <summary>
    /// PUSH: Gets local changes since last sent version and POSTs them to central server.
    /// </summary>
    private async Task<int> EnviarAlteracoes(HttpClient httpClient, string tabela, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();

        // Get last sent version from SyncControle
        var statusList = await syncService.ObterStatus(_filialLocalId);
        var status = statusList.FirstOrDefault(s => s.Tabela == tabela);
        var ultimaVersaoEnviada = status?.UltimaVersaoEnviada ?? 0;

        // Get local changes since that version (only records from THIS filial)
        var pacote = await syncService.ObterAlteracoesLocais(tabela, ultimaVersaoEnviada, _filialLocalId);
        if (pacote.TotalRegistros == 0)
            return 0;

        // POST to central /api/sync/enviar
        var dto = new
        {
            tabela = pacote.Tabela,
            filialId = _filialLocalId,
            versaoAte = pacote.VersaoAte,
            registros = pacote.Registros
        };

        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"{_urlCentral}/api/sync/enviar", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"Erro ao enviar para central ({response.StatusCode}): {body}");
        }

        // Parse response to check results
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var resultado = JsonSerializer.Deserialize<SyncEnviarResponse>(responseJson, _jsonOptions);

        // Update local SyncControle with last sent version
        await syncService.AtualizarControle(_filialLocalId, tabela, versaoEnviada: pacote.VersaoAte, status: "OK");

        Log.Debug("PUSH {Tabela}: {Enviados} enviados, {Conflitos} conflitos",
            tabela, resultado?.Data?.Aplicados ?? 0, resultado?.Data?.Conflitos ?? 0);

        return pacote.TotalRegistros;
    }

    /// <summary>
    /// PULL: Gets remote changes from central server and applies them locally.
    /// </summary>
    private async Task<int> ReceberAlteracoes(HttpClient httpClient, string tabela, CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();

        // Get last received version from SyncControle
        var statusList = await syncService.ObterStatus(_filialLocalId);
        var status = statusList.FirstOrDefault(s => s.Tabela == tabela);
        var ultimaVersaoRecebida = status?.UltimaVersaoRecebida ?? 0;

        // GET from central /api/sync/receber
        var url = $"{_urlCentral}/api/sync/receber?tabela={tabela}&versaoDesde={ultimaVersaoRecebida}&filialId={_filialLocalId}&limite=500";
        var response = await httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new Exception($"Erro ao receber do central ({response.StatusCode}): {body}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var pacoteResponse = JsonSerializer.Deserialize<SyncReceberResponse>(responseJson, _jsonOptions);
        var pacote = pacoteResponse?.Data;

        if (pacote == null || pacote.TotalRegistros == 0)
            return 0;

        // Apply received changes locally (suspend VersaoSync auto-increment to avoid echo loop)
        var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
        db.SuspenderAutoSync = true;
        SyncResultado resultado;
        try
        {
            resultado = await syncService.AplicarAlteracoes(tabela, pacote.Registros);
        }
        finally
        {
            db.SuspenderAutoSync = false;
        }

        // Reset PostgreSQL sequence to avoid PK conflicts on new local inserts
        await syncService.ResetarSequence(tabela);

        // Update local SyncControle with last received version (from central's perspective)
        await syncService.AtualizarControle(_filialLocalId, tabela, versaoRecebida: pacote.VersaoAte, status: "OK");

        Log.Debug("PULL {Tabela}: {Aplicados} aplicados, {Conflitos} conflitos, {Erros} erros",
            tabela, resultado.Aplicados, resultado.Conflitos, resultado.Erros);

        return pacote.TotalRegistros;
    }

    /// <summary>
    /// Authenticates with the central server using SISTEMA user credentials.
    /// Caches the JWT token until it expires.
    /// </summary>
    private async Task<string?> ObterToken(HttpClient httpClient, CancellationToken ct)
    {
        // Return cached token if still valid (with 5min margin)
        if (!string.IsNullOrEmpty(_tokenCache) && DateTime.UtcNow < _tokenExpiracao.AddMinutes(-5))
            return _tokenCache;

        try
        {
            var senha = GerarSenhaSistema();
            var loginDto = new { login = "SISTEMA", senha };
            var json = JsonSerializer.Serialize(loginDto, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{_urlCentral}/api/auth/login", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("Falha ao autenticar no central: {StatusCode}", response.StatusCode);
                _tokenCache = null;
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var loginResponse = JsonSerializer.Deserialize<LoginCentralResponse>(responseJson, _jsonOptions);

            if (loginResponse?.Success != true || string.IsNullOrEmpty(loginResponse.Data?.Token))
            {
                Log.Warning("Resposta de login inválida do central");
                _tokenCache = null;
                return null;
            }

            _tokenCache = loginResponse.Data.Token;
            _tokenExpiracao = loginResponse.Data.Expiracao;

            Log.Debug("Token obtido do central. Expira em: {Expiracao}", _tokenExpiracao);
            return _tokenCache;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro ao autenticar no servidor central");
            _tokenCache = null;
            return null;
        }
    }

    /// <summary>
    /// Generates the daily SISTEMA password using SHA256(YYYYMMDD + SistemaKey)[0..8].
    /// Same algorithm as AuthService.GerarSenhaSistema().
    /// </summary>
    private string GerarSenhaSistema()
    {
        var chave = _config["SistemaKey"] ?? "ZulexPharma2026!";
        var data = DateTime.UtcNow.ToString("yyyyMMdd");
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data + chave));
        return Convert.ToHexString(hash)[..8].ToLower();
    }

    private HttpClient CriarHttpClient()
    {
        var handler = new HttpClientHandler
        {
            // Em desenvolvimento, aceitar certificados self-signed se necessário
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}

// ─── Response DTOs for central server API calls ────────────────────────────

internal class LoginCentralResponse
{
    public bool Success { get; set; }
    public LoginCentralData? Data { get; set; }
}

internal class LoginCentralData
{
    public string Token { get; set; } = "";
    public DateTime Expiracao { get; set; }
}

internal class SyncEnviarResponse
{
    public bool Success { get; set; }
    public SyncResultado? Data { get; set; }
}

internal class SyncReceberResponse
{
    public bool Success { get; set; }
    public SyncPacoteResponse? Data { get; set; }
}

internal class SyncPacoteResponse
{
    public string Tabela { get; set; } = "";
    public long VersaoDesde { get; set; }
    public long VersaoAte { get; set; }
    public int TotalRegistros { get; set; }
    public List<string> Registros { get; set; } = new();
}
