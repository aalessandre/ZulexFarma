namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Certificado digital A1 da filial para comunicação com SEFAZ.
/// Armazena o .pfx em Base64 no banco (criptografado futuramente).
/// NÃO replica entre filiais (cada filial tem seu certificado).
/// </summary>
public class CertificadoDigital : BaseEntity
{
    public long FilialId { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }

    /// <summary>Conteúdo do arquivo .pfx em Base64.</summary>
    public string PfxBase64 { get; set; } = string.Empty;

    /// <summary>Senha do certificado (em produção, criptografar).</summary>
    public string Senha { get; set; } = string.Empty;

    public DateTime Validade { get; set; }
    public string? Emissor { get; set; }
}
