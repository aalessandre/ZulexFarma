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
        var lastSuccessId = ultimoId;

        // Ordenar operações por dependência: tabelas pai primeiro em I/U, filhas primeiro em D
        var ordenadas = resultado.Data
            .OrderBy(op => op.Operacao == "D" ? -GetOrdemTabela(op.Tabela) : GetOrdemTabela(op.Tabela))
            .ThenBy(op => op.Id)
            .ToList();

        // Processar cada operação individualmente com SaveChanges separado
        // para respeitar a ordem de dependência entre tabelas
        foreach (var op in ordenadas)
        {
            try
            {
                var tipo = ResolverTipo(op.Tabela);
                if (tipo == null) continue;

                if (op.Operacao == "D")
                {
                    var existente = await BuscarPorCodigo(db, tipo, op.RegistroCodigo);
                    if (existente != null)
                    {
                        db.Remove(existente);
                        await db.SaveChangesAsync(ct);
                        aplicados++;
                        if (op.Id > lastSuccessId) lastSuccessId = op.Id;
                    }
                }
                else if (op.Operacao == "I")
                {
                    var existente = await BuscarPorCodigo(db, tipo, op.RegistroCodigo);
                    if (existente == null && op.DadosJson != null)
                    {
                        var entidade = (Domain.Entities.BaseEntity?)JsonSerializer.Deserialize(op.DadosJson, tipo, _jsonOpts);
                        if (entidade != null)
                        {
                            entidade.Id = 0;
                            LimparNavigations(db, entidade);
                            await ResolverFksLocais(db, entidade, op.DadosJson);
                            db.Add(entidade);
                            await db.SaveChangesAsync(ct);
                            aplicados++;
                            if (op.Id > lastSuccessId) lastSuccessId = op.Id;
                        }
                    }
                }
                else if (op.Operacao == "U")
                {
                    var existente = await BuscarPorCodigo(db, tipo, op.RegistroCodigo);
                    if (existente != null && op.DadosJson != null)
                    {
                        var entidade = (Domain.Entities.BaseEntity?)JsonSerializer.Deserialize(op.DadosJson, tipo, _jsonOpts);
                        if (entidade != null)
                        {
                            entidade.Id = existente.Id;
                            LimparNavigations(db, entidade);
                            await ResolverFksLocais(db, entidade, op.DadosJson);
                            db.Entry(existente).CurrentValues.SetValues(entidade);
                            await db.SaveChangesAsync(ct);
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
                // Detach entries com erro para não poluir próximas operações
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged))
                    entry.State = EntityState.Detached;
            }
        }

        db.AplicandoSync = false;

        // Atualizar progresso
        if (aplicados > 0 || lastSuccessId > ultimoId)
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

    /// <summary>
    /// Ordem de dependência: tabelas pai têm número menor (processadas primeiro em INSERT).
    /// </summary>
    private static int GetOrdemTabela(string tabela) => tabela switch
    {
        "Filiais" => 0,
        "UsuariosGrupos" => 0,
        "Pessoas" => 1,
        "Colaboradores" => 2,
        "Fornecedores" => 2,
        "PessoasContato" => 2,
        "PessoasEndereco" => 2,
        "Usuarios" => 3,
        "UsuarioFilialGrupos" => 4,
        "UsuariosGruposPermissao" => 1,
        "LogsAcao" => 5,
        _ => 10
    };

    /// <summary>
    /// Resolve FKs que referenciam outras entidades: troca o Id da origem pelo Id local
    /// usando o Codigo para lookup. Ex: Colaborador.PessoaId vem com Id=5 da origem,
    /// mas no banco local a Pessoa com aquele Codigo tem Id=12.
    /// </summary>
    private static async Task ResolverFksLocais(AppDbContext db, Domain.Entities.BaseEntity entidade, string dadosJson)
    {
        var doc = JsonDocument.Parse(dadosJson);
        var root = doc.RootElement;

        // Mapa de propriedades FK -> tabela alvo
        var fkMap = new Dictionary<string, string>
        {
            ["PessoaId"] = "Pessoas",
            ["ColaboradorId"] = "Colaboradores",
            ["GrupoUsuarioId"] = "UsuariosGrupos",
            ["FilialId"] = "Filiais",
            ["UsuarioId"] = "Usuarios",
            ["UsuarioLiberouId"] = "Usuarios"
        };

        var tipo = entidade.GetType();
        foreach (var (fkProp, tabelaAlvo) in fkMap)
        {
            var prop = tipo.GetProperty(fkProp);
            if (prop == null) continue;

            // Ler o valor atual da FK (vem com Id da origem)
            var fkValue = prop.GetValue(entidade);
            if (fkValue == null) continue;

            long fkId;
            if (prop.PropertyType == typeof(long))
                fkId = (long)fkValue;
            else if (prop.PropertyType == typeof(long?))
                fkId = ((long?)fkValue).Value;
            else
                continue;

            if (fkId == 0) continue;

            // Precisamos achar o Codigo original da entidade referenciada.
            // O DadosJson do sync NÃO traz o Codigo da FK, mas o registro
            // referenciado já deve ter sido sincronizado. Procuramos por Id
            // na SyncFila para achar o Codigo, ou consultamos direto.
            // Abordagem pragmática: buscar entidade local com mesmo Id.
            // Se não existir, buscar pela FK via Codigo usando a fila.

            var tipoAlvo = ResolverTipo(tabelaAlvo);
            if (tipoAlvo == null) continue;

            // Primeiro: verificar se o Id local coincide (caso comum quando ambos iniciaram do zero)
            var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(tipoAlvo);
            var dbSet = (IQueryable<Domain.Entities.BaseEntity>)method.Invoke(db, null)!;

            // Buscar por Id original (pode coincidir)
            var porId = await dbSet.FirstOrDefaultAsync(e => e.Id == fkId);
            if (porId != null) continue; // Id coincide, FK está correta

            // Não coincide: precisamos achar o registro local pelo Codigo.
            // Buscar o Codigo original consultando a SyncFila
            var syncOp = await db.SyncFila
                .Where(s => s.Tabela == tabelaAlvo && s.RegistroId == fkId && s.Operacao == "I")
                .FirstOrDefaultAsync();

            if (syncOp?.RegistroCodigo != null)
            {
                var local = await dbSet.FirstOrDefaultAsync(e => e.Codigo == syncOp.RegistroCodigo);
                if (local != null)
                {
                    prop.SetValue(entidade, local.Id);
                    continue;
                }
            }

            // Fallback: buscar no receber - o registro alvo pode ter acabado de ser inserido neste batch
            // Nesse caso, buscar pelo Codigo que veio no JSON original do registro alvo
            // Não temos essa info aqui, mas como processamos em ordem de dependência, o pai já foi salvo
            // Tentar buscar qualquer registro com aquele Codigo na tabela alvo via SyncFila recebida
            var recebido = await db.Set<Domain.Entities.SyncFila>()
                .Where(s => s.Tabela == tabelaAlvo && s.RegistroId == fkId && !s.Enviado)
                .FirstOrDefaultAsync();

            if (recebido?.RegistroCodigo != null)
            {
                var local = await dbSet.FirstOrDefaultAsync(e => e.Codigo == recebido.RegistroCodigo);
                if (local != null)
                    prop.SetValue(entidade, local.Id);
            }
        }
    }

    /// <summary>
    /// Anula todas as navigation properties para evitar que db.Add() tente trackear o grafo inteiro.
    /// Mantém apenas as FKs (ex: PessoaId), removendo os objetos (ex: Pessoa = null).
    /// </summary>
    private static void LimparNavigations(AppDbContext db, object entidade)
    {
        var entityType = db.Model.FindEntityType(entidade.GetType());
        if (entityType == null) return;

        foreach (var nav in entityType.GetNavigations())
        {
            var prop = nav.PropertyInfo;
            if (prop != null && prop.CanWrite)
                prop.SetValue(entidade, null);
        }

        foreach (var nav in entityType.GetSkipNavigations())
        {
            var prop = nav.PropertyInfo;
            if (prop != null && prop.CanWrite)
                prop.SetValue(entidade, null);
        }
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
