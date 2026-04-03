namespace ZulexPharma.Domain.Entities;

/// <summary>Dados fiscais de um item da nota fiscal de entrada.</summary>
public class CompraFiscal : BaseEntity
{
    public long CompraProdutoId { get; set; }
    public CompraProduto CompraProduto { get; set; } = null!;

    // ── ICMS ────────────────────────────────────────────────────
    public string? OrigemMercadoria { get; set; }
    public string? CstIcms { get; set; }
    public decimal BaseIcms { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal ValorIcms { get; set; }
    public string? ModalidadeBcSt { get; set; }
    public decimal MvaSt { get; set; }
    public decimal BaseSt { get; set; }
    public decimal AliquotaSt { get; set; }
    public decimal ValorSt { get; set; }
    public decimal BaseFcpSt { get; set; }
    public decimal AliquotaFcpSt { get; set; }
    public decimal ValorFcpSt { get; set; }

    // ── PIS ─────────────────────────────────────────────────────
    public string? CstPis { get; set; }
    public decimal BasePis { get; set; }
    public decimal AliquotaPis { get; set; }
    public decimal ValorPis { get; set; }

    // ── COFINS ──────────────────────────────────────────────────
    public string? CstCofins { get; set; }
    public decimal BaseCofins { get; set; }
    public decimal AliquotaCofins { get; set; }
    public decimal ValorCofins { get; set; }

    // ── IBS/CBS (Reforma Tributária) ────────────────────────────
    public string? CstIbsCbs { get; set; }
    public string? ClasseTributariaIbsCbs { get; set; }
    public decimal BaseIbsCbs { get; set; }
    public decimal AliquotaIbsUf { get; set; }
    public decimal ValorIbsUf { get; set; }
    public decimal AliquotaIbsMun { get; set; }
    public decimal ValorIbsMun { get; set; }
    public decimal AliquotaCbs { get; set; }
    public decimal ValorCbs { get; set; }
}
