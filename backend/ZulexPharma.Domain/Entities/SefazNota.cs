namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Cache local de notas consultadas no SEFAZ via DistribuicaoDFe.
/// NÃO replica entre filiais (cada filial consulta suas notas).
/// </summary>
public class SefazNota
{
    public long Id { get; set; }
    public long FilialId { get; set; }
    public string ChaveNfe { get; set; } = string.Empty;
    public long Nsu { get; set; }

    // ── Dados do emitente ───────────────────────────────────────
    public string? Cnpj { get; set; }
    public string? RazaoSocial { get; set; }

    // ── Dados da nota ───────────────────────────────────────────
    public string? NumeroNf { get; set; }
    public string? SerieNf { get; set; }
    public DateTime? DataEmissao { get; set; }
    public decimal ValorNota { get; set; }
    public string Situacao { get; set; } = "Autorizada";

    // ── XML ─────────────────────────────────────────────────────
    /// <summary>resNFe ou procNFe</summary>
    public string TipoDocumento { get; set; } = "resNFe";
    public string? XmlCompleto { get; set; }

    // ── Status ──────────────────────────────────────────────────
    public bool Manifestada { get; set; }
    public string? TipoManifestacao { get; set; }
    public bool Importada { get; set; }
    public bool Lancada { get; set; }
    public DateTime ConsultadaEm { get; set; } = DateTime.UtcNow;
}
