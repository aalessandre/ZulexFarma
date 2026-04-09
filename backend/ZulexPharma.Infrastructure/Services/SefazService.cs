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
using ZulexPharma.Domain.Helpers;
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
            Valido = cert.Validade > DataHoraHelper.Agora(),
            DiasParaVencer = (int)(cert.Validade - DataHoraHelper.Agora()).TotalDays
        };
    }

    // ── Consultar NF-e pendentes no SEFAZ ────────────────────────────
    public async Task<ConsultaSefazResult> ConsultarNfePendentesAsync(long filialId)
    {
        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == filialId)
            ?? throw new ArgumentException("Certificado digital nao configurado para esta filial.");

        if (certDb.Validade <= DataHoraHelper.Agora())
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

        if (certDb.Validade <= DataHoraHelper.Agora())
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

    // ── Listar notas do cache ───────────────────────────────────────
    public async Task<List<SefazNotaDto>> ListarNotasAsync(long filialId, DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        var query = _db.SefazNotas.Where(n => n.FilialId == filialId);

        if (dataInicio.HasValue)
            query = query.Where(n => n.DataEmissao >= dataInicio.Value);
        if (dataFim.HasValue)
            query = query.Where(n => n.DataEmissao < dataFim.Value.AddDays(1));

        // Verificar quais estão importadas/lançadas via tabela Compras
        var notas = await query.OrderByDescending(n => n.DataEmissao ?? n.ConsultadaEm).ToListAsync();
        var chaves = notas.Select(n => n.ChaveNfe).ToList();
        var compras = await _db.Compras
            .Where(c => chaves.Contains(c.ChaveNfe))
            .Select(c => new { c.ChaveNfe, c.Status })
            .ToListAsync();
        var comprasMap = compras.ToDictionary(c => c.ChaveNfe, c => c.Status);

        return notas.Select(n =>
        {
            var importada = comprasMap.ContainsKey(n.ChaveNfe);
            var lancada = importada && comprasMap[n.ChaveNfe] == Domain.Enums.CompraStatus.Finalizada;
            return new SefazNotaDto
            {
                Id = n.Id, ChaveNfe = n.ChaveNfe, Cnpj = n.Cnpj, RazaoSocial = n.RazaoSocial,
                NumeroNf = n.NumeroNf, SerieNf = n.SerieNf, DataEmissao = n.DataEmissao,
                ValorNota = n.ValorNota, Situacao = n.Situacao, TipoDocumento = n.TipoDocumento,
                TemXml = !string.IsNullOrEmpty(n.XmlCompleto), Manifestada = n.Manifestada,
                TipoManifestacao = n.TipoManifestacao, Importada = importada, Lancada = lancada,
                ConsultadaEm = n.ConsultadaEm
            };
        }).ToList();
    }

    // ── Manifestar ───────────────────────────────────────────────────
    public async Task ManifestarAsync(ManifestacaoRequest request)
    {
        var certDb = await _db.CertificadosDigitais.FirstOrDefaultAsync(c => c.FilialId == request.FilialId)
            ?? throw new ArgumentException("Certificado digital nao configurado.");

        var filial = await _db.Filiais.FindAsync(request.FilialId)
            ?? throw new KeyNotFoundException("Filial nao encontrada.");

        var ufCodigo = ObterCodigoUf(filial.Uf);
        var cnpj = certDb.Cnpj.Replace(".", "").Replace("/", "").Replace("-", "");

        var tipoDesc = request.TipoEvento switch
        {
            210210 => "Ciencia da Operacao",
            210220 => "Desconhecimento da Operacao",
            210240 => "Operacao nao Realizada",
            _ => throw new ArgumentException("Tipo de evento invalido.")
        };

        var soapXml = MontarSoapManifestacao(ufCodigo, cnpj, request.ChaveNfe, request.TipoEvento, request.Justificativa);
        var pfxBytes = Convert.FromBase64String(certDb.PfxBase64);
        var cert = new X509Certificate2(pfxBytes, certDb.Senha, X509KeyStorageFlags.Exportable);

        try
        {
            var responseXml = await EnviarSoapEvento(soapXml, cert);
            // Atualizar cache
            var nota = await _db.SefazNotas.FirstOrDefaultAsync(n => n.FilialId == request.FilialId && n.ChaveNfe == request.ChaveNfe);
            if (nota != null)
            {
                nota.Manifestada = true;
                nota.TipoManifestacao = tipoDesc;
                await _db.SaveChangesAsync();
            }

            // Se foi ciência, buscar XML completo por chave
            if (request.TipoEvento == 210210)
            {
                // Aguardar um pouco para SEFAZ processar
                await Task.Delay(2000);
                var resultado = await ConsultarPorChaveAsync(request.FilialId, request.ChaveNfe);
                // O ProcessarResposta já salva no cache com XML completo
            }
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

    private static string MontarSoapManifestacao(int ufCodigo, string cnpj, string chaveNfe, int tpEvento, string? justificativa)
    {
        var seqEvento = 1;
        var detEvento = tpEvento == 210240
            ? $@"<detEvento versao=""1.00""><descEvento>Operacao nao Realizada</descEvento><xJust>{justificativa ?? "Mercadoria nao recebida"}</xJust></detEvento>"
            : tpEvento == 210220
            ? $@"<detEvento versao=""1.00""><descEvento>Desconhecimento da Operacao</descEvento></detEvento>"
            : $@"<detEvento versao=""1.00""><descEvento>Ciencia da Operacao</descEvento></detEvento>";

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Body>
    <nfeRecepcaoEvento xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4"">
      <nfeDadosMsg>
        <envEvento xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""1.00"">
          <idLote>1</idLote>
          <evento versao=""1.00"">
            <infEvento Id=""ID{tpEvento}{chaveNfe}{seqEvento:00}"">
              <cOrgao>{ufCodigo}</cOrgao>
              <tpAmb>1</tpAmb>
              <CNPJ>{cnpj}</CNPJ>
              <chNFe>{chaveNfe}</chNFe>
              <dhEvento>{DateTimeOffset.Now:yyyy-MM-ddTHH:mm:sszzz}</dhEvento>
              <tpEvento>{tpEvento}</tpEvento>
              <nSeqEvento>{seqEvento}</nSeqEvento>
              <verEvento>1.00</verEvento>
              {detEvento}
            </infEvento>
          </evento>
        </envEvento>
      </nfeDadosMsg>
    </nfeRecepcaoEvento>
  </soap12:Body>
</soap12:Envelope>";
    }

    private static async Task<string> EnviarSoapEvento(string soapXml, X509Certificate2 cert)
    {
        var url = "https://www.nfe.fazenda.gov.br/NFeRecepcaoEvento4/NFeRecepcaoEvento4.asmx";
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(cert);
        handler.ServerCertificateCustomValidationCallback = (msg, c, chain, errors) => true;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        var content = new StringContent(soapXml, Encoding.UTF8, "application/soap+xml");
        var response = await client.PostAsync(url, content);
        return await response.Content.ReadAsStringAsync();
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

            // Salvar no cache
            foreach (var n in notas)
                await SalvarNoCache(filialId, n);

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

    private async Task SalvarNoCache(long filialId, NfeSefazResumo n)
    {
        if (string.IsNullOrEmpty(n.ChaveNfe)) return;
        var existente = await _db.SefazNotas.FirstOrDefaultAsync(s => s.FilialId == filialId && s.ChaveNfe == n.ChaveNfe);
        if (existente != null)
        {
            // Atualizar se agora tem XML completo
            if (!string.IsNullOrEmpty(n.XmlCompleto) && string.IsNullOrEmpty(existente.XmlCompleto))
            {
                existente.XmlCompleto = n.XmlCompleto;
                existente.TipoDocumento = "procNFe";
            }
            if (!string.IsNullOrEmpty(n.NumeroNf)) existente.NumeroNf = n.NumeroNf;
            if (!string.IsNullOrEmpty(n.SerieNf)) existente.SerieNf = n.SerieNf;
            if (!string.IsNullOrEmpty(n.RazaoSocial)) existente.RazaoSocial = n.RazaoSocial;
        }
        else
        {
            _db.SefazNotas.Add(new SefazNota
            {
                FilialId = filialId,
                ChaveNfe = n.ChaveNfe,
                Cnpj = n.Cnpj,
                RazaoSocial = n.RazaoSocial,
                NumeroNf = n.NumeroNf,
                SerieNf = n.SerieNf,
                DataEmissao = n.DataEmissao.HasValue ? DateTime.SpecifyKind(n.DataEmissao.Value, DateTimeKind.Utc) : null,
                ValorNota = n.ValorNota,
                Situacao = n.Situacao ?? "Autorizada",
                TipoDocumento = string.IsNullOrEmpty(n.XmlCompleto) ? "resNFe" : "procNFe",
                XmlCompleto = n.XmlCompleto
            });
        }
        await _db.SaveChangesAsync();
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
