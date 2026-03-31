namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Tributos federais vinculados a um NCM (IPI, PIS, COFINS, II).
/// </summary>
public class NcmFederal : BaseEntity
{
    public long NcmId { get; set; }
    public Ncm Ncm { get; set; } = null!;

    public decimal AliquotaIi { get; set; }
    public decimal AliquotaIpi { get; set; }
    public string? CstIpi { get; set; }
    public decimal AliquotaPis { get; set; }
    public string? CstPis { get; set; }
    public decimal AliquotaCofins { get; set; }
    public string? CstCofins { get; set; }

    public DateTime? VigenciaInicio { get; set; }
    public DateTime? VigenciaFim { get; set; }
}
