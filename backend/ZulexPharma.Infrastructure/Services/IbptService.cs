using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class IbptService
{
    private readonly AppDbContext _db;

    public IbptService(AppDbContext db) => _db = db;

    /// <summary>Importa CSV do IBPTax para o banco. Substitui todos os registros.</summary>
    /// <param name="csvContent">Conteúdo do CSV (separador ;)</param>
    /// <param name="uf">UF para filtrar (ex: "PR"). Se null, importa tudo.</param>
    /// <returns>Quantidade de registros importados.</returns>
    public async Task<IbptImportResult> ImportarCsvAsync(string csvContent, string? uf = null)
    {
        var linhas = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (linhas.Length < 2)
            throw new ArgumentException("CSV vazio ou inválido.");

        var registros = new List<IbptTax>();
        int erros = 0;

        // Pular header se existir
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
                var aliqNacional = ParseDecimal(cols[4]);
                var aliqImportado = ParseDecimal(cols[5]);
                var aliqEstadual = ParseDecimal(cols[6]);
                var aliqMunicipal = ParseDecimal(cols[7]);
                var vigInicio = ParseData(cols[8]);
                var vigFim = ParseData(cols[9]);
                var chave = cols[10].Trim();
                var versao = cols[11].Trim();
                var fonte = cols.Length > 12 ? cols[12].Trim() : "IBPT";

                // Filtrar por UF se especificado (a tabela IBPTax tem alíquotas estaduais específicas por UF)
                // O campo UF não está no CSV padrão — usamos o nome do arquivo ou parâmetro
                if (!string.IsNullOrEmpty(uf) && tipo == 0) // Tipo 0 = Nacional (mais relevante)
                {
                    registros.Add(new IbptTax
                    {
                        Ncm = ncm, Ex = ex, Tipo = tipo, Descricao = descricao,
                        AliqNacional = aliqNacional, AliqImportado = aliqImportado,
                        AliqEstadual = aliqEstadual, AliqMunicipal = aliqMunicipal,
                        VigenciaInicio = vigInicio, VigenciaFim = vigFim,
                        Chave = chave, Versao = versao, Fonte = fonte,
                        Uf = uf.ToUpper()
                    });
                }
                else
                {
                    registros.Add(new IbptTax
                    {
                        Ncm = ncm, Ex = ex, Tipo = tipo, Descricao = descricao,
                        AliqNacional = aliqNacional, AliqImportado = aliqImportado,
                        AliqEstadual = aliqEstadual, AliqMunicipal = aliqMunicipal,
                        VigenciaInicio = vigInicio, VigenciaFim = vigFim,
                        Chave = chave, Versao = versao, Fonte = fonte,
                        Uf = uf?.ToUpper() ?? ""
                    });
                }
            }
            catch
            {
                erros++;
            }
        }

        if (registros.Count == 0)
            throw new ArgumentException("Nenhum registro válido encontrado no CSV.");

        // Substituir todos os registros da UF (ou todos se UF não especificada)
        if (!string.IsNullOrEmpty(uf))
            await _db.IbptTaxes.Where(x => x.Uf == uf.ToUpper()).ExecuteDeleteAsync();
        else
            await _db.IbptTaxes.ExecuteDeleteAsync();

        // Inserir em lotes de 5000
        foreach (var lote in registros.Chunk(5000))
        {
            _db.IbptTaxes.AddRange(lote);
            await _db.SaveChangesAsync();
        }

        var versaoImportada = registros.FirstOrDefault()?.Versao ?? "";
        var vigencia = registros.FirstOrDefault()?.VigenciaFim;

        // Salvar metadados na configuração
        await SalvarConfig("ibpt.versao", versaoImportada);
        await SalvarConfig("ibpt.uf", uf?.ToUpper() ?? "");
        await SalvarConfig("ibpt.data.importacao", DateTime.UtcNow.ToString("o"));
        await SalvarConfig("ibpt.vigencia.fim", vigencia?.ToString("yyyy-MM-dd") ?? "");
        await SalvarConfig("ibpt.total.registros", registros.Count.ToString());
        await _db.SaveChangesAsync();

        Log.Information("IBPTax importado: {Total} registros, versão {Versao}, UF {Uf}",
            registros.Count, versaoImportada, uf ?? "TODAS");

        return new IbptImportResult
        {
            TotalImportado = registros.Count,
            Erros = erros,
            Versao = versaoImportada,
            VigenciaFim = vigencia
        };
    }

    /// <summary>Busca alíquotas aproximadas para um NCM e UF.</summary>
    public async Task<IbptTax?> BuscarPorNcmAsync(string ncm, string uf)
    {
        ncm = ncm.Replace(".", "").PadRight(8, '0');
        if (ncm.Length > 8) ncm = ncm[..8];

        return await _db.IbptTaxes
            .Where(x => x.Ncm == ncm && x.Uf == uf && x.Tipo == 0)
            .FirstOrDefaultAsync();
    }

    /// <summary>Busca alíquotas para múltiplos NCMs de uma vez (otimizado para NFC-e).</summary>
    public async Task<Dictionary<string, IbptTax>> BuscarPorNcmsAsync(IEnumerable<string> ncms, string uf)
    {
        var ncmsNormalizados = ncms.Select(n => n.Replace(".", "").PadRight(8, '0').Substring(0, 8)).Distinct().ToList();
        var registros = await _db.IbptTaxes
            .Where(x => ncmsNormalizados.Contains(x.Ncm) && x.Uf == uf && x.Tipo == 0)
            .ToListAsync();
        return registros.GroupBy(r => r.Ncm).ToDictionary(g => g.Key, g => g.First());
    }

    /// <summary>Retorna informações sobre a tabela importada.</summary>
    public async Task<IbptStatusResult> ObterStatusAsync()
    {
        var versao = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.versao"))?.Valor;
        var uf = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.uf"))?.Valor;
        var dataImport = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.data.importacao"))?.Valor;
        var vigFim = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.vigencia.fim"))?.Valor;
        var total = (await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "ibpt.total.registros"))?.Valor;

        return new IbptStatusResult
        {
            Versao = versao,
            Uf = uf,
            DataImportacao = DateTime.TryParse(dataImport, out var d) ? d : null,
            VigenciaFim = DateTime.TryParse(vigFim, out var v) ? v : null,
            TotalRegistros = int.TryParse(total, out var t) ? t : 0,
            Expirada = DateTime.TryParse(vigFim, out var vf) && vf < DateTime.UtcNow
        };
    }

    private async Task SalvarConfig(string chave, string valor)
    {
        var cfg = await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == chave);
        if (cfg != null)
            cfg.Valor = valor;
        else
            _db.Set<Configuracao>().Add(new Configuracao { Chave = chave, Valor = valor });
    }

    private static decimal ParseDecimal(string s)
    {
        s = s.Trim();
        if (string.IsNullOrEmpty(s)) return 0;
        // CSV IBPT usa ponto ou vírgula como separador decimal
        s = s.Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static DateTime ParseData(string s)
    {
        s = s.Trim();
        // Formatos possíveis: dd/MM/yyyy, yyyy-MM-dd
        if (DateTime.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1)) return d1;
        if (DateTime.TryParse(s, out var d2)) return d2;
        return DateTime.MinValue;
    }
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
