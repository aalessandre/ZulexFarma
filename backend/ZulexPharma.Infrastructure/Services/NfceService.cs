using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Domain.Helpers;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

public class NfceService
{
    private readonly AppDbContext _db;

    public NfceService(AppDbContext db) => _db = db;

    public async Task<NfceResult> EmitirAsync(long vendaId)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens)
            .Include(v => v.Pagamentos).ThenInclude(p => p.TipoPagamento)
            .Include(v => v.Cliente).ThenInclude(c => c!.Pessoa)
            .FirstOrDefaultAsync(v => v.Id == vendaId)
            ?? throw new KeyNotFoundException("Venda não encontrada.");

        var filial = await _db.Filiais.FindAsync(venda.FilialId)
            ?? throw new KeyNotFoundException("Filial não encontrada.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filial.Id)
            ?? throw new ArgumentException("Certificado digital não configurado.");

        if (certDb.Validade <= DataHoraHelper.Agora())
            throw new ArgumentException("Certificado digital expirado.");

        var prodIds = venda.Itens.Select(i => i.ProdutoId).ToList();
        var fiscais = await _db.ProdutosFiscal
            .Where(f => prodIds.Contains(f.ProdutoId) && f.FilialId == filial.Id)
            .ToDictionaryAsync(f => f.ProdutoId);
        var produtos = await _db.Produtos
            .Where(p => prodIds.Contains(p.Id)).Include(p => p.Ncm)
            .ToDictionaryAsync(p => p.Id);

        // Buscar alíquotas IBPTax para cálculo do vTotTrib (Lei 12.741/2012)
        var ncms = produtos.Values.Select(p => (p.Ncm?.CodigoNcm ?? "00000000").Replace(".", "").PadRight(8, '0')[..8]).Distinct().ToList();
        var ibptDict = await _db.IbptTaxes
            .Where(x => ncms.Contains(x.Ncm) && x.Uf == filial.Uf && x.Tipo == 0)
            .GroupBy(x => x.Ncm).Select(g => g.First())
            .ToDictionaryAsync(x => x.Ncm);

        var configs = await _db.Set<Configuracao>().ToDictionaryAsync(c => c.Chave, c => c.Valor);
        var ambiente = int.Parse(configs.GetValueOrDefault("fiscal.ambiente", "2"));
        var csc = configs.GetValueOrDefault("fiscal.nfce.csc", "");
        var cscId = configs.GetValueOrDefault("fiscal.nfce.csc.id", "1");
        var serie = int.Parse(configs.GetValueOrDefault("fiscal.nfce.serie", "1"));
        var regimeTributario = int.Parse(configs.GetValueOrDefault("fiscal.regime.tributario", "1"));

        var ultimoNumero = await _db.Nfces
            .Where(n => n.FilialId == filial.Id && n.Serie == serie)
            .MaxAsync(n => (int?)n.Numero) ?? 0;
        var numero = ultimoNumero + 1;

        var ufCodigo = ObterCodigoUf(filial.Uf);
        var agora = DataHoraHelper.Agora();
        var cnpj = CpfCnpjHelper.SomenteDigitos(filial.Cnpj);
        var codigoNumerico = new Random().Next(10000000, 99999999);
        var chaveAcesso = GerarChaveAcesso(ufCodigo, agora, cnpj, 65, serie, numero, 1, codigoNumerico);

        var xml = MontarXml(venda, filial, chaveAcesso, numero, serie, ambiente, regimeTributario,
            ufCodigo, cnpj, agora, codigoNumerico, fiscais, produtos, ibptDict);

        var pfxBytes = Convert.FromBase64String(certDb.PfxBase64);
        var cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
        string xmlAssinado;
        try { xmlAssinado = AssinarXml(xml, cert); }
        finally { cert.Dispose(); }

        // Inserir infNFeSupl ANTES da Signature
        var qrCodeUrl = GerarUrlQrCode(chaveAcesso, ambiente, csc, cscId, filial.Uf);
        var urlConsulta = ObterUrlConsulta(filial.Uf, ambiente);
        var qrCodeEscaped = qrCodeUrl.Replace("&", "&amp;");
        var infNFeSupl = $"<infNFeSupl><qrCode><![CDATA[{qrCodeEscaped}]]></qrCode><urlChave>{urlConsulta}</urlChave></infNFeSupl>";
        // Inserir infNFeSupl ANTES da Signature principal (xmlns="http://www.w3.org/2000/09/xmldsig#")
        var sigTag = "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\">";
        if (xmlAssinado.Contains(sigTag))
            xmlAssinado = xmlAssinado.Replace(sigTag, infNFeSupl + sigTag);
        else
            xmlAssinado = xmlAssinado.Replace("</NFe>", infNFeSupl + "</NFe>");

        var urlAutorizacao = ObterUrlAutorizacao(filial.Uf, ambiente);
        string xmlRetorno;
        try
        {
            cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);
            var soapEnvelope = MontarSoapEnvelope(xmlAssinado);
            xmlRetorno = await EnviarSoap(urlAutorizacao, soapEnvelope, cert);
        }
        finally { cert.Dispose(); }

        var resultado = ProcessarRetorno(xmlRetorno);

        var nfce = new Nfce
        {
            FilialId = filial.Id, VendaId = vendaId,
            Numero = numero, Serie = serie, ChaveAcesso = chaveAcesso,
            Protocolo = resultado.Protocolo, DataAutorizacao = resultado.Autorizada ? agora : null,
            Ambiente = ambiente, CodigoStatus = resultado.CodigoStatus,
            MotivoStatus = resultado.MotivoStatus,
            XmlEnvio = xmlAssinado, XmlRetorno = xmlRetorno,
            ValorTotal = venda.TotalLiquido
        };
        _db.Nfces.Add(nfce);

        var cfgNumero = await _db.Set<Configuracao>().FirstOrDefaultAsync(c => c.Chave == "fiscal.nfce.numero.atual");
        if (cfgNumero != null) cfgNumero.Valor = numero.ToString();

        await _db.SaveChangesAsync();

        return new NfceResult
        {
            NfceId = nfce.Id, Numero = numero, Serie = serie, ChaveAcesso = chaveAcesso,
            Protocolo = resultado.Protocolo, CodigoStatus = resultado.CodigoStatus,
            MotivoStatus = resultado.MotivoStatus, Autorizada = resultado.Autorizada
        };
    }

    // ═══ Helpers de formatação ═══
    private static string ObterCodigoPagamento(Domain.Enums.ModalidadePagamento? mod) => mod switch
    {
        Domain.Enums.ModalidadePagamento.VendaVista => "01",
        Domain.Enums.ModalidadePagamento.VendaCartao => "03",
        Domain.Enums.ModalidadePagamento.VendaPix => "17",
        Domain.Enums.ModalidadePagamento.VendaPrazo => "05",
        _ => "99"
    };

    private static string D2(decimal v) => v.ToString("F2", CultureInfo.InvariantCulture);
    private static string D4(decimal v) => v.ToString("F4", CultureInfo.InvariantCulture);
    private static string Esc(string? s) => s == null ? "" : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ═══ XML NFC-e layout 4.00 ═══
    private string MontarXml(Venda venda, Filial filial, string chaveAcesso, int numero, int serie,
        int ambiente, int crt, int ufCodigo, string cnpj, DateTime agora, int codigoNumerico,
        Dictionary<long, ProdutoFiscal> fiscais, Dictionary<long, Produto> produtos,
        Dictionary<string, IbptTax> ibptDict)
    {
        var sb = new StringBuilder();
        sb.Append("<NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\">");
        sb.Append($"<infNFe versao=\"4.00\" Id=\"NFe{chaveAcesso}\">");

        // ide
        sb.Append("<ide>");
        sb.Append($"<cUF>{ufCodigo}</cUF>");
        sb.Append($"<cNF>{codigoNumerico:D8}</cNF>");
        sb.Append("<natOp>VENDA</natOp>");
        sb.Append("<mod>65</mod>");
        sb.Append($"<serie>{serie}</serie>");
        sb.Append($"<nNF>{numero}</nNF>");
        sb.Append($"<dhEmi>{agora:yyyy-MM-ddTHH:mm:sszzz}</dhEmi>");
        sb.Append("<tpNF>1</tpNF>");
        sb.Append("<idDest>1</idDest>");
        sb.Append($"<cMunFG>{filial.CodigoIbgeMunicipio ?? "0000000"}</cMunFG>");
        sb.Append("<tpImp>4</tpImp>");
        sb.Append("<tpEmis>1</tpEmis>");
        sb.Append($"<cDV>{chaveAcesso[43]}</cDV>");
        sb.Append($"<tpAmb>{ambiente}</tpAmb>");
        sb.Append("<finNFe>1</finNFe>");
        sb.Append("<indFinal>1</indFinal>");
        sb.Append("<indPres>1</indPres>");
        sb.Append("<procEmi>0</procEmi>");
        sb.Append("<verProc>ZulexPharma1.0</verProc>");
        sb.Append("</ide>");

        // emit
        sb.Append("<emit>");
        sb.Append($"<CNPJ>{cnpj}</CNPJ>");
        sb.Append($"<xNome>{Esc(filial.RazaoSocial)}</xNome>");
        sb.Append($"<xFant>{Esc(filial.NomeFantasia)}</xFant>");
        sb.Append("<enderEmit>");
        sb.Append($"<xLgr>{Esc(filial.Rua)}</xLgr>");
        sb.Append($"<nro>{Esc(filial.Numero)}</nro>");
        sb.Append($"<xBairro>{Esc(filial.Bairro)}</xBairro>");
        sb.Append($"<cMun>{filial.CodigoIbgeMunicipio ?? "0000000"}</cMun>");
        sb.Append($"<xMun>{Esc(filial.Cidade)}</xMun>");
        sb.Append($"<UF>{filial.Uf}</UF>");
        sb.Append($"<CEP>{filial.Cep.Replace("-", "")}</CEP>");
        sb.Append("<cPais>1058</cPais>");
        sb.Append("<xPais>Brasil</xPais>");
        var fone = filial.Telefone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
        if (!string.IsNullOrEmpty(fone)) sb.Append($"<fone>{fone}</fone>");
        sb.Append("</enderEmit>");
        var ie = filial.InscricaoEstadual?.Replace(".", "").Replace("-", "").Replace("/", "");
        if (!string.IsNullOrEmpty(ie)) sb.Append($"<IE>{ie}</IE>");
        sb.Append($"<CRT>{crt}</CRT>");
        sb.Append("</emit>");

        // dest (opcional NFC-e)
        if (venda.Cliente?.Pessoa != null)
        {
            var cpfCnpjDest = CpfCnpjHelper.SomenteDigitos(venda.Cliente.Pessoa.CpfCnpj);
            if (cpfCnpjDest.Length == 11 || cpfCnpjDest.Length == 14)
            {
                sb.Append("<dest>");
                if (cpfCnpjDest.Length == 11) sb.Append($"<CPF>{cpfCnpjDest}</CPF>");
                else sb.Append($"<CNPJ>{cpfCnpjDest}</CNPJ>");
                var xNome = ambiente == 2 ? "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : Esc(venda.Cliente.Pessoa.Nome);
                sb.Append($"<xNome>{xNome}</xNome>");
                sb.Append("<indIEDest>9</indIEDest>");
                sb.Append("</dest>");
            }
        }

        // det (produtos)
        int nItem = 1;
        decimal totalProdutos = 0, totalDesconto = 0, totalTributos = 0;
        foreach (var item in venda.Itens)
        {
            var prod = produtos.GetValueOrDefault(item.ProdutoId);
            var fiscal = fiscais.GetValueOrDefault(item.ProdutoId);
            var ncmRaw = (prod?.Ncm?.CodigoNcm ?? "00000000").Replace(".", "").PadRight(8, '0');
            if (ncmRaw.Length > 8) ncmRaw = ncmRaw[..8];
            var cfop = fiscal?.Cfop ?? "5102";
            var origem = fiscal?.OrigemMercadoria ?? "0";
            var valorBruto = Math.Round(item.PrecoVenda * item.Quantidade, 2);
            var valorDesc = Math.Round(item.ValorDesconto, 2);
            totalProdutos += valorBruto;
            totalDesconto += valorDesc;

            var xProd = ambiente == 2 ? "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : Esc(item.ProdutoNome);

            sb.Append($"<det nItem=\"{nItem}\">");
            sb.Append("<prod>");
            sb.Append($"<cProd>{item.ProdutoCodigo}</cProd>");
            sb.Append("<cEAN>SEM GTIN</cEAN>");
            sb.Append($"<xProd>{xProd}</xProd>");
            sb.Append($"<NCM>{ncmRaw}</NCM>");
            if (!string.IsNullOrEmpty(fiscal?.Cest)) sb.Append($"<CEST>{fiscal.Cest}</CEST>");
            sb.Append($"<CFOP>{cfop}</CFOP>");
            sb.Append("<uCom>UN</uCom>");
            sb.Append($"<qCom>{item.Quantidade}.0000</qCom>");
            sb.Append($"<vUnCom>{D4(item.PrecoVenda)}</vUnCom>");
            sb.Append($"<vProd>{D2(valorBruto)}</vProd>");
            sb.Append("<cEANTrib>SEM GTIN</cEANTrib>");
            sb.Append("<uTrib>UN</uTrib>");
            sb.Append($"<qTrib>{item.Quantidade}.0000</qTrib>");
            sb.Append($"<vUnTrib>{D4(item.PrecoVenda)}</vUnTrib>");
            if (valorDesc > 0) sb.Append($"<vDesc>{D2(valorDesc)}</vDesc>");
            sb.Append("<indTot>1</indTot>");
            sb.Append("</prod>");

            // impostos
            var valorLiquido = valorBruto - valorDesc;
            decimal vTotTribItem = 0;
            if (ibptDict.TryGetValue(ncmRaw, out var ibpt))
            {
                vTotTribItem = Math.Round(valorLiquido * (ibpt.AliqNacional + ibpt.AliqEstadual + ibpt.AliqMunicipal) / 100, 2);
            }
            totalTributos += vTotTribItem;

            sb.Append("<imposto>");
            sb.Append($"<vTotTrib>{D2(vTotTribItem)}</vTotTrib>");
            sb.Append("<ICMS>");
            if (crt <= 2)
            {
                var csosn = (fiscal?.Csosn ?? "102").TrimStart('0');
                if (string.IsNullOrEmpty(csosn)) csosn = "102";
                sb.Append("<ICMSSN102>");
                sb.Append($"<orig>{origem}</orig>");
                sb.Append($"<CSOSN>{csosn}</CSOSN>");
                sb.Append("</ICMSSN102>");
            }
            else
            {
                var cstIcms = fiscal?.CstIcms ?? "00";
                var aliqIcms = fiscal?.AliquotaIcms ?? 0;
                sb.Append("<ICMS00>");
                sb.Append($"<orig>{origem}</orig>");
                sb.Append($"<CST>{cstIcms}</CST>");
                sb.Append("<modBC>3</modBC>");
                sb.Append($"<vBC>{D2(valorBruto - valorDesc)}</vBC>");
                sb.Append($"<pICMS>{D2(aliqIcms)}</pICMS>");
                sb.Append($"<vICMS>{D2(Math.Round((valorBruto - valorDesc) * aliqIcms / 100, 2))}</vICMS>");
                sb.Append("</ICMS00>");
            }
            sb.Append("</ICMS>");

            // PIS
            var cstPis = fiscal?.CstPis ?? "49";
            sb.Append("<PIS>");
            if (cstPis == "01" || cstPis == "02")
            {
                sb.Append("<PISAliq>");
                sb.Append($"<CST>{cstPis}</CST>");
                sb.Append($"<vBC>{D2(valorBruto - valorDesc)}</vBC>");
                sb.Append($"<pPIS>{D2(fiscal?.AliquotaPis ?? 0)}</pPIS>");
                sb.Append($"<vPIS>{D2(Math.Round((valorBruto - valorDesc) * (fiscal?.AliquotaPis ?? 0) / 100, 2))}</vPIS>");
                sb.Append("</PISAliq>");
            }
            else if (cstPis == "04" || cstPis == "05" || cstPis == "06" || cstPis == "07" || cstPis == "08" || cstPis == "09")
            {
                sb.Append("<PISNT>");
                sb.Append($"<CST>{cstPis}</CST>");
                sb.Append("</PISNT>");
            }
            else
            {
                sb.Append("<PISOutr>");
                sb.Append($"<CST>{cstPis}</CST>");
                sb.Append("<vBC>0.00</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS>");
                sb.Append("</PISOutr>");
            }
            sb.Append("</PIS>");

            // COFINS
            var cstCofins = fiscal?.CstCofins ?? "49";
            sb.Append("<COFINS>");
            if (cstCofins == "01" || cstCofins == "02")
            {
                sb.Append("<COFINSAliq>");
                sb.Append($"<CST>{cstCofins}</CST>");
                sb.Append($"<vBC>{D2(valorBruto - valorDesc)}</vBC>");
                sb.Append($"<pCOFINS>{D2(fiscal?.AliquotaCofins ?? 0)}</pCOFINS>");
                sb.Append($"<vCOFINS>{D2(Math.Round((valorBruto - valorDesc) * (fiscal?.AliquotaCofins ?? 0) / 100, 2))}</vCOFINS>");
                sb.Append("</COFINSAliq>");
            }
            else if (cstCofins == "04" || cstCofins == "05" || cstCofins == "06" || cstCofins == "07" || cstCofins == "08" || cstCofins == "09")
            {
                sb.Append("<COFINSNT>");
                sb.Append($"<CST>{cstCofins}</CST>");
                sb.Append("</COFINSNT>");
            }
            else
            {
                sb.Append("<COFINSOutr>");
                sb.Append($"<CST>{cstCofins}</CST>");
                sb.Append("<vBC>0.00</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS>");
                sb.Append("</COFINSOutr>");
            }
            sb.Append("</COFINS>");

            sb.Append("</imposto>");
            sb.Append("</det>");
            nItem++;
        }

        // total
        sb.Append("<total><ICMSTot>");
        sb.Append("<vBC>0.00</vBC><vICMS>0.00</vICMS><vICMSDeson>0.00</vICMSDeson>");
        sb.Append("<vFCPUFDest>0.00</vFCPUFDest><vICMSUFDest>0.00</vICMSUFDest><vICMSUFRemet>0.00</vICMSUFRemet>");
        sb.Append("<vFCP>0.00</vFCP><vBCST>0.00</vBCST><vST>0.00</vST><vFCPST>0.00</vFCPST><vFCPSTRet>0.00</vFCPSTRet>");
        sb.Append($"<vProd>{D2(totalProdutos)}</vProd>");
        sb.Append("<vFrete>0.00</vFrete><vSeg>0.00</vSeg>");
        sb.Append($"<vDesc>{D2(totalDesconto)}</vDesc>");
        sb.Append("<vII>0.00</vII><vIPI>0.00</vIPI><vIPIDevol>0.00</vIPIDevol><vPIS>0.00</vPIS><vCOFINS>0.00</vCOFINS><vOutro>0.00</vOutro>");
        sb.Append($"<vNF>{D2(venda.TotalLiquido)}</vNF>");
        sb.Append($"<vTotTrib>{D2(totalTributos)}</vTotTrib>");
        sb.Append("</ICMSTot></total>");

        // transp
        sb.Append("<transp><modFrete>9</modFrete></transp>");

        // pag — REGRA SEFAZ: soma(vPag) = vNF + vTroco
        var pagamentos = venda.Pagamentos.Where(p => p.Valor > 0).ToList();
        var trocoReal = pagamentos.Sum(p => p.Troco);
        var totalNF = venda.TotalLiquido;

        sb.Append("<pag>");
        if (pagamentos.Count == 1)
        {
            var pag = pagamentos[0];
            var tPag = ObterCodigoPagamento(pag.TipoPagamento?.Modalidade);
            // vPag = totalNF + troco (garante a regra)
            sb.Append($"<detPag><tPag>{tPag}</tPag><vPag>{D2(totalNF + trocoReal)}</vPag></detPag>");
        }
        else
        {
            // Múltiplos: usar o valor proporcional ao totalNF
            var somaPag = pagamentos.Sum(p => p.Valor);
            decimal acumulado = 0;
            for (int i = 0; i < pagamentos.Count; i++)
            {
                var pag = pagamentos[i];
                var tPag = ObterCodigoPagamento(pag.TipoPagamento?.Modalidade);
                decimal vPag;
                if (i == pagamentos.Count - 1)
                    vPag = totalNF + trocoReal - acumulado; // último pega o resto para evitar centavo de diferença
                else
                    vPag = Math.Round(pag.Valor / somaPag * (totalNF + trocoReal), 2);
                acumulado += vPag;
                sb.Append($"<detPag><tPag>{tPag}</tPag><vPag>{D2(vPag)}</vPag></detPag>");
            }
        }
        sb.Append($"<vTroco>{D2(trocoReal)}</vTroco>");
        sb.Append("</pag>");

        sb.Append("<infAdic><infCpl>Documento emitido por ZulexPharma ERP</infCpl></infAdic>");

        // infRespTec (obrigatório no PR e outros estados)
        sb.Append("<infRespTec>");
        sb.Append($"<CNPJ>{cnpj}</CNPJ>");
        sb.Append($"<xContato>{Esc(filial.RazaoSocial)}</xContato>");
        sb.Append($"<email>{filial.Email}</email>");
        var foneTec = filial.Telefone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "");
        sb.Append($"<fone>{foneTec}</fone>");
        sb.Append("</infRespTec>");
        sb.Append("</infNFe>");
        sb.Append("</NFe>");
        return sb.ToString();
    }

    // ═══ Assinatura ═══
    private static string AssinarXml(string xml, X509Certificate2 cert)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);
        var signedXml = new SignedXml(doc) { SigningKey = cert.GetRSAPrivateKey() };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        var reference = new Reference("#" + doc.GetElementsByTagName("infNFe")[0]!.Attributes!["Id"]!.Value);
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
        signedXml.AddReference(reference);
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();
        var infNFe = doc.GetElementsByTagName("infNFe")[0]!;
        infNFe.ParentNode!.InsertAfter(doc.ImportNode(signedXml.GetXml(), true), infNFe);
        return doc.OuterXml;
    }

    // ═══ SOAP ═══
    private static string MontarSoapEnvelope(string xmlAssinado)
    {
        return "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
            "<soap12:Body>" +
            "<nfeDadosMsg xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4\">" +
            "<enviNFe xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"4.00\">" +
            "<idLote>1</idLote>" +
            "<indSinc>1</indSinc>" +
            xmlAssinado +
            "</enviNFe>" +
            "</nfeDadosMsg>" +
            "</soap12:Body>" +
            "</soap12:Envelope>";
    }

    private static async Task<string> EnviarSoap(string url, string soapXml, X509Certificate2 cert)
    {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        var content = new StringContent(soapXml, Encoding.UTF8, "application/soap+xml");
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }

    // ═══ Retorno ═══
    private static RetornoSefaz ProcessarRetorno(string xmlRetorno)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xmlRetorno);
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("nfe", "http://www.portalfiscal.inf.br/nfe");

        // Buscar primeiro no protNFe/infProt (status da nota individual)
        var cStat = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:cStat", ns)?.InnerText;
        var xMotivo = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:xMotivo", ns)?.InnerText;
        var nProt = doc.SelectSingleNode("//nfe:protNFe/nfe:infProt/nfe:nProt", ns)?.InnerText;

        // Se não encontrou no protNFe, pegar do retorno do lote (rejeição antes do processamento)
        if (string.IsNullOrEmpty(cStat))
        {
            cStat = doc.SelectSingleNode("//nfe:cStat", ns)?.InnerText ?? "0";
            xMotivo = doc.SelectSingleNode("//nfe:xMotivo", ns)?.InnerText ?? "";
        }

        return new RetornoSefaz
        {
            CodigoStatus = int.Parse(cStat ?? "0"),
            MotivoStatus = xMotivo ?? "",
            Protocolo = nProt,
            Autorizada = cStat == "100"
        };
    }

    // ═══ Utilitários ═══
    private static string GerarChaveAcesso(int uf, DateTime data, string cnpj, int modelo, int serie, int numero, int tipoEmissao, int codigoNumerico)
    {
        var chave = $"{uf:D2}{data:yyMM}{cnpj}{modelo:D2}{serie:D3}{numero:D9}{tipoEmissao}{codigoNumerico:D8}";
        var digito = CalcularDigitoVerificador(chave);
        return chave + digito;
    }

    private static int CalcularDigitoVerificador(string chave)
    {
        int peso = 2, soma = 0;
        for (var i = chave.Length - 1; i >= 0; i--) { soma += (chave[i] - '0') * peso; peso = peso == 9 ? 2 : peso + 1; }
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static string GerarUrlQrCode(string chaveAcesso, int ambiente, string csc, string cscId, string uf)
    {
        var baseUrl = ObterUrlQrCode(uf, ambiente);

        // Verificar se o estado usa QR Code v3 (PR e outros)
        if (uf.ToUpper() == "PR")
        {
            // QR Code v3: URL?p=CHAVE|3|AMBIENTE
            return $"{baseUrl}?p={chaveAcesso}|3|{ambiente}";
        }

        // QR Code v2 (demais estados):
        // URL?p=CHAVE|2|AMBIENTE|IDCSC|HASH
        using var sha1 = SHA1.Create();
        var conteudoHash = $"{cscId}{csc}{chaveAcesso}";
        var hash = Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(conteudoHash))).ToLower();
        return $"{baseUrl}?p={chaveAcesso}|2|{ambiente}|{cscId}|{hash}";
    }

    private static string ObterUrlAutorizacao(string uf, int ambiente) => uf.ToUpper() switch
    {
        "SP" => ambiente == 2 ? "https://homologacao.nfce.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx" : "https://nfce.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx",
        "PR" => ambiente == 2 ? "https://homologacao.nfce.sefa.pr.gov.br/nfce/NFeAutorizacao4" : "https://nfce.sefa.pr.gov.br/nfce/NFeAutorizacao4",
        "RS" => ambiente == 2 ? "https://nfce-homologacao.sefazrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" : "https://nfce.sefazrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx",
        "MG" => ambiente == 2 ? "https://hnfce.fazenda.mg.gov.br/nfce/services/NFeAutorizacao4" : "https://nfce.fazenda.mg.gov.br/nfce/services/NFeAutorizacao4",
        "SC" => ambiente == 2 ? "https://nfce-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" : "https://nfce.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx",
        _ => ambiente == 2 ? "https://nfce-homologacao.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx" : "https://nfce.svrs.rs.gov.br/ws/NfeAutorizacao/NFeAutorizacao4.asmx"
    };

    private static string ObterUrlQrCode(string uf, int ambiente) => uf.ToUpper() switch
    {
        "SP" => ambiente == 2 ? "https://homologacao.nfce.fazenda.sp.gov.br/NFCeConsultaPublica/Paginas/ConsultaQRCode.aspx" : "https://www.nfce.fazenda.sp.gov.br/NFCeConsultaPublica/Paginas/ConsultaQRCode.aspx",
        "PR" => "http://www.fazenda.pr.gov.br/nfce/qrcode",
        "SC" => ambiente == 2 ? "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx" : "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx",
        _ => ambiente == 2 ? "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx" : "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx"
    };

    private static string ObterUrlConsulta(string uf, int ambiente) => uf.ToUpper() switch
    {
        "SP" => "https://www.nfce.fazenda.sp.gov.br/consulta",
        "PR" => "http://www.fazenda.pr.gov.br/nfce/consulta",
        _ => "https://www.sefaz.rs.gov.br/NFCE/NFCE-COM.aspx"
    };

    private static int ObterCodigoUf(string uf) => uf.ToUpper() switch
    {
        "AC" => 12, "AL" => 27, "AP" => 16, "AM" => 13, "BA" => 29, "CE" => 23,
        "DF" => 53, "ES" => 32, "GO" => 52, "MA" => 21, "MT" => 51, "MS" => 50,
        "MG" => 31, "PA" => 15, "PB" => 25, "PR" => 41, "PE" => 26, "PI" => 22,
        "RJ" => 33, "RN" => 24, "RS" => 43, "RO" => 11, "RR" => 14, "SC" => 42,
        "SP" => 35, "SE" => 28, "TO" => 17, _ => 42
    };

    // ═══ DTOs ═══
    public class NfceResult
    {
        public long NfceId { get; set; }
        public int Numero { get; set; }
        public int Serie { get; set; }
        public string ChaveAcesso { get; set; } = "";
        public string? Protocolo { get; set; }
        public int CodigoStatus { get; set; }
        public string? MotivoStatus { get; set; }
        public bool Autorizada { get; set; }
    }

    private class RetornoSefaz
    {
        public int CodigoStatus { get; set; }
        public string? MotivoStatus { get; set; }
        public string? Protocolo { get; set; }
        public bool Autorizada { get; set; }
    }
}
