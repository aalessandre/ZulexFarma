namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Substituição Tributária por NCM + par UF origem/destino.
/// Crítico para farmácias — medicamentos quase sempre têm ST.
/// </summary>
public class NcmStUf : BaseEntity
{
    public long NcmId { get; set; }
    public Ncm Ncm { get; set; } = null!;

    public string UfOrigem { get; set; } = "";
    public string UfDestino { get; set; } = "";

    /// <summary>Margem de Valor Agregado (%)</summary>
    public decimal Mva { get; set; }

    /// <summary>MVA Ajustado para operações interestaduais (%)</summary>
    public decimal MvaAjustado { get; set; }

    public decimal AliquotaIcmsSt { get; set; }
    public decimal ReducaoBaseCalculoSt { get; set; }

    /// <summary>Código CEST</summary>
    public string? Cest { get; set; }

    public DateTime? VigenciaInicio { get; set; }
    public DateTime? VigenciaFim { get; set; }
}
