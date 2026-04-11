using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class IbptService
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public IbptService(AppDbContext db, IHttpClientFactory httpFactory)
    {
        _db = db;
        _httpFactory = httpFactory;
    }

    // ── API IBPT ────────────────────────────────────────────────────

    /// <summary>Sincroniza todos os NCMs dos produtos via API do IBPT.</summary>
    public async Task<IbptSyncResult> SincronizarViaApiAsync(CancellationToken ct = default)
    {
        var token = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.token", ct))?.Valor;
        var cnpj = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.cnpj", ct))?.Valor;
        var ufConfig = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.uf", ct))?.Valor;

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Token IBPT não configurado. Acesse Configurações > Fiscal.");
        if (string.IsNullOrWhiteSpace(cnpj))
            throw new InvalidOperationException("CNPJ IBPT não configurado. Acesse Configurações > Fiscal.");

        var uf = !string.IsNullOrWhiteSpace(ufConfig) ? ufConfig.ToUpper() : "PR";
        var cnpjLimpo = cnpj.Replace(".", "").Replace("/", "").Replace("-", "");

        // Buscar NCMs distintos dos produtos
        var ncms = await _db.Set<Ncm>()
            .Select(n => new { Codigo = n.CodigoNcm, n.Descricao })
            .ToListAsync(ct);

        if (ncms.Count == 0)
            throw new InvalidOperationException("Nenhum NCM cadastrado no sistema.");

        var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        var registrosNovos = new List<IbptTax>();
        int erros = 0;
        int consultados = 0;

        foreach (var ncm in ncms)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var ncmCodigo = ncm.Codigo.Replace(".", "").PadRight(8, '0');
                if (ncmCodigo.Length > 8) ncmCodigo = ncmCodigo[..8];

                var url = $"https://apidoni.ibpt.org.br/api/v1/produtos?" +
                          $"token={Uri.EscapeDataString(token)}" +
                          $"&cnpj={cnpjLimpo}" +
                          $"&codigo={ncmCodigo}" +
                          $"&uf={uf}" +
                          $"&ex=0" +
                          $"&descricao={Uri.EscapeDataString(ncm.Descricao ?? "PRODUTO")}";

                var response = await client.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    erros++;
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                var dados = JsonSerializer.Deserialize<IbptApiResponse>(json, JsonOpts);
                if (dados == null) { erros++; continue; }

                registrosNovos.Add(new IbptTax
                {
                    Ncm = ncmCodigo,
                    Ex = dados.EX.ToString(),
                    Tipo = 0,
                    Descricao = dados.Descricao ?? ncm.Descricao ?? "",
                    AliqNacional = dados.Nacional,
                    AliqImportado = dados.Importado,
                    AliqEstadual = dados.Estadual,
                    AliqMunicipal = dados.Municipal,
                    VigenciaInicio = ParseDataApi(dados.VigenciaInicio),
                    VigenciaFim = ParseDataApi(dados.VigenciaFim),
                    Chave = dados.Chave ?? "",
                    Versao = dados.Versao ?? "",
                    Fonte = dados.Fonte ?? "IBPT",
                    Uf = uf
                });

                consultados++;

                // Rate limit: pequena pausa entre chamadas
                await Task.Delay(100, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                erros++;
                Log.Debug("IBPTax API: Erro ao consultar NCM {Ncm}: {Msg}", ncm.Codigo, ex.Message);
            }
        }

        if (registrosNovos.Count == 0)
            throw new InvalidOperationException($"Nenhum registro retornado pela API. {erros} erro(s).");

        // Substituir registros da UF
        await _db.IbptTaxes.Where(x => x.Uf == uf).ExecuteDeleteAsync(ct);
        foreach (var lote in registrosNovos.Chunk(5000))
        {
            _db.IbptTaxes.AddRange(lote);
            await _db.SaveChangesAsync(ct);
        }

        var versao = registrosNovos.FirstOrDefault()?.Versao ?? "";
        var vigencia = registrosNovos.Max(r => r.VigenciaFim);

        await SalvarConfig("ibpt.versao", versao);
        await SalvarConfig("ibpt.uf", uf);
        await SalvarConfig("ibpt.data.importacao", DateTime.UtcNow.ToString("o"));
        await SalvarConfig("ibpt.vigencia.fim", vigencia.ToString("yyyy-MM-dd"));
        await SalvarConfig("ibpt.total.registros", registrosNovos.Count.ToString());
        await _db.SaveChangesAsync(ct);

        Log.Information("IBPTax API: Sincronizado {Total} NCMs, versão {Versao}, UF {Uf}, {Erros} erros",
            registrosNovos.Count, versao, uf, erros);

        return new IbptSyncResult
        {
            TotalSincronizado = registrosNovos.Count,
            TotalNcms = ncms.Count,
            Erros = erros,
            Versao = versao,
            VigenciaFim = vigencia
        };
    }

    private static DateTime ParseDataApi(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return DateTime.MinValue;
        if (DateTime.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1)) return d1;
        if (DateTime.TryParse(s, out var d2)) return d2;
        return DateTime.MinValue;
    }

    // ── CSV Import (mantido como fallback) ──────────────────────────

    public async Task<IbptImportResult> ImportarCsvAsync(string csvContent, string? uf = null)
    {
        var linhas = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (linhas.Length < 2)
            throw new ArgumentException("CSV vazio ou inválido.");

        var registros = new List<IbptTax>();
        int erros = 0;
        int inicio = linhas[0].Contains("codigo") || linhas[0].Contains("NCM") ? 1 : 0;

        for (int i = inicio; i < linhas.Length; i++)
        {
            try
            {
                var cols = linhas[i].Trim().Split(';');
                if (cols.Length < 12) continue;

                var ncm = cols[0].Trim().Replace(".", "");
                var ex = cols[1].Trim();
                var tipo = int.TryParse(cols[2].Trim(), out var t) ? t : 0;
                var descricao = cols[3].Trim();

                if (!string.IsNullOrEmpty(uf) && tipo == 0)
                {
                    registros.Add(new IbptTax
                    {
                        Ncm = ncm, Ex = ex, Tipo = tipo, Descricao = descricao,
                        AliqNacional = ParseDecimal(cols[4]), AliqImportado = ParseDecimal(cols[5]),
                        AliqEstadual = ParseDecimal(cols[6]), AliqMunicipal = ParseDecimal(cols[7]),
                        VigenciaInicio = ParseData(cols[8]), VigenciaFim = ParseData(cols[9]),
                        Chave = cols[10].Trim(), Versao = cols[11].Trim(),
                        Fonte = cols.Length > 12 ? cols[12].Trim() : "IBPT",
                        Uf = uf.ToUpper()
                    });
                }
            }
            catch { erros++; }
        }

        if (registros.Count == 0)
            throw new ArgumentException("Nenhum registro válido encontrado no CSV.");

        if (!string.IsNullOrEmpty(uf))
            await _db.IbptTaxes.Where(x => x.Uf == uf.ToUpper()).ExecuteDeleteAsync();
        else
            await _db.IbptTaxes.ExecuteDeleteAsync();

        foreach (var lote in registros.Chunk(5000))
        {
            _db.IbptTaxes.AddRange(lote);
            await _db.SaveChangesAsync();
        }

        var versaoImportada = registros.FirstOrDefault()?.Versao ?? "";
        var vigencia = registros.FirstOrDefault()?.VigenciaFim;

        await SalvarConfig("ibpt.versao", versaoImportada);
        await SalvarConfig("ibpt.uf", uf?.ToUpper() ?? "");
        await SalvarConfig("ibpt.data.importacao", DateTime.UtcNow.ToString("o"));
        await SalvarConfig("ibpt.vigencia.fim", vigencia?.ToString("yyyy-MM-dd") ?? "");
        await SalvarConfig("ibpt.total.registros", registros.Count.ToString());
        await _db.SaveChangesAsync();

        return new IbptImportResult
        {
            TotalImportado = registros.Count, Erros = erros,
            Versao = versaoImportada, VigenciaFim = vigencia
        };
    }

    // ── Consultas ───────────────────────────────────────────────────

    public async Task<IbptTax?> BuscarPorNcmAsync(string ncm, string uf)
    {
        ncm = ncm.Replace(".", "").PadRight(8, '0');
        if (ncm.Length > 8) ncm = ncm[..8];
        return await _db.IbptTaxes
            .Where(x => x.Ncm == ncm && x.Uf == uf && x.Tipo == 0)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<string, IbptTax>> BuscarPorNcmsAsync(IEnumerable<string> ncms, string uf)
    {
        var ncmsNormalizados = ncms.Select(n => n.Replace(".", "").PadRight(8, '0').Substring(0, 8)).Distinct().ToList();
        var registros = await _db.IbptTaxes
            .Where(x => ncmsNormalizados.Contains(x.Ncm) && x.Uf == uf && x.Tipo == 0)
            .ToListAsync();
        return registros.GroupBy(r => r.Ncm).ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<IbptStatusResult> ObterStatusAsync()
    {
        var versao = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.versao"))?.Valor;
        var uf = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.uf"))?.Valor;
        var dataImport = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.data.importacao"))?.Valor;
        var vigFim = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.vigencia.fim"))?.Valor;
        var total = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.total.registros"))?.Valor;

        return new IbptStatusResult
        {
            Versao = versao, Uf = uf,
            DataImportacao = DateTime.TryParse(dataImport, out var d) ? d : null,
            VigenciaFim = DateTime.TryParse(vigFim, out var v) ? v : null,
            TotalRegistros = int.TryParse(total, out var t) ? t : 0,
            Expirada = DateTime.TryParse(vigFim, out var vf) && vf < DateTime.UtcNow
        };
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private async Task SalvarConfig(string chave, string valor)
    {
        var cfg = await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == chave);
        if (cfg != null) cfg.Valor = valor;
        else _db.Set<Configuracao>().Add(new Configuracao { Chave = chave, Valor = valor });
    }

    private static decimal ParseDecimal(string s)
    {
        s = s.Trim();
        if (string.IsNullOrEmpty(s)) return 0;
        s = s.Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static DateTime ParseData(string s)
    {
        s = s.Trim();
        if (DateTime.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1)) return d1;
        if (DateTime.TryParse(s, out var d2)) return d2;
        return DateTime.MinValue;
    }
}

// ── DTOs ────────────────────────────────────────────────────────────

public class IbptApiResponse
{
    public string? Codigo { get; set; }
    public string? UF { get; set; }
    public int EX { get; set; }
    public string? Descricao { get; set; }
    public decimal Nacional { get; set; }
    public decimal Estadual { get; set; }
    public decimal Importado { get; set; }
    public decimal Municipal { get; set; }
    public string? Tipo { get; set; }
    public string? VigenciaInicio { get; set; }
    public string? VigenciaFim { get; set; }
    public string? Chave { get; set; }
    public string? Versao { get; set; }
    public string? Fonte { get; set; }
}

public class IbptSyncResult
{
    public int TotalSincronizado { get; set; }
    public int TotalNcms { get; set; }
    public int Erros { get; set; }
    public string Versao { get; set; } = "";
    public DateTime? VigenciaFim { get; set; }
}

public class IbptImportResult
{
    public int TotalImportado { get; set; }
    public int Erros { get; set; }
    public string Versao { get; set; } = "";
    public DateTime? VigenciaFim { get; set; }
}

public class IbptStatusResult
{
    public string? Versao { get; set; }
    public string? Uf { get; set; }
    public DateTime? DataImportacao { get; set; }
    public DateTime? VigenciaFim { get; set; }
    public int TotalRegistros { get; set; }
    public bool Expirada { get; set; }
}
