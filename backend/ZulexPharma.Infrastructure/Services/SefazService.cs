using Microsoft.EntityFrameworkCore;
using Serilog;
using System.IO.Compression;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using ZulexPharma.Application.DTOs.Fiscal;
using ZulexPharma.Application.Interfaces;
using ZulexPharma.Domain.Entities;
using ZulexPharma.Infrastructure.Data;

namespace ZulexPharma.Infrastructure.Services;

/// <summary>
/// Comunicação direta com SEFAZ via SOAP para DistribuicaoDFe.
/// Não depende de libs externas para o SOAP — apenas System.Net.Http + certificado.
/// </summary>
public class SefazService : ISefazService
{
    private readonly AppDbContext _db;

    private const string URL_PRODUCAO = "https://www1.nfe.fazenda.gov.br/NFeDistribuicaoDFe/NFeDistribuicaoDFe.asmx";
    private const string URL_HOMOLOGACAO = "https://hom.nfe.fazenda.gov.br/NFeDistribuicaoDFe/NFeDistribuicaoDFe.asmx";
    private static readonly XNamespace _nfeNs = "http://www.portalfiscal.inf.br/nfe";
    private static readonly XNamespace _soapNs = "http://www.w3.org/2003/05/soap-envelope";

    public SefazService(AppDbContext db)
    {
        _db = db;
    }

    // ── Upload Certificado ──────────────────────────────────────────
    public async Task<CertificadoInfoDto> UploadCertificadoAsync(CertificadoUploadRequest request)
    {
        var pfxBytes = Convert.FromBase64String(request.PfxBase64);

        X509Certificate2 cert;
        try
        {
            cert = new X509Certificate2(pfxBytes, request.Senha, X509KeyStorageFlags.Exportable);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Certificado invalido ou senha incorreta: {ex.Message}");
        }

        var cnpj = ExtrairCnpj(cert);
        var razaoSocial = cert.GetNameInfo(X509NameType.SimpleName, false);

        var existente = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == request.FilialId);

        if (existente != null)
        {
            existente.PfxBase64 = request.PfxBase64;
            existente.Senha = request.Senha;
            existente.Cnpj = cnpj;
            existente.RazaoSocial = razaoSocial;
            existente.Validade = cert.NotAfter;
            existente.Emissor = cert.Issuer;
        }
        else
        {
            _db.CertificadosDigitais.Add(new CertificadoDigital
            {
                FilialId = request.FilialId,
                PfxBase64 = request.PfxBase64,
                Senha = request.Senha,
                Cnpj = cnpj,
                RazaoSocial = razaoSocial,
                Validade = cert.NotAfter,
                Emissor = cert.Issuer
            });
        }

        await _db.SaveChangesAsync();
        cert.Dispose();

