namespace ZulexPharma.Application.DTOs.SelfCheckout;

/// <summary>
/// Snapshot fiscal de um produto consultado no ERP origem.
/// Resolvido pela hierarquia: Produto_Fiscal_UF (específico do produto/UF/regime)
/// → Produto_NCM_UF (por NCM/UF) → Produto_NCM (por NCM puro), junto com
/// Produto_NCM_Tributo (CSOSN) e os campos diretos do Produto (NCM, CEST,
/// Origem, PisCofinsCST, PisCofinsNatureza).
///
/// Mapeia direto para os campos de <c>VendaItemFiscal</c> usados na NFC-e.
/// </summary>
public class ProdutoFiscalSnapshotDto
{
    public string Ncm { get; set; } = "00000000";
    public string? Cest { get; set; }
    public string OrigemMercadoria { get; set; } = "0";
    public string Cfop { get; set; } = "5102";
    public string Unidade { get; set; } = "UN";

    // ── ICMS ─────────────────────────────────────────────────────
    public string? CstIcms { get; set; }
    public string? Csosn { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal AliquotaFcp { get; set; }
    public decimal PercentualReducaoBc { get; set; }
    public string? CodigoBeneficioFiscal { get; set; }

    // ── ICMS-ST (preparado, normalmente zero para NFC-e padrão) ──
    public decimal AliquotaIcmsSt { get; set; }
    public decimal MvaSt { get; set; }
    public decimal AliquotaFcpSt { get; set; }

    // ── PIS ──────────────────────────────────────────────────────
    public string CstPis { get; set; } = "49";
    public decimal AliquotaPis { get; set; }

    // ── COFINS ───────────────────────────────────────────────────
    public string CstCofins { get; set; } = "49";
    public decimal AliquotaCofins { get; set; }

    // ── IPI ──────────────────────────────────────────────────────
    public string? CstIpi { get; set; }
    public decimal AliquotaIpi { get; set; }
}
