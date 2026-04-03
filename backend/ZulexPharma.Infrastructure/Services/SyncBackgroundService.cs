using Microsoft.EntityFrameworkCore;
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
/// Sync v2: push/pull baseado em fila de operações.
/// </summary>
public class SyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly int _intervaloSegundos;
    private readonly bool _habilitado;
    private readonly int _filialCodigo;
    private readonly string _urlCentral;
    private readonly HttpClient _httpClient;

    private string? _tokenCache;
    private DateTime _tokenExpiracao = DateTime.MinValue;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static bool Rodando { get; private set; }
    public static DateTime? UltimaExecucao { get; private set; }
    public static string? UltimoStatus { get; private set; }
    public static int PendentesEnvio { get; private set; }
    public static int FalhasConsecutivas { get; private set; }

    public SyncBackgroundService(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _config = config;
        _intervaloSegundos = int.TryParse(config["Sync:IntervaloSegundos"], out var i) ? i : 30;
        _habilitado = config["Sync:Habilitado"]?.ToLower() == "true";
        _filialCodigo = int.TryParse(config["Filial:Codigo"], out var f) ? f : 0;
        _urlCentral = config["Sync:UrlCentral"]?.TrimEnd('/') ?? "";

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_habilitado || _filialCodigo == 0 || string.IsNullOrEmpty(_urlCentral))
        {
            Log.Information("Sync v2 desabilitado. Configure Sync:Habilitado, Filial:Codigo e Sync:UrlCentral.");
            return;
        }

        Log.Information("Sync v2 iniciado. Filial: {Filial} | Central: {Url} | Intervalo: {Int}s",
            _filialCodigo, _urlCentral, _intervaloSegundos);
        Rodando = true;

        await Task.Delay(10_000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var token = await ObterToken(stoppingToken);
                if (string.IsNullOrEmpty(token))
                {
                    FalhasConsecutivas++;
                    Log.Warning("Sync: falha ao obter token da central ({Falhas} consecutivas)", FalhasConsecutivas);
                }
                else
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    await Enviar(stoppingToken);
                    await Receber(stoppingToken);
                    FalhasConsecutivas = 0;
                }
                UltimaExecucao = DateTime.UtcNow;
                UltimoStatus = FalhasConsecutivas > 0 ? "ERRO" : "OK";
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro no ciclo sync v2");
                FalhasConsecutivas++;
                UltimoStatus = "ERRO";
            }

            await Task.Delay(_intervaloSegundos * 1000, stoppingToken);
        }
        Rodando = false;
    }

    private async Task Enviar(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var lote = int.TryParse(_config["Sync:LoteTamanho"], out var l) ? l : 100;
        var pendentes = await db.SyncFila
            .Where(f => !f.Enviado)
            .OrderBy(f => f.Id)
            .Take(lote)
            .ToListAsync(ct);

        PendentesEnvio = await db.SyncFila.CountAsync(f => !f.Enviado, ct);

        if (pendentes.Count == 0) return;

        var json = JsonSerializer.Serialize(pendentes.Select(p => new
        {
            p.Tabela, p.Operacao, p.RegistroId, p.RegistroCodigo,
            p.DadosJson, p.FilialOrigemId, p.CriadoEm
        }), _jsonOpts);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_urlCentral}/api/sync/enviar", content, ct);

        if (response.IsSuccessStatusCode)
        {
            foreach (var p in pendentes) { p.Enviado = true; p.EnviadoEm = DateTime.UtcNow; }
            await db.SaveChangesAsync(ct);
            Log.Information("Sync PUSH: {Count} operações enviadas", pendentes.Count);
        }
        else
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            Log.Warning("Sync PUSH falhou ({Status}): {Body}", response.StatusCode, body);
        }
    }

    private async Task Receber(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Buscar último Id recebido (armazena em config local simples)
        var ultimoIdConfig = await db.Configuracoes
            .FirstOrDefaultAsync(c => c.Chave == "sync.ultimo.id.recebido", ct);
        var ultimoId = long.TryParse(ultimoIdConfig?.Valor, out var u) ? u : 0;

        var response = await _httpClient.GetAsync(
            $"{_urlCentral}/api/sync/receber?filialId={_filialCodigo}&ultimoId={ultimoId}", ct);

        if (!response.IsSuccessStatusCode) return;

        var json = await response.Content.ReadAsStringAsync(ct);
        var resultado = JsonSerializer.Deserialize<SyncReceberResponse>(json, _jsonOpts);
        if (resultado?.Data == null || resultado.Data.Count == 0) return;

        db.AplicandoSync = true;
        var aplicados = 0;
        var erros = 0;
        var lastSuccessId = ultimoId;

        // Com IDs globais por filial (faixa exclusiva), FKs são sempre válidas.
        // Ordenar por dependência para INSERTs (tabelas pai primeiro).
        var ordenadas = resultado.Data
            .OrderBy(op => op.Operacao == "D" ? -SyncApplicator.GetOrdemTabela(op.Tabela) : SyncApplicator.GetOrdemTabela(op.Tabela))
            .ThenBy(op => op.Id)
            .ToList();

        foreach (var op in ordenadas)
        {
            try
            {
                var aplicou = await SyncApplicator.AplicarOperacaoAsync(
                    db, op.Tabela, op.Operacao, op.RegistroId, op.DadosJson, ct);

                if (aplicou)
                {
                    RegistrarRecebido(db, op);
                    aplicados++;
                }

                if (op.Id > lastSuccessId) lastSuccessId = op.Id;
            }
            catch (Exception ex)
            {
                // Avançar lastSuccessId mesmo em falha para não ficar em loop infinito
                if (op.Id > lastSuccessId) lastSuccessId = op.Id;

                // Detach entries com erro
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged))
                    entry.State = EntityState.Detached;

                // Extrair mensagem legível do erro
                var erroMsg = ExtrairMensagemErro(ex, op);
                Log.Warning("Sync PULL: {Erro}", erroMsg);

                // Registrar erro na SyncFila local para aparecer no painel
                try
                {
                    db.SyncFila.Add(new Domain.Entities.SyncFila
                    {
                        Tabela = op.Tabela,
                        Operacao = op.Operacao,
                        RegistroId = op.RegistroId,
                        RegistroCodigo = op.RegistroCodigo,
                        FilialOrigemId = op.FilialOrigemId,
                        Enviado = true, // Não tentar reenviar
                        EnviadoEm = DateTime.UtcNow,
                        Erro = erroMsg
                    });
                    await db.SaveChangesAsync(ct);
                }
                catch { /* silenciar erro ao gravar o próprio erro */ }

                erros++;
            }
        }

        // Sempre avançar o ponteiro para não reprocessar operações (inclusive falhas)
        // Manter AplicandoSync = true para não gerar SyncFila (config local, não replica)
        if (lastSuccessId > ultimoId)
        {
            if (ultimoIdConfig == null)
            {
                db.Configuracoes.Add(new Domain.Entities.Configuracao { Chave = "sync.ultimo.id.recebido", Valor = lastSuccessId.ToString() });
            }
            else
            {
                ultimoIdConfig.Valor = lastSuccessId.ToString();
            }
            await db.SaveChangesAsync(ct);
        }

        db.AplicandoSync = false;

        if (erros > 0) FalhasConsecutivas += erros;
        if (aplicados > 0) Log.Information("Sync PULL: {Count} aplicadas, {Erros} erros", aplicados, erros);
    }

    /// <summary>
    /// Ordem de dependência: tabelas pai têm número menor (processadas primeiro em INSERT).
    /// </summary>
    // GetOrdemTabela movido para SyncApplicator (classe pública compartilhada).

    /// <summary>
    /// Registra operação recebida na SyncFila local para aparecer no painel.
    /// </summary>
    private static void RegistrarRecebido(AppDbContext db, SyncOperacao op)
    {
        db.SyncFila.Add(new Domain.Entities.SyncFila
        {
            Tabela = op.Tabela,
            Operacao = op.Operacao,
            RegistroId = op.RegistroId,
            RegistroCodigo = op.RegistroCodigo,
            FilialOrigemId = op.FilialOrigemId,
            Enviado = true,
            EnviadoEm = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Extrai mensagem legível de erros de sync para exibir no painel.
    /// </summary>
    private static string ExtrairMensagemErro(Exception ex, SyncOperacao op)
    {
        var inner = ex.InnerException;

        // FK violation (23503)
        if (inner is Npgsql.PostgresException pgEx && pgEx.SqlState == "23503")
        {
            return $"[{op.Operacao}] {op.Tabela} Id={op.RegistroId}: FK inválida ({pgEx.ConstraintName}). " +
                   $"O registro referenciado não existe neste PC. Verifique se o registro pai foi sincronizado primeiro.";
        }

        // PK/Unique violation (23505)
        if (inner is Npgsql.PostgresException pgEx2 && pgEx2.SqlState == "23505")
        {
            return $"[{op.Operacao}] {op.Tabela} Id={op.RegistroId}: Registro duplicado ({pgEx2.ConstraintName}). " +
                   $"Já existe um registro com o mesmo valor único neste PC.";
        }

        // Genérico
        return $"[{op.Operacao}] {op.Tabela} Id={op.RegistroId}: {inner?.Message ?? ex.Message}";
    }

    // Métodos BuscarPorId, LimparNavigations, ResolverTipo e _tiposPorTabela
    // foram movidos para SyncApplicator (classe compartilhada).

    private async Task<string?> ObterToken(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_tokenCache) && DateTime.UtcNow < _tokenExpiracao.AddMinutes(-5))
            return _tokenCache;

        try
        {
            var chave = _config["SistemaKey"] ?? "ZulexPharma2026!";
            var data = DateTime.UtcNow.ToString("yyyyMMdd");
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data + chave));
            var senha = Convert.ToHexString(hash)[..8].ToLower();

            var loginJson = JsonSerializer.Serialize(new { login = "SISTEMA", senha }, _jsonOpts);
            var content = new StringContent(loginJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_urlCentral}/api/auth/login", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                Log.Warning("Sync token: login SISTEMA falhou ({Status}): {Body}", response.StatusCode, errBody);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            var result = JsonDocument.Parse(body);
            var token = result.RootElement.GetProperty("data").GetProperty("token").GetString();
            var exp = result.RootElement.GetProperty("data").GetProperty("expiracao").GetDateTime();

            _tokenCache = token;
            _tokenExpiracao = exp;
            return token;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Sync: falha ao autenticar no central");
            return null;
        }
    }
}

internal class SyncReceberResponse
{
    public List<SyncOperacao>? Data { get; set; }
}

internal class SyncOperacao
{
    public long Id { get; set; }
    public string Tabela { get; set; } = "";
    public string Operacao { get; set; } = "";
    public long RegistroId { get; set; }
    public string? RegistroCodigo { get; set; }
    public string? DadosJson { get; set; }
    public long FilialOrigemId { get; set; }
    public DateTime CriadoEm { get; set; }
}
