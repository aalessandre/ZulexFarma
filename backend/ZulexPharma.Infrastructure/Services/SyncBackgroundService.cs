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
                if (string.IsNullOrEmpty(token)) { FalhasConsecutivas++; }
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
            .Where(f => !f.Enviado && f.FilialOrigemId == _filialCodigo)
            .OrderBy(f => f.Id)
            .Take(lote)
            .ToListAsync(ct);

        PendentesEnvio = await db.SyncFila.CountAsync(f => !f.Enviado && f.FilialOrigemId == _filialCodigo, ct);

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
        var lastSuccessId = ultimoId;

        foreach (var op in resultado.Data)
        {
            try
            {
                // Resolver tipo da entidade pela tabela
                var tipo = ResolverTipo(op.Tabela);
                if (tipo == null) continue;

                if (op.Operacao == "D")
                {
                    // DELETE: buscar por Codigo
                    var existente = await BuscarPorCodigo(db, tipo, op.RegistroCodigo);
                    if (existente != null)
                    {
                        db.Remove(existente);
                        aplicados++;
                        if (op.Id > lastSuccessId) lastSuccessId = op.Id;
                    }
                }
                else if (op.Operacao == "I")
                {
                    // INSERT: verificar se já existe pelo Codigo
                    var existente = await BuscarPorCodigo(db, tipo, op.RegistroCodigo);
                    if (existente == null && op.DadosJson != null)
                    {
                        var entidade = (Domain.Entities.BaseEntity?)JsonSerializer.Deserialize(op.DadosJson, tipo, _jsonOpts);
                        if (entidade != null)
                        {
                            entidade.Id = 0; // EF gera novo Id local
                            db.Add(entidade);
                            aplicados++;
                            if (op.Id > lastSuccessId) lastSuccessId = op.Id;
                        }
                    }
                }
                else if (op.Operacao == "U")
                {
                    // UPDATE: buscar por Codigo e atualizar
                    var existente = await BuscarPorCodigo(db, tipo, op.RegistroCodigo);
                    if (existente != null && op.DadosJson != null)
                    {
                        var entidade = (Domain.Entities.BaseEntity?)JsonSerializer.Deserialize(op.DadosJson, tipo, _jsonOpts);
                        if (entidade != null)
                        {
                            entidade.Id = existente.Id; // Manter Id local
                            db.Entry(existente).CurrentValues.SetValues(entidade);
                            aplicados++;
                            if (op.Id > lastSuccessId) lastSuccessId = op.Id;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Sync PULL: erro ao aplicar op {Op} em {Tabela} codigo {Codigo}",
                    op.Operacao, op.Tabela, op.RegistroCodigo);
            }
        }

        var batchFailed = false;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            Log.Warning(ex, "Sync PULL: erro ao salvar batch");
            batchFailed = true;
            // Detach entries com erro
            foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged))
                entry.State = EntityState.Detached;
        }

        db.AplicandoSync = false;

        // Only update progress if batch succeeded
        if (!batchFailed)
        {
            ultimoId = lastSuccessId;
            if (ultimoIdConfig == null)
            {
                db.Configuracoes.Add(new Domain.Entities.Configuracao { Chave = "sync.ultimo.id.recebido", Valor = ultimoId.ToString() });
            }
            else
            {
                ultimoIdConfig.Valor = ultimoId.ToString();
            }
            await db.SaveChangesAsync(ct);
        }

        if (aplicados > 0) Log.Information("Sync PULL: {Count} operações aplicadas", aplicados);
    }

    private static async Task<Domain.Entities.BaseEntity?> BuscarPorCodigo(AppDbContext db, Type tipo, string? codigo)
    {
        if (string.IsNullOrEmpty(codigo)) return null;
        var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(tipo);
        var dbSet = (IQueryable<Domain.Entities.BaseEntity>)method.Invoke(db, null)!;
        return await dbSet.FirstOrDefaultAsync(e => e.Codigo == codigo);
    }

    private static readonly Dictionary<string, Type> _tiposPorTabela = new()
    {
        ["Filiais"] = typeof(Domain.Entities.Filial),
        ["Pessoas"] = typeof(Domain.Entities.Pessoa),
        ["PessoasContato"] = typeof(Domain.Entities.PessoaContato),
        ["PessoasEndereco"] = typeof(Domain.Entities.PessoaEndereco),
        ["Colaboradores"] = typeof(Domain.Entities.Colaborador),
        ["Fornecedores"] = typeof(Domain.Entities.Fornecedor),
        ["Usuarios"] = typeof(Domain.Entities.Usuario),
        ["UsuariosGrupos"] = typeof(Domain.Entities.GrupoUsuario),
        ["UsuariosGruposPermissao"] = typeof(Domain.Entities.GrupoPermissao),
        ["UsuarioFilialGrupos"] = typeof(Domain.Entities.UsuarioFilialGrupo),
        ["Fabricantes"] = typeof(Domain.Entities.Fabricante),
        ["Substancias"] = typeof(Domain.Entities.Substancia),
        ["GruposPrincipais"] = typeof(Domain.Entities.GrupoPrincipal),
        ["GruposProdutos"] = typeof(Domain.Entities.GrupoProduto),
        ["SubGrupos"] = typeof(Domain.Entities.SubGrupo),
        ["Secoes"] = typeof(Domain.Entities.Secao),
        ["LogsAcao"] = typeof(Domain.Entities.LogAcao),
        ["LogsErro"] = typeof(Domain.Entities.LogErro),
    };

    private static Type? ResolverTipo(string tabela) => _tiposPorTabela.GetValueOrDefault(tabela);

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

            if (!response.IsSuccessStatusCode) return null;

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
