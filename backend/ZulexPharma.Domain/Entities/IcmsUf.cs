namespace ZulexPharma.Domain.Entities;

/// <summary>Alíquota de ICMS por UF (tabela de referência).</summary>
public class IcmsUf : BaseEntity
{
    public string Uf { get; set; } = string.Empty;
    public string NomeEstado { get; set; } = string.Empty;
    public decimal AliquotaInterna { get; set; }
}