        return await ObterCertificadoAsync(request.FilialId)
            ?? throw new InvalidOperationException("Erro ao salvar certificado.");
    }

    // ── Obter info certificado ───────────────────────────────────────
    public async Task<CertificadoInfoDto?> ObterCertificadoAsync(long filialId)
    {
        var cert = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filialId);
        if (cert == null) return null;

        return new CertificadoInfoDto
        {
            Id = cert.Id,
            FilialId = cert.FilialId,
            Cnpj = cert.Cnpj,
            RazaoSocial = cert.RazaoSocial,
            Validade = cert.Validade,
            Emissor = cert.Emissor,
            Valido = cert.Validade > DateTime.UtcNow,
            DiasParaVencer = (int)(cert.Validade - DateTime.UtcNow).TotalDays
        };
    }

    // ── Consultar NF-e pendentes no SEFAZ ────────────────────────────
    public async Task<ConsultaSefazResult> ConsultarNfePendentesAsync(long filialId)
    {
        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filialId)
            ?? throw new ArgumentException("Certificado digital nao configurado para esta filial.");

        if (certDb.Validade <= DateTime.UtcNow)
            throw new ArgumentException("Certificado digital expirado.");

        var filial = await _db.Filiais.FindAsync(filialId)
            ?? throw new KeyNotFoundException("Filial nao encontrada.");

        var ufCodigo = ObterCodigoUf(filial.Uf);
        var cnpj = certDb.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "");
        var ultimoNsu = await ObterUltimoNsu(filialId);

        // Montar SOAP para distNSU
        var soapXml = MontarSoapDistNSU(ufCodigo, cnpj, ultimoNsu);

        // Enviar com certificado
        var pfxBytes = Convert.FromBase64String(certDb.PfxBase64);
        var cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);

        try
        {
            var responseXml = await EnviarSoap(URL_PRODUCAO, soapXml, cert);
            return await ProcessarResposta(responseXml, filialId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao consultar SEFAZ");
            throw new InvalidOperationException($"Erro na comunicacao com SEFAZ: {ex.Message}");
        }
        finally
        {
            cert.Dispose();
        }
    }

    // ── Consultar NF-e por chave ────────────────────────────────────
    public async Task<ConsultaSefazResult> ConsultarPorChaveAsync(long filialId, string chaveNfe)
    {
        if (string.IsNullOrEmpty(chaveNfe) || chaveNfe.Length != 44)
            throw new ArgumentException("Chave da NF-e deve ter 44 digitos.");

        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filialId)
            ?? throw new ArgumentException("Certificado digital nao configurado.");

        if (certDb.Validade <= DateTime.UtcNow)
            throw new ArgumentException("Certificado digital expirado.");

        var filial = await _db.Filiais.FindAsync(filialId)
            ?? throw new KeyNotFoundException("Filial nao encontrada.");

        var ufCodigo = ObterCodigoUf(filial.Uf);
        var cnpj = certDb.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "");

        var soapXml = MontarSoapConsChNFe(ufCodigo, cnpj, chaveNfe);

        var pfxBytes = Convert.FromBase64String(certDb.PfxBase64);
        var cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);

        try
        {
            var responseXml = await EnviarSoap(URL_PRODUCAO, soapXml, cert);
            return await ProcessarResposta(responseXml, filialId);
        }
        finally
        {
            cert.Dispose();
        }
    }

    // ═════════════════════════════════════════════════════════════════
    // SOAP / HTTP
    // ═════════════════════════════════════════════════════════════════

    private static string MontarSoapDistNSU(int ufCodigo, string cnpj, string ultNSU)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Header>
    <nfeCabecMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeDistribuicaoDFe"">
      <cUF>{ufCodigo}</cUF>
      <versaoDados>1.01</versaoDados>
    </nfeCabecMsg>
  </soap12:Header>
  <soap12:Body>
    <nfeDistDFeInteresse xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeDistribuicaoDFe"">
      <nfeDadosMsg>
        <distDFeInt xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""1.01"">
          <tpAmb>1</tpAmb>
          <cUFAutor>{ufCodigo}</cUFAutor>
          <CNPJ>{cnpj}</CNPJ>
          <distNSU>
            <ultNSU>{ultNSU.PadLeft(15, '0')}</ultNSU>
          </distNSU>
        </distDFeInt>
      </nfeDadosMsg>
    </nfeDistDFeInteresse>
  </soap12:Body>
</soap12:Envelope>";
    }

    private static string MontarSoapConsChNFe(int ufCodigo, string cnpj, string chaveNfe)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Header>
    <nfeCabecMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeDistribuicaoDFe"">
      <cUF>{ufCodigo}</cUF>
      <versaoDados>1.01</versaoDados>
    </nfeCabecMsg>
  </soap12:Header>
  <soap12:Body>
    <nfeDistDFeInteresse xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeDistribuicaoDFe"">
      <nfeDadosMsg>
        <distDFeInt xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""1.01"">
          <tpAmb>1</tpAmb>
          <cUFAutor>{ufCodigo}</cUFAutor>
          <CNPJ>{cnpj}</CNPJ>
          <consChNFe>
            <chNFe>{chaveNfe}</chNFe>
          </consChNFe>
        </distDFeInt>
      </nfeDadosMsg>
    </nfeDistDFeInteresse>
  </soap12:Body>
