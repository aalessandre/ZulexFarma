using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ZulexPharma.Application.DTOs.GestorTributario;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services.GestorTributario;

/// <summary>
/// Provedor Avant (figurafiscal.com.br v3.0).
///
/// Autenticação via URL path: idParceiro/CNPJ/token.
///
/// Endpoint usado: POST /revisao/{id}/{cnpj}/{token} — aceita lote de até 300 itens
/// e retorna um JSON aninhado com toda a parametrização fiscal.
///
/// Este provider é responsável SOMENTE pelo transporte HTTP e tradução do JSON da Avant
/// para o DTO normalizado. O orquestrador (<c>GestorTributarioService</c>) cuida de
/// rate limiting, persistência e aplicação nos produtos.
/// </summary>
public class AvantGestorTributarioProvider : IGestorTributarioProvider
{
    public string Nome => "avant";
    public int LimiteMensal => 50_000;

    private const int LoteMax = 300;
    private const string BaseUrl = "https://figurafiscalws.com.br/v3.0";

    private readonly IHttpClientFactory _httpFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    public AvantGestorTributarioProvider(IHttpClientFactory httpFactory, IServiceScopeFactory scopeFactory)
    {
        _httpFactory = httpFactory;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> TestarConexaoAsync(CancellationToken ct = default)
    {
        var cfg = await CarregarConfigAsync();
        if (cfg == null) return false;
        try
        {
            // Teste: envia 1 item fake só pra validar credenciais
            var itens = new List<ProdutoRevisaoDto>
            {
                new() { CodInterno = "TEST", Ean = "0000000000000", Descricao = "TESTE CONEXAO" }
            };
            var result = await EnviarLoteAsync(cfg, itens, ct);
            return result != null;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "AvantGestorTributarioProvider.TestarConexao falhou");
            return false;
        }
    }

