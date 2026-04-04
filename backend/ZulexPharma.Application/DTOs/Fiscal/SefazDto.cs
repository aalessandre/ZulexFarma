namespace ZulexPharma.Application.DTOs.Fiscal;

// ── Certificado Digital ─────────────────────────────────────────────
public class CertificadoUploadRequest
{
    public long FilialId { get; set; }
    public string PfxBase64 { get; set; } = "";
    public string Senha { get; set; } = "";
}

public class CertificadoInfoDto
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public string Cnpj { get; set; } = "";
    public string? RazaoSocial { get; set; }
    public DateTime Validade { get; set; }
    public string? Emissor { get; set; }
    public bool Valido { get; set; }
    public int DiasParaVencer { get; set; }
}

// ── Consulta SEFAZ ──────────────────────────────────────────────────
public class ConsultaSefazRequest
{
    public long FilialId { get; set; }
}

public class NfeSefazResumo
{
    public string ChaveNfe { get; set; } = "";
    public string? Cnpj { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NumeroNf { get; set; }
    public string? SerieNf { get; set; }
    public DateTime? DataEmissao { get; set; }
    public decimal ValorNota { get; set; }
    public string? Situacao { get; set; }
    public string? XmlCompleto { get; set; }
    public bool JaImportada { get; set; }
}

public class ConsultaSefazResult
{
    public int TotalNotas { get; set; }
    public int NotasNovas { get; set; }
    public string? UltimoNsu { get; set; }
    public List<NfeSefazResumo> Notas { get; set; } = new();
    public string? Mensagem { get; set; }
}
