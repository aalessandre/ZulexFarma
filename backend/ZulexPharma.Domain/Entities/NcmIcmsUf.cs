namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Alíquotas e regras de ICMS por NCM + UF.
/// </summary>
public class NcmIcmsUf : BaseEntity
{
    public long NcmId { get; set; }
    public Ncm Ncm { get; set; } = null!;

    /// <summary>UF do estado (ex: "SP", "RJ")</summary>
    public string Uf { get; set; } = "";

    public string? CstIcms { get; set; }
    public string? Csosn { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal ReducaoBaseCalculo { get; set; }

    /// <summary>Fundo de Combate à Pobreza</summary>
    public decimal AliquotaFcp { get; set; }

    /// <summary>Código de Benefício Fiscal</summary>
    public string? Cbenef { get; set; }

    public DateTime? VigenciaInicio { get; set; }
    public DateTime? VigenciaFim { get; set; }
}