    public async Task<ProdutoFiscalExternoDto?> ConsultarPorEanAsync(string ean, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ean)) return null;
        var cfg = await CarregarConfigAsync();
        if (cfg == null) throw new InvalidOperationException("Gestor Tributário não configurado.");

        var itens = new List<ProdutoRevisaoDto>
        {
            new() { CodInterno = ean, Ean = ean, Descricao = "" }
        };
        var resposta = await EnviarLoteAsync(cfg, itens, ct);
        return resposta?.Itens.FirstOrDefault();
    }

    public async Task<ResultadoRevisaoDto> RevisarLoteAsync(List<ProdutoRevisaoDto> itens, CancellationToken ct = default)
    {
        var cfg = await CarregarConfigAsync()
            ?? throw new InvalidOperationException("Gestor Tributário não configurado.");

        var resultado = new ResultadoRevisaoDto { TotalEnviados = itens.Count };
        // Quebra em lotes de 300
        for (int i = 0; i < itens.Count; i += LoteMax)
        {
            ct.ThrowIfCancellationRequested();
            var lote = itens.Skip(i).Take(LoteMax).ToList();
            var parcial = await EnviarLoteAsync(cfg, lote, ct);
            if (parcial != null)
            {
                resultado.Itens.AddRange(parcial.Itens);
                resultado.TotalEncontrados += parcial.TotalEncontrados;
            }
        }
        return resultado;
    }

    // ── HTTP + parse ─────────────────────────────────────────────────
    private async Task<ResultadoRevisaoDto?> EnviarLoteAsync(AvantConfig cfg, List<ProdutoRevisaoDto> itens, CancellationToken ct)
    {
        var url = $"{BaseUrl}/revisao/{cfg.IdParceiro}/{cfg.CnpjCliente}/{cfg.Token}";
        // A Avant EXIGE todos os campos fiscais no payload. A ideia é que você manda os
        // dados ATUAIS do produto e ela retorna os corrigidos. Se o produto é novo e
        // não tem dados, usamos defaults aceitáveis abaixo.
        var body = itens.Select(i => new Dictionary<string, object?>
        {
            ["codinterno"] = string.IsNullOrWhiteSpace(i.CodInterno) ? i.Ean : i.CodInterno,
            ["ean"] = string.IsNullOrWhiteSpace(i.Ean) ? i.CodInterno : i.Ean,
            ["descricao"] = GarantirDescricaoMinima(i.Descricao, i.Ean),
            ["ncm"] = string.IsNullOrWhiteSpace(i.NcmAtual) ? "30049099" : i.NcmAtual,  // default: medicamentos
            ["ex_tipi"] = i.ExTipiAtual ?? "",
            ["cest"] = i.CestAtual ?? "",
            ["cfop"] = string.IsNullOrWhiteSpace(i.CfopAtual) ? "5102" : i.CfopAtual,   // default: venda mercadoria
            ["csosn"] = DetermineCsosn(cfg.Crt, i.CsosnAtual),
            ["cst"] = cfg.Crt == 3 ? (i.CstAtual ?? "00") : "",
            ["pICMS"] = i.PIcmsAtual ?? 18.00m,
            ["pFCP"] = i.PFcpAtual ?? 0m,
            ["cst_pis_saida"] = string.IsNullOrWhiteSpace(i.CstPisAtual) ? "01" : i.CstPisAtual,
            ["cst_cofins_saida"] = string.IsNullOrWhiteSpace(i.CstCofinsAtual) ? "01" : i.CstCofinsAtual,
            ["regime"] = cfg.Regime,
            ["crt"] = cfg.Crt,
            ["cMun"] = cfg.CodigoMunicipio
        }).ToList();

        var client = _httpFactory.CreateClient("Avant");
        client.Timeout = TimeSpan.FromMinutes(2);

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Log.Information("Avant: POST {Url} ({Count} itens) — body: {Body}",
            url.Replace(cfg.Token, "***"), itens.Count, json);
        var resp = await client.PostAsync(url, content, ct);
        var respText = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            Log.Error("Avant: HTTP {Status} — {Body}", (int)resp.StatusCode, respText);
            if ((int)resp.StatusCode == 429)
                throw new InvalidOperationException("Limite de requisições Avant excedido (429). Tente novamente no próximo mês.");

            // Tenta extrair mensagem amigável do body da Avant
            var msgAvant = TentarExtrairMensagemErro(respText);
            throw new InvalidOperationException(
                $"Avant retornou HTTP {(int)resp.StatusCode}. " +
                (msgAvant != null ? $"Mensagem: {msgAvant}" : $"Resposta: {TruncarTexto(respText, 500)}"));
        }

        // Avant às vezes retorna 200 mas com erro=1 no corpo
        var mensagemErroBody = ExtrairErroDoBody(respText);
        if (mensagemErroBody != null)
        {
            Log.Error("Avant: HTTP 200 mas body indica erro — {Msg}", mensagemErroBody);
            throw new InvalidOperationException($"Avant: {mensagemErroBody}");
        }

        return ParseRespostaAvant(respText);
    }

    /// <summary>Tenta extrair campo "mensagem" ou "error" de um JSON de erro da Avant.</summary>
    private static string? TentarExtrairMensagemErro(string respText)
    {
        if (string.IsNullOrWhiteSpace(respText)) return null;
        try
        {
            using var doc = JsonDocument.Parse(respText);
            var root = doc.RootElement;
            if (root.TryGetProperty("mensagem", out var m) && !string.IsNullOrWhiteSpace(m.GetString()))
                return m.GetString();
            if (root.TryGetProperty("message", out var m2) && !string.IsNullOrWhiteSpace(m2.GetString()))
                return m2.GetString();
            if (root.TryGetProperty("error", out var e) && !string.IsNullOrWhiteSpace(e.GetString()))
                return e.GetString();
        }
        catch { /* body não é JSON */ }
        return null;
    }

    /// <summary>Avant retorna erro=1 quando há falha de negócio mesmo com HTTP 200.</summary>
    private static string? ExtrairErroDoBody(string respText)
    {
        if (string.IsNullOrWhiteSpace(respText)) return null;
        try
        {
            using var doc = JsonDocument.Parse(respText);
            var root = doc.RootElement;
            if (root.TryGetProperty("erro", out var erroEl))
            {
                var erroVal = 0;
                if (erroEl.ValueKind == JsonValueKind.Number) erroEl.TryGetInt32(out erroVal);
                else if (erroEl.ValueKind == JsonValueKind.String && int.TryParse(erroEl.GetString(), out var e)) erroVal = e;
                if (erroVal != 0)
                {
                    return root.TryGetProperty("mensagem", out var msg) ? msg.GetString() ?? "Erro não especificado" : "Erro não especificado";
                }
            }
        }
        catch { }
        return null;
    }

    private static string TruncarTexto(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max) + "...");

    /// <summary>A Avant exige descrição com no mínimo 3 caracteres. Se vazio/curto, usa fallback.</summary>
    private static string GarantirDescricaoMinima(string? desc, string ean)
    {
        var d = (desc ?? "").Trim();
        if (d.Length >= 3) return d;
        return $"PRODUTO EAN {ean}";
    }

    /// <summary>
    /// Para Simples Nacional (crt 1 ou 2), CSOSN é obrigatório.
    /// Default "102" = Simples sem permissão de crédito.
    /// Para regime Normal (crt 3), CSOSN deve ser vazio.
    /// </summary>
    private static string DetermineCsosn(int crt, string? csosnAtual)
    {
        if (crt == 3) return ""; // Regime Normal usa CST, não CSOSN
        return string.IsNullOrWhiteSpace(csosnAtual) ? "102" : csosnAtual;
    }

    private static ResultadoRevisaoDto? ParseRespostaAvant(string respText)
    {
        if (string.IsNullOrWhiteSpace(respText)) return null;
        using var doc = JsonDocument.Parse(respText);
        var root = doc.RootElement;

        var resultado = new ResultadoRevisaoDto();
        if (root.TryGetProperty("itens_enviados", out var ie) && ie.TryGetInt32(out var iev))
            resultado.TotalEnviados = iev;
        if (root.TryGetProperty("itens_encontrados", out var ien) && ien.TryGetInt32(out var ienv))
            resultado.TotalEncontrados = ienv;

        if (!root.TryGetProperty("itens", out var itensArr) || itensArr.ValueKind != JsonValueKind.Array)
            return resultado;

        foreach (var itemEl in itensArr.EnumerateArray())
        {
            var dto = new ProdutoFiscalExternoDto { Encontrado = true };

            // produtos.produto_cliente.ean_cliente
            if (itemEl.TryGetProperty("produtos", out var produtos))
            {
                if (produtos.TryGetProperty("produto_cliente", out var pc))
                {
                    dto.Ean = pc.TryGetProperty("ean_cliente", out var ec) ? ec.GetString() ?? "" : "";
                }
                if (produtos.TryGetProperty("produto", out var p))
                {
                    if (string.IsNullOrEmpty(dto.Ean) && p.TryGetProperty("ean", out var e2)) dto.Ean = e2.GetString() ?? "";
                    dto.Ncm = p.TryGetProperty("ncm", out var n) ? n.GetString() : null;
                    dto.Cest = p.TryGetProperty("cest", out var c) ? c.GetString() : null;
                    dto.ExTipi = p.TryGetProperty("ex_tipi", out var et) ? et.GetString() : null;
                    dto.DescricaoPadronizada = p.TryGetProperty("descricao", out var d) ? d.GetString() : null;
                }
            }

            // tributos.cfop.cfop_saida.cfop_venda
            if (itemEl.TryGetProperty("tributos", out var trib))
            {
                if (trib.TryGetProperty("cfop", out var cfop) && cfop.TryGetProperty("cfop_saida", out var cs))
                    dto.Cfop = cs.TryGetProperty("cfop_venda", out var cv) ? cv.GetString() : null;

                // grupoTribPDV.ST = true indica Substituição Tributária
                if (trib.TryGetProperty("grupoTribPDV", out var grupoTrib))
                {
                    if (grupoTrib.TryGetProperty("ST", out var stEl) && stEl.ValueKind == JsonValueKind.True)
                        dto.TemSubstituicaoTributaria = true;
                }

                // ICMS saída
                if (trib.TryGetProperty("imposto_estadual", out var ie2) && ie2.TryGetProperty("icms_saida", out var icms))
                {
                    dto.Csosn = GetStr(icms, "csosn");
                    dto.CstIcms = GetStr(icms, "cst");
                    dto.ModBc = GetStr(icms, "modBC");
                    dto.AliquotaIcms = GetDec(icms, "pICMS");
                    dto.AliquotaFcp = GetDec(icms, "pFCP");
                    dto.PercentualReducaoBc = GetDec(icms, "pRedBC");
                    dto.CodigoBeneficio = GetStr(icms, "cBenef");
                    dto.DispositivoLegalIcms = GetStr(icms, "dispositivoLegal");
                }

                // ICMS entrada (por UF + ST antecipado)
                if (trib.TryGetProperty("imposto_estadual", out var ie3) && ie3.TryGetProperty("icms_entrada", out var ice))
                {
                    // por_uf: dicionário UF → alíquota (ex: "SP":"18.00", "MG":"12.00")
                    if (ice.TryGetProperty("por_uf", out var porUf) && porUf.ValueKind == JsonValueKind.Object)
                    {
                        var mapa = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                        foreach (var prop in porUf.EnumerateObject())
                        {
                            var v = prop.Value;
                            decimal val = 0;
                            if (v.ValueKind == JsonValueKind.Number) v.TryGetDecimal(out val);
                            else if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var vd)) val = vd;
                            mapa[prop.Name] = val;
                        }
                        dto.IcmsEntradaPorUf = mapa;
                    }

                    if (ice.TryGetProperty("antecipado", out var ant))
                    {
                        dto.MvaOriginal = GetDec(ant, "MVA_original");
                        dto.MvaAjustado4 = GetDec(ant, "MVA_ajustado_4%");
                        dto.MvaAjustado7 = GetDec(ant, "MVA_ajustado_7%");
                        dto.MvaAjustado12 = GetDec(ant, "MVA_ajustado_12%");
                        dto.AliquotaIcmsSt = GetDec(ant, "pST");
                        dto.AliquotaFcpSt = GetDec(ant, "pFCPST");
                    }
                }

                // IPI / PIS / COFINS
                if (trib.TryGetProperty("imposto_federal", out var ifed))
                {
                    if (ifed.TryGetProperty("ipi", out var ipi))
                    {
                        if (ipi.TryGetProperty("ipi_saida", out var ipiS))
                        {
                            dto.CstIpi = GetStr(ipiS, "cst");
                            dto.AliquotaIpi = GetDec(ipiS, "pIpi");
                            dto.EnquadramentoIpi = GetStr(ipiS, "cEnq");
                        }
                        if (ipi.TryGetProperty("ipi_entrada", out var ipiE))
                        {
                            dto.CstIpiEntrada = GetStr(ipiE, "cst");
                            dto.AliquotaIpiEntrada = GetDec(ipiE, "pIpi");
                            dto.AliquotaIpiIndustria = GetDec(ipiE, "pIpi_industria");
                        }
                    }
                    if (ifed.TryGetProperty("pis", out var pis))
                    {
                        if (pis.TryGetProperty("pis_saida", out var pisS))
                        {
                            dto.CstPis = GetStr(pisS, "cst");
                            dto.AliquotaPis = GetDec(pisS, "pPIS");
                            dto.NaturezaReceita = GetStr(pisS, "naturezaReceita");
                        }
                        if (pis.TryGetProperty("pis_entrada", out var pisE))
                            dto.CstPisEntrada = GetStr(pisE, "cst");
                    }
                    if (ifed.TryGetProperty("cofins", out var cof))
                    {
                        if (cof.TryGetProperty("cofins_saida", out var cofS))
                        {
                            dto.CstCofins = GetStr(cofS, "cst");
                            dto.AliquotaCofins = GetDec(cofS, "pCofins");
                        }
                        if (cof.TryGetProperty("cofins_entrada", out var cofE))
                            dto.CstCofinsEntrada = GetStr(cofE, "cst");
                    }
                }

                // Reforma Tributária (campo reforma_tributaria.ano.{ano}.{IS|IBSCBS})
                if (trib.TryGetProperty("reforma_tributaria", out var rt) && rt.TryGetProperty("ano", out var anoObj))
                {
                    // Pega o primeiro ano disponível (ou o atual)
                    var anoAtual = DateTime.UtcNow.Year.ToString();
                    JsonElement? anoSelecionado = null;
                    if (anoObj.TryGetProperty(anoAtual, out var atual))
                        anoSelecionado = atual;
                    else
                    {
                        foreach (var prop in anoObj.EnumerateObject())
                        {
                            anoSelecionado = prop.Value;
                            break;
                        }
                    }

                    if (anoSelecionado.HasValue)
                    {
                        var ano = anoSelecionado.Value;
                        if (ano.TryGetProperty("IS", out var isEl))
                        {
                            dto.CstIs = GetStr(isEl, "CSTIS");
                            dto.ClassTribIs = GetStr(isEl, "cClassTribIS");
                            dto.AliquotaIs = GetDec(isEl, "pIS");
                        }
                        if (ano.TryGetProperty("IBSCBS", out var ibs))
                        {
                            dto.CstIbsCbs = GetStr(ibs, "CST");
                            dto.ClassTribIbsCbs = GetStr(ibs, "cClassTrib");
                            if (ibs.TryGetProperty("gIBSCBS", out var g))
                            {
                                if (g.TryGetProperty("gIBSUF", out var gIbsUf))
                                    dto.AliquotaIbsUf = GetDec(gIbsUf, "pAliqEfet");
                                if (g.TryGetProperty("gIBSMun", out var gIbsMun))
                                    dto.AliquotaIbsMun = GetDec(gIbsMun, "pAliqEfet");
                                if (g.TryGetProperty("gCBS", out var gCbs))
                                    dto.AliquotaCbs = GetDec(gCbs, "pAliqEfet");
                            }
                        }
                    }
                }
            }

            resultado.Itens.Add(dto);
        }

        return resultado;
    }

    private static string? GetStr(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null;

    private static decimal GetDec(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null) return 0;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d)) return d;
        if (v.ValueKind == JsonValueKind.String)
        {
            var s = v.GetString();
            if (!string.IsNullOrWhiteSpace(s) && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var dd))
                return dd;
        }
        return 0;
    }

    // ── Config loader ────────────────────────────────────────────────
    private async Task<AvantConfig?> CarregarConfigAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var map = await db.Configuracoes
            .Where(c => c.Chave.StartsWith("gestor.avant.") || c.Chave == "fiscal.regime.tributario")
            .ToDictionaryAsync(c => c.Chave, c => c.Valor ?? "");

        var idParceiro = map.GetValueOrDefault("gestor.avant.id_parceiro", "");
        var cnpj = new string((map.GetValueOrDefault("gestor.avant.cnpj_cliente", "")).Where(char.IsDigit).ToArray());
        var token = map.GetValueOrDefault("gestor.avant.token", "");
        var cMun = map.GetValueOrDefault("gestor.avant.cod_municipio", "");

        if (string.IsNullOrWhiteSpace(idParceiro) ||
            string.IsNullOrWhiteSpace(cnpj) ||
            string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrWhiteSpace(cMun))
        {
            return null;
        }

        var regimeCfg = map.GetValueOrDefault("fiscal.regime.tributario", "1");
        // 1=Simples, 2=Simples excesso, 3=Normal
        var crt = int.TryParse(regimeCfg, out var c) ? c : 1;
        var regimeNome = crt switch { 3 => "real", 2 => "simples", _ => "simples" };

        return new AvantConfig
        {
            IdParceiro = idParceiro,
            CnpjCliente = cnpj,
            Token = token,
            CodigoMunicipio = cMun,
            Crt = crt,
            Regime = regimeNome
        };
    }

    private class AvantConfig
    {
        public string IdParceiro { get; set; } = "";
        public string CnpjCliente { get; set; } = "";
        public string Token { get; set; } = "";
        public string CodigoMunicipio { get; set; } = "";
        public int Crt { get; set; }
        public string Regime { get; set; } = "simples";
    }
}
