using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Serilog;
using ZulexPharma.Application.DTOs.FarmaciaPopular;
using ZulexPharma.Application.Interfaces;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Cliente SOAP 1.1 RPC/encoded do WebService DATASUS Farmácia Popular.
/// Axis 1.4 legado — montamos envelope manualmente porque WCF .NET moderno não
/// serializa bem esse estilo. Namespaces/prefixos seguem o WSDL oficial.
/// </summary>
public class FarmaciaPopularSoapClient : IFarmaciaPopularSoapClient
{
    private readonly HttpClient _http;
    private readonly string _endpoint;

    private const string NsSoap = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string NsEnc  = "http://schemas.xmlsoap.org/soap/encoding/";
    private const string NsXsi  = "http://www.w3.org/2001/XMLSchema-instance";
    private const string NsXsd  = "http://www.w3.org/2001/XMLSchema";
    private const string NsSer  = "http://service.datasus.org/";

    public FarmaciaPopularSoapClient(HttpClient http, string endpoint)
    {
        _http = http;
        _endpoint = endpoint;
    }

    // ── Fase 1 ────────────────────────────────────────────────────────
    public async Task<SolicitacaoRetornoDto> ExecutarSolicitacaoAsync(SolicitacaoRequest req, CredenciaisFp cred, CancellationToken ct = default)
    {
        var soapenv = XNamespace.Get(NsSoap);
        var ser = XNamespace.Get(NsSer);
        var xsi = XNamespace.Get(NsXsi);

        var arrMed = new XElement(XName.Get("arrMedicamentoDTO", ""));
        foreach (var m in req.Medicamentos)
        {
            arrMed.Add(new XElement("item",
                new XAttribute(xsi + "type", "ser:MedicamentoDTO"),
                new XElement("coCodigoBarra", m.CoCodigoBarra),
                new XElement("qtSolicitada", m.QtSolicitada.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new XElement("vlPrecoVenda", m.VlPrecoVenda.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement("qtPrescrita", m.QtPrescrita.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ));
        }

        var envelope = new XElement(soapenv + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soapenv", NsSoap),
            new XAttribute(XNamespace.Xmlns + "ser", NsSer),
            new XAttribute(XNamespace.Xmlns + "soapenc", NsEnc),
            new XAttribute(XNamespace.Xmlns + "xsi", NsXsi),
            new XAttribute(XNamespace.Xmlns + "xsd", NsXsd),
            new XElement(soapenv + "Header"),
            new XElement(soapenv + "Body",
                new XElement(ser + "executarSolicitacao",
                    new XAttribute(soapenv + "encodingStyle", NsEnc),
                    new XElement("in0",
                        new XAttribute(xsi + "type", "ser:SolicitacaoDTO"),
                        new XElement("coSolicitacaoFarmacia", req.CoSolicitacaoFarmacia),
                        new XElement("nuCnpj", req.NuCnpj),
                        new XElement("nuCpf", req.NuCpf),
                        new XElement("nuCrm", req.NuCrm),
                        new XElement("sgUfCrm", req.SgUfCrm),
                        new XElement("dtEmissaoReceita", req.DtEmissaoReceita.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-ddTHH:mm:ss")),
                        new XElement("dnaEstacao", req.DnaEstacao),
                        arrMed
                    ),
                    CredEnvelope("in1", cred)
                )
            )
        );

        var (reqXml, respXml) = await PostAsync(envelope, "urn:executarSolicitacao", ct);

        var ret = new SolicitacaoRetornoDto { RequestXml = reqXml, ResponseXml = respXml };
        try
        {
            var doc = XDocument.Parse(respXml);

            // SOAP Fault: servidor rejeitou a requisição (ex: formato de data inválido, campo obrigatório ausente).
            var fault = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault != null)
            {
                var faultString = fault.Elements().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value ?? "";
                var faultCode = fault.Elements().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value ?? "";
                ret.Sucesso = false;
                ret.CodigoRetorno = "FAULT";
                ret.MensagemRetorno = $"{faultCode}: {faultString}".Trim(':', ' ');
                return ret;
            }

            // Axis 1.4 usa multiRef: <executarSolicitacaoReturn href="#id0"/> + <multiRef id="id0">...</multiRef>
            var multiRefs = doc.Descendants().Where(e => e.Name.LocalName == "multiRef")
                .ToDictionary(e => e.Attribute("id")?.Value ?? "", e => e);
            var retRef = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "executarSolicitacaoReturn");
            if (retRef == null)
            {
                ret.Sucesso = false;
                ret.CodigoRetorno = "ERR";
                ret.MensagemRetorno = "Resposta SOAP inesperada (sem executarSolicitacaoReturn nem Fault).";
                return ret;
            }
            var autorizacao = ResolverRef(retRef, multiRefs) ?? retRef;

            ret.CodigoRetorno = autorizacao.Elements().FirstOrDefault(e => e.Name.LocalName == "inAutorizacaoSolicitacao")?.Value ?? "";
            ret.MensagemRetorno = autorizacao.Elements().FirstOrDefault(e => e.Name.LocalName == "descMensagemErro")?.Value;
            ret.NuAutorizacao = autorizacao.Elements().FirstOrDefault(e => e.Name.LocalName == "nuAutorizacao")?.Value;
            ret.NoPaciente = autorizacao.Elements().FirstOrDefault(e => e.Name.LocalName == "noPessoa")?.Value;
            ret.Sucesso = ret.CodigoRetorno.StartsWith("00S") || ret.CodigoRetorno.StartsWith("01S");

            var arr = autorizacao.Elements().FirstOrDefault(e => e.Name.LocalName == "arrMedicamentoDTO");
            if (arr != null)
            {
                foreach (var itemRef in arr.Elements())
                {
                    var item = ResolverRef(itemRef, multiRefs) ?? itemRef;
                    var inAut = item.Elements().FirstOrDefault(e => e.Name.LocalName == "inAutorizacaoMedicamento")?.Value;
                    // Formato: "00SM - Autorizado" ou "24SM - Produto não autorizado. ..." — código vem antes do " - "
                    string? codItem = null;
                    if (!string.IsNullOrEmpty(inAut))
                    {
                        var idx = inAut.IndexOf(" - ", StringComparison.Ordinal);
                        codItem = idx > 0 ? inAut[..idx].Trim() : inAut.Trim();
                    }
                    ret.Itens.Add(new SolicitacaoRetornoItemDto
                    {
                        CodigoBarraEAN = item.Elements().FirstOrDefault(e => e.Name.LocalName == "coCodigoBarra")?.Value ?? "",
                        CodigoRetornoItem = codItem,
                        MensagemRetornoItem = inAut,
                        QtAutorizada = ParseDec(item.Elements().FirstOrDefault(e => e.Name.LocalName == "qtAutorizada")?.Value),
                        VlPrecoSubsidiadoMS = ParseDec(item.Elements().FirstOrDefault(e => e.Name.LocalName == "vlPrecoSubsidiadoMS")?.Value),
                        VlPrecoSubsidiadoPaciente = ParseDec(item.Elements().FirstOrDefault(e => e.Name.LocalName == "vlPrecoSubsidiadoPaciente")?.Value),
                        InAutorizacaoMedicamento = inAut
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro parseando retorno SOAP FP Fase 1");
            ret.Sucesso = false;
            ret.CodigoRetorno = "ERRPARSE";
            ret.MensagemRetorno = "Erro ao processar resposta SOAP: " + ex.Message;
        }
        return ret;
    }

    // ── Fase 2 ────────────────────────────────────────────────────────
    public Task<ConfirmacaoRetornoDto> ConfirmarSolicitacaoAsync(string coSolicitacaoFarmacia, string nuAutorizacao, string nuCupomFiscal, CredenciaisFp cred, CancellationToken ct = default)
        => ChamarFaseSimplesAsync("confirmarSolicitacao", new[]
        {
            ("coSolicitacaoFarmacia", coSolicitacaoFarmacia),
            ("nuAutorizacao", nuAutorizacao),
            ("nuCupomFiscal", nuCupomFiscal)
        }, cred, ct);

    // ── Fase 3 ────────────────────────────────────────────────────────
    public Task<ConfirmacaoRetornoDto> ReceberMedicamentoAsync(string coSolicitacaoFarmacia, string nuAutorizacao, CredenciaisFp cred, CancellationToken ct = default)
        => ChamarFaseSimplesAsync("receberMedicamento", new[]
        {
            ("coSolicitacaoFarmacia", coSolicitacaoFarmacia),
            ("nuAutorizacao", nuAutorizacao)
        }, cred, ct);

    // ── Fase E ────────────────────────────────────────────────────────
    public Task<ConfirmacaoRetornoDto> EstornarAsync(string coSolicitacaoFarmacia, string nuAutorizacao, string motivo, CredenciaisFp cred, CancellationToken ct = default)
        => ChamarFaseSimplesAsync("estornar", new[]
        {
            ("coSolicitacaoFarmacia", coSolicitacaoFarmacia),
            ("nuAutorizacao", nuAutorizacao),
            ("dsMotivoEstorno", motivo)
        }, cred, ct);

    // ── Helper: envelope genérico para fases 2/3/E ────────────────────
    private async Task<ConfirmacaoRetornoDto> ChamarFaseSimplesAsync(string operacao, (string nome, string valor)[] campos, CredenciaisFp cred, CancellationToken ct)
    {
        var soapenv = XNamespace.Get(NsSoap);
        var ser = XNamespace.Get(NsSer);

        var in0 = new XElement("in0");
        foreach (var (nome, valor) in campos) in0.Add(new XElement(nome, valor));

        var envelope = new XElement(soapenv + "Envelope",
            new XAttribute(XNamespace.Xmlns + "soapenv", NsSoap),
            new XAttribute(XNamespace.Xmlns + "ser", NsSer),
            new XAttribute(XNamespace.Xmlns + "soapenc", NsEnc),
            new XAttribute(XNamespace.Xmlns + "xsi", NsXsi),
            new XAttribute(XNamespace.Xmlns + "xsd", NsXsd),
            new XElement(soapenv + "Header"),
            new XElement(soapenv + "Body",
                new XElement(ser + operacao,
                    new XAttribute(soapenv + "encodingStyle", NsEnc),
                    in0,
                    CredEnvelope("in1", cred)
                )
            )
        );

        var (reqXml, respXml) = await PostAsync(envelope, $"urn:{operacao}", ct);
        var ret = new ConfirmacaoRetornoDto { RequestXml = reqXml, ResponseXml = respXml };
        try
        {
            var doc = XDocument.Parse(respXml);
            var fault = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (fault != null)
            {
                var faultString = fault.Elements().FirstOrDefault(e => e.Name.LocalName == "faultstring")?.Value ?? "";
                var faultCode = fault.Elements().FirstOrDefault(e => e.Name.LocalName == "faultcode")?.Value ?? "";
                ret.Sucesso = false;
                ret.CodigoRetorno = "FAULT";
                ret.MensagemRetorno = $"{faultCode}: {faultString}".Trim(':', ' ');
                return ret;
            }
            var root = doc.Descendants().FirstOrDefault(e => e.Name.LocalName.EndsWith("Return") || e.Name.LocalName == "return");
            ret.CodigoRetorno = root?.Elements().FirstOrDefault(e => e.Name.LocalName == "coRetorno")?.Value ?? "";
            ret.MensagemRetorno = root?.Elements().FirstOrDefault(e => e.Name.LocalName == "msRetorno")?.Value;
            ret.Sucesso = ret.CodigoRetorno.StartsWith("00") || ret.CodigoRetorno.StartsWith("01");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro parseando retorno SOAP FP {Operacao}", operacao);
            ret.Sucesso = false;
            ret.CodigoRetorno = "ERRPARSE";
            ret.MensagemRetorno = "Erro ao processar resposta SOAP: " + ex.Message;
        }
        return ret;
    }

    private static XElement CredEnvelope(string nome, CredenciaisFp cred)
    {
        var xsi = XNamespace.Get(NsXsi);
        return new XElement(nome,
            new XAttribute(xsi + "type", "ser:UsuarioFarmaciaDTO"),
            new XElement("usuarioFarmacia", cred.UsuarioFarmacia),
            new XElement("senhaFarmacia", cred.SenhaFarmacia),
            new XElement("usuarioVendedor", cred.UsuarioVendedor),
            new XElement("senhaVendedor", cred.SenhaVendedor)
        );
    }

    private async Task<(string reqXml, string respXml)> PostAsync(XElement envelope, string soapAction, CancellationToken ct)
    {
        var xmlReq = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + envelope.ToString(SaveOptions.DisableFormatting);
        using var content = new StringContent(xmlReq, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "UTF-8" };

        using var msg = new HttpRequestMessage(HttpMethod.Post, _endpoint) { Content = content };
        msg.Headers.Add("SOAPAction", soapAction);

        Log.Information("SOAP FP POST {Endpoint} action={Action} reqLen={Len}", _endpoint, soapAction, xmlReq.Length);
        var resp = await _http.SendAsync(msg, ct);
        var respXml = await resp.Content.ReadAsStringAsync(ct);
        Log.Information("SOAP FP resp status={Status} respLen={Len}", (int)resp.StatusCode, respXml.Length);
        if (!resp.IsSuccessStatusCode && string.IsNullOrWhiteSpace(respXml))
            throw new HttpRequestException($"SOAP FP HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}");
        return (xmlReq, respXml);
    }

    private static decimal? ParseDec(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    /// <summary>Resolve o atributo href="#id0" para o multiRef correspondente. Retorna null se o elemento não tem href.</summary>
    private static XElement? ResolverRef(XElement placeholder, Dictionary<string, XElement> multiRefs)
    {
        var href = placeholder.Attribute("href")?.Value;
        if (string.IsNullOrEmpty(href) || !href.StartsWith("#")) return null;
        return multiRefs.TryGetValue(href[1..], out var el) ? el : null;
    }
}
