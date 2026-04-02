namespace ZulexPharma.Domain.Entities;

/// <summary>Dados fiscais/tributários do produto por filial.</summary>
public class ProdutoFiscal : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public long FilialId { get; set; }

    public long? NcmId { get; set; }
    public Ncm? Ncm { get; set; }

    public string? Cest { get; set; }
    public string? OrigemMercadoria { get; set; }

    // ICMS
    public string? CstIcms { get; set; }
    public string? Csosn { get; set; }
    public decimal AliquotaIcms { get; set; }

    // PIS
    public string? CstPis { get; set; }
    public decimal AliquotaPis { get; set; }

    // COFINS
    public string? CstCofins { get; set; }
    public decimal AliquotaCofins { get; set; }

    // IPI
    public string? CstIpi { get; set; }
    public decimal AliquotaIpi { get; set; }
}
