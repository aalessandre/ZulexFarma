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
    private readonly int _noCodigo;
    private readonly string _filiais;
    private readonly string _urlCentral;
    private readonly HttpClient _httpClient;

    private string? _tokenCache;
    private DateTime _tokenExpiracao = DateTime.MinValue;
    private DateTime _ultimaPurgaLapides = DateTime.MinValue;

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
        _noCodigo = int.TryParse(config["No:Codigo"] ?? config["Filial:Codigo"], out var f) ? f : 0;
        // Fase 3b: filiais que ESTE no atende (escopo por-filial no PULL). Vazio = so' GLOBAL.
        _filiais = config["No:Filiais"] ?? "";
        _urlCentral = config["Sync:UrlCentral"]?.TrimEnd('/') ?? "";

        // TLS: valida o certificado da central por padrao (a Railway tem cert publico valido).
        // So' aceita cert inseguro/self-signed quando Sync:AceitarCertInseguro=true (dev/central local).
        var aceitarCertInseguro = config["Sync:AceitarCertInseguro"]?.ToLower() == "true";
        var handler = new HttpClientHandler();
        if (aceitarCertInseguro)
        {
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            Log.Warning("Sync: validacao TLS DESABILITADA (Sync:AceitarCertInseguro=true) — use so' em dev/central local.");
        }
        _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Fase 0: early-return HONESTO por modo. Antes, No:Codigo=0 (hub) caia no mesmo if de
        // "desabilitado" e o log mandava "configure No:Codigo" justamente quando 0 era o valor certo.
        var modo = Data.NoDeployment.LerModoLeniente(_config);
        if (modo == Data.NoModo.Hub || _noCodigo == 0)
        {
            Log.Information("Sync v2: modo Hub (no 0) — loop de transporte nao roda POR DESIGN. " +
                "O hub recebe via /api/sync/enviar e serve /api/sync/receber.");
            return;
        }
        if (modo == Data.NoModo.StandaloneCloud)
        {
            Log.Information("Sync v2: modo StandaloneCloud — sem replicacao (captura de outbox desligada, transporte nao roda).");
            return;
        }
        if (!_habilitado)
        {
            Log.Warning("Sync v2: modo Edge com Sync:Habilitado=false — captura de outbox LIGADA, transporte " +
                "DESLIGADO. O backlog acumula na SyncFila ate' o transporte ser ligado.");
            return;
        }
        if (string.IsNullOrEmpty(_urlCentral))
        {
            // O boot (NoDeployment.Resolver) ja' barra essa combinacao; aqui e' so' defesa em profundidade.
            Log.Error("Sync v2: modo Edge habilitado SEM Sync:UrlCentral — transporte morto por config invalida.");
            UltimoStatus = "CONFIG";
            return;
        }

        Log.Information("Sync v2 iniciado. Filial: {Filial} | Central: {Url} | Intervalo: {Int}s",
            _noCodigo, _urlCentral, _intervaloSegundos);
        Rodando = true;

        await Task.Delay(10_000, stoppingToken);

        var falhasTransporte = 0; // consecutivas de TRANSPORTE (token/HTTP) — dirige o backoff (quarentena NAO conta)

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var token = await ObterToken(stoppingToken);
                if (string.IsNullOrEmpty(token))
                {
                    FalhasConsecutivas++;
                    falhasTransporte++;
                    Log.Warning("Sync: falha ao obter token da central ({Falhas} consecutivas)", FalhasConsecutivas);
                }
                else
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    await Enviar(stoppingToken);
                    await Receber(stoppingToken);
                    FalhasConsecutivas = 0;
                    falhasTransporte = 0; // transporte OK — erros de quarentena (dentro de Receber) NAO contam
                }
                UltimaExecucao = DateTime.UtcNow;
                UltimoStatus = FalhasConsecutivas > 0 ? "ERRO" : "OK";
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro no ciclo sync v2");
                FalhasConsecutivas++;
                falhasTransporte++;
                UltimoStatus = "ERRO";
            }

            // Faxina LOCAL: nao depende de rede nem de token, entao roda fora do if do transporte
            // (a lapide cresce mesmo com a central fora do ar). Tem try/catch proprio.
            await PurgarLapidesSePreciso(stoppingToken);

            await Task.Delay(CalcularDelayMs(falhasTransporte), stoppingToken);
        }
        Rodando = false;
    }

    /// <summary>
    /// Faxina das lapides fora da retencao, com CADENCIA PROPRIA (1x/hora).
    /// Ficava dentro do Receber() e funcionava por ACIDENTE: naquela epoca a lapide so' nascia ao APLICAR um
    /// "D" REMOTO, logo se nasceu lapide o pull tinha trazido algo e o fluxo chegava na faxina. Desde que o
    /// delete LOCAL passou a cravar lapide (outbox), o nascimento se DESACOPLOU do pull: num no de loja unica
    /// o /receber volta SEMPRE vazio (anti-eco filtra as ops do proprio no), o Receber sai no early return e
    /// a faxina NUNCA rodaria — as lapides dos deletes locais cresceriam pra sempre, justo no no que as cria.
    /// LIMITACAO CONHECIDA: no com Sync:Habilitado=false nao roda este laco, entao nada purga — mas ali o
    /// outbox tambem enche a SyncFila sem ninguem esvaziar (propriedade pre-existente, nao regressao daqui).
    /// </summary>
    private async Task PurgarLapidesSePreciso(CancellationToken ct)
    {
        if (DateTime.UtcNow - _ultimaPurgaLapides < TimeSpan.FromHours(1)) return;
        _ultimaPurgaLapides = DateTime.UtcNow;
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // ExecuteDelete: nao passa por SaveChanges, entao nao gera op de sync da propria faxina.
            var n = await SyncApplicator.PurgarTombstonesAsync(db, ct);
            if (n > 0)
                Log.Information("Sync: {N} lapide(s) fora da retencao de {Dias}d purgada(s)",
                    n, SyncApplicator.SyncTombstoneRetencaoDias);
        }
        catch (Exception ex) { Log.Warning(ex, "Sync: falha ao purgar lapides (nao fatal)"); }
    }

    /// <summary>
    /// Delay ate' o proximo ciclo: intervalo normal quando saudavel; backoff exponencial com teto + jitter
    /// enquanto o TRANSPORTE falha (central em queda) — evita martelar. Quarentena NAO entra aqui.
    /// </summary>
    private int CalcularDelayMs(int falhasTransporte)
    {
        var baseSeg = _intervaloSegundos;
        if (falhasTransporte <= 0) return baseSeg * 1000;

        var tetoSeg = int.TryParse(_config["Sync:BackoffMaxSegundos"], out var bm) ? bm : 300;
        var expo = baseSeg * Math.Pow(2, Math.Min(falhasTransporte, 6)); // 2^6 = 64x, capado no teto
        var delaySeg = (int)Math.Min(expo, tetoSeg);
        var jitter = Random.Shared.Next(0, Math.Max(1, delaySeg / 4)); // ate' +25% p/ dessincronizar os nos
        return (delaySeg + jitter) * 1000;
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
            p.DadosJson, p.NoOrigemId, p.FilialDonoId, p.CriadoEm,
            // Fase 4b: identidade global da op (nasceu no outbox deste no) — a central usa como chave de
            // idempotencia, entao um reenvio (PUSH ok + resposta perdida) nao duplica a redistribuicao.
            p.OpUid
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
            $"{_urlCentral}/api/sync/receber?filialId={_noCodigo}&filiais={Uri.EscapeDataString(_filiais)}&ultimoId={ultimoId}", ct);

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
                var res = await SyncApplicator.AplicarOperacaoAsync(
                    db, op.Tabela, op.Operacao, op.RegistroId, op.DadosJson, op.CriadoEm, op.NoOrigemId, ct);

                switch (res)
                {
                    case ResultadoSync.Aplicado:
                        RegistrarRecebido(db, op);
                        aplicados++;
                        break;
                    case ResultadoSync.Idempotente:
                    case ResultadoSync.Stale:
                        break; // ja' no estado alvo / versao velha descartada por LWW — nada a fazer
                    default: // PrecisaRetry | Conflito | TipoDesconhecido -> quarentena p/ retry
                        await SyncApplicator.QuarentenarAsync(db, op.Tabela, op.Operacao, op.RegistroId,
                            op.DadosJson, op.CriadoEm, op.NoOrigemId, res.ToString(), null, ct);
                        erros++;
                        break;
                }
            }
            catch (Exception ex)
            {
                SyncApplicator.Desanexar(db);
                await SyncApplicator.QuarentenarAsync(db, op.Tabela, op.Operacao, op.RegistroId,
                    op.DadosJson, op.CriadoEm, op.NoOrigemId, "Erro", ExtrairMensagemErro(ex, op), ct);
                erros++;
            }

            // Ponteiro avanca SEMPRE: a op nao se perde (aplicada, descartada por LWW, ou na quarentena p/ retry).
            if (op.Id > lastSuccessId) lastSuccessId = op.Id;
        }

        // Avançar o ponteiro (nao reprocessa): falhas nao se perdem — vao pra SyncQuarentena e sao
        // reprocessadas na drenagem abaixo. Manter AplicandoSync = true (nao gera SyncFila).
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

        // Drenar a quarentena (retry) — resolve, p.ex., "U chegou antes do I" depois que o I entra.
        var resolvidosQ = await SyncApplicator.DrenarQuarentenaAsync(db, ct);
        if (resolvidosQ > 0) Log.Information("Sync PULL: {N} itens da quarentena resolvidos", resolvidosQ);

        // (A faxina das lapides saiu daqui — virou PurgarLapidesSePreciso, com cadencia propria no laco.
        //  Motivo no XML doc de la': o delete LOCAL agora crava lapide, entao o nascimento delas nao tem
        //  mais relacao nenhuma com o pull, e este metodo sai cedo quando o pull volta vazio.)

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
            NoOrigemId = op.NoOrigemId,
            FilialDonoId = op.FilialDonoId,
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
            var chave = _config["SistemaKey"];
            if (string.IsNullOrWhiteSpace(chave))
            {
                Log.Error("Sync: 'SistemaKey' nao configurada — o no nao autentica no hub. Defina um segredo por deployment (sem fallback compartilhado).");
                return null;
            }
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
    public long NoOrigemId { get; set; }
    public long? FilialDonoId { get; set; }
    public DateTime CriadoEm { get; set; }
}