</soap12:Envelope>";
    }

    private static async Task<string> EnviarSoap(string url, string soapXml, X509Certificate2 cert)
    {
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        handler.ServerCertificateCustomValidationCallback = (msg, c, chain, errors) => true;

        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        var content = new StringContent(soapXml, Encoding.UTF8, "application/soap+xml");
        content.Headers.ContentType!.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("action", "\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeDistribuicaoDFe/nfeDistDFeInteresse\""));

        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
    }

    private async Task<ConsultaSefazResult> ProcessarResposta(string responseXml, long filialId)
    {
        var resultado = new ConsultaSefazResult();

        try
        {
            var doc = XDocument.Parse(responseXml);
            var retDistDFeInt = doc.Descendants(_nfeNs + "retDistDFeInt").FirstOrDefault();

            if (retDistDFeInt == null)
            {
                resultado.Mensagem = "Resposta inesperada da SEFAZ.";
                return resultado;
            }

            var cStat = retDistDFeInt.Element(_nfeNs + "cStat")?.Value;
            var xMotivo = retDistDFeInt.Element(_nfeNs + "xMotivo")?.Value;
            var ultNSU = retDistDFeInt.Element(_nfeNs + "ultNSU")?.Value ?? "0";

            if (cStat != "138") // 138 = Documentos localizados
            {
                resultado.Mensagem = $"{cStat} - {xMotivo}";
                await SalvarUltimoNsu(filialId, ultNSU);
                return resultado;
            }

            var lote = retDistDFeInt.Element(_nfeNs + "loteDistDFeInt");
            if (lote == null) { resultado.Mensagem = xMotivo; return resultado; }

            var notas = new List<NfeSefazResumo>();

            foreach (var docZip in lote.Elements(_nfeNs + "docZip"))
            {
                try
                {
                    var nsuAttr = docZip.Attribute("NSU")?.Value;
                    var schema = docZip.Attribute("schema")?.Value ?? "";
                    var base64 = docZip.Value;

                    // Descompactar gzip
                    var xmlBytes = Convert.FromBase64String(base64);
                    var xml = DescompactarGzip(xmlBytes);

                    var xDoc = XDocument.Parse(xml);

                    if (schema.Contains("resNFe"))
                    {
                        // Resumo da NF-e
                        var resNFe = xDoc.Root;
                        var chave = resNFe?.Element(_nfeNs + "chNFe")?.Value ?? nsuAttr ?? "";
                        notas.Add(new NfeSefazResumo
                        {
                            ChaveNfe = chave,
                            Cnpj = resNFe?.Element(_nfeNs + "CNPJ")?.Value,
                            RazaoSocial = resNFe?.Element(_nfeNs + "xNome")?.Value,
                            DataEmissao = DateTime.TryParse(resNFe?.Element(_nfeNs + "dhEmi")?.Value, out var dt) ? dt : null,
                            ValorNota = decimal.TryParse(resNFe?.Element(_nfeNs + "vNF")?.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0,
                            Situacao = resNFe?.Element(_nfeNs + "cSitNFe")?.Value == "1" ? "Autorizada" : "Cancelada",
                            JaImportada = await _db.Compras.AnyAsync(c => c.ChaveNfe == chave)
                        });
                    }
                    else if (schema.Contains("procNFe") || schema.Contains("nfeProc"))
                    {
                        // XML completo
                        var infNFe = xDoc.Descendants(_nfeNs + "infNFe").FirstOrDefault();
                        var ide = infNFe?.Element(_nfeNs + "ide");
                        var emit = infNFe?.Element(_nfeNs + "emit");
                        var total = infNFe?.Element(_nfeNs + "total")?.Element(_nfeNs + "ICMSTot");
                        var chave = infNFe?.Attribute("Id")?.Value?.Replace("NFe", "") ?? "";

                        notas.Add(new NfeSefazResumo
                        {
                            ChaveNfe = chave,
                            Cnpj = emit?.Element(_nfeNs + "CNPJ")?.Value,
                            RazaoSocial = emit?.Element(_nfeNs + "xNome")?.Value,
                            NumeroNf = ide?.Element(_nfeNs + "nNF")?.Value,
                            SerieNf = ide?.Element(_nfeNs + "serie")?.Value,
                            DataEmissao = DateTime.TryParse(ide?.Element(_nfeNs + "dhEmi")?.Value, out var dt2) ? dt2 : null,
                            ValorNota = decimal.TryParse(total?.Element(_nfeNs + "vNF")?.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v2) ? v2 : 0,
                            Situacao = "Autorizada",
                            XmlCompleto = xml,
                            JaImportada = await _db.Compras.AnyAsync(c => c.ChaveNfe == chave)
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("Erro ao processar docZip: {Msg}", ex.Message);
                }
            }

            await SalvarUltimoNsu(filialId, ultNSU);

            resultado.TotalNotas = notas.Count;
            resultado.NotasNovas = notas.Count(n => !n.JaImportada);
            resultado.UltimoNsu = ultNSU;
            resultado.Notas = notas;
            resultado.Mensagem = $"{notas.Count} nota(s) encontrada(s).";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao processar resposta SEFAZ");
            resultado.Mensagem = $"Erro ao processar resposta: {ex.Message}";
        }

        return resultado;
    }

    // ═════════════════════════════════════════════════════════════════
    // HELPERS
    // ═════════════════════════════════════════════════════════════════

    private static string ExtrairCnpj(X509Certificate2 cert)
    {
        var subject = cert.Subject;
        foreach (var part in subject.Split(','))
        {
            var trimmed = part.Trim();
            var digits = new string(trimmed.Where(char.IsDigit).ToArray());
            if (digits.Length == 14 && digits.Distinct().Count() > 1) return digits;
        }
        var allDigits = new string(subject.Where(char.IsDigit).ToArray());
        for (int i = 0; i <= allDigits.Length - 14; i++)
        {
            var candidate = allDigits.Substring(i, 14);
            if (candidate.Distinct().Count() > 1) return candidate;
        }
        return "";
    }

    private static string DescompactarGzip(byte[] data)
    {
        try
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            return reader.ReadToEnd();
        }
        catch
        {
            return Encoding.UTF8.GetString(data);
        }
    }

    private async Task<string> ObterUltimoNsu(long filialId)
    {
        var config = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == $"sefaz.ultimo.nsu.{filialId}");
        return config?.Valor ?? "0";
    }

    private async Task SalvarUltimoNsu(long filialId, string nsu)
    {
        var chave = $"sefaz.ultimo.nsu.{filialId}";
        var config = await _db.Configuracoes.FirstOrDefaultAsync(c => c.Chave == chave);
        if (config != null) config.Valor = nsu;
        else _db.Configuracoes.Add(new Configuracao { Chave = chave, Valor = nsu });
        _db.AplicandoSync = true;
        await _db.SaveChangesAsync();
        _db.AplicandoSync = false;
    }

    private static int ObterCodigoUf(string uf)
    {
        return uf.ToUpper() switch
        {
            "AC" => 12, "AL" => 27, "AP" => 16, "AM" => 13, "BA" => 29, "CE" => 23,
            "DF" => 53, "ES" => 32, "GO" => 52, "MA" => 21, "MT" => 51, "MS" => 50,
            "MG" => 31, "PA" => 15, "PB" => 25, "PR" => 41, "PE" => 26, "PI" => 22,
            "RJ" => 33, "RN" => 24, "RS" => 43, "RO" => 11, "RR" => 14, "SC" => 42,
            "SP" => 35, "SE" => 28, "TO" => 17, _ => 41
        };
    }
}
