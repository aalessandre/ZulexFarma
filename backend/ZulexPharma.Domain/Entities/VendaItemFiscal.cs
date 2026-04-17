namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Snapshot fiscal de um VendaItem no momento da emissão do documento.
/// Relação 1:1 com VendaItem. Campos calculados a partir de NaturezaOperacao,
/// regime tributário, NCM, UF destino, etc.
/// </summary>
public class VendaItemFiscal : BaseEntity
{
    public long VendaItemId { get; set; }
    public VendaItem VendaItem { get; set; } = null!;

    public int NumeroItem { get; set; }

    // ── prod (snapshot; pode divergir de Produto se o cadastro mudar depois) ──
    public string CodigoProduto { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = "SEM GTIN";
    public string DescricaoProduto { get; set; } = string.Empty;
    public string Ncm { get; set; } = string.Empty;
    public string? Cest { get; set; }
    public string Cfop { get; set; } = string.Empty;
    public string Unidade { get; set; } = "UN";
    public decimal ValorFrete { get; set; }
    public decimal ValorSeguro { get; set; }
    public decimal ValorOutros { get; set; }
    public int IndicadorTotal { get; set; } = 1;

    // ── Medicamento / Rastro ────────────────────────────────────
    public string? CodigoAnvisa { get; set; }
    public string? RastroLote { get; set; }
    public DateTime? RastroFabricacao { get; set; }
    public DateTime? RastroValidade { get; set; }
    public decimal? RastroQuantidade { get; set; }

    // ── ICMS ─────────────────────────────────────────────────────
    public string OrigemMercadoria { get; set; } = "0";
    public string? CstIcms { get; set; }
    public string? Csosn { get; set; }
    public string? ModBcIcms { get; set; }
    public decimal BaseIcms { get; set; }
    public decimal AliquotaIcms { get; set; }
    public decimal ValorIcms { get; set; }
    public decimal PercentualReducaoBc { get; set; }
    public decimal ValorIcmsDesonerado { get; set; }
    public string? MotivoDesoneracaoIcms { get; set; }
    public string? CodigoBeneficioFiscal { get; set; }

    // ── ICMS-ST ──────────────────────────────────────────────────
    public string? ModBcIcmsSt { get; set; }
    public decimal MvaSt { get; set; }
    public decimal BaseIcmsSt { get; set; }
    public decimal AliquotaIcmsSt { get; set; }
    public decimal ValorIcmsSt { get; set; }

    // ── FCP ──────────────────────────────────────────────────────
    public decimal BaseFcp { get; set; }
    public decimal AliquotaFcp { get; set; }
    public decimal ValorFcp { get; set; }
    public decimal BaseFcpSt { get; set; }
    public decimal AliquotaFcpSt { get; set; }
    public decimal ValorFcpSt { get; set; }

    // ── PIS ──────────────────────────────────────────────────────
    public string CstPis { get; set; } = "49";
    public decimal BasePis { get; set; }
    public decimal AliquotaPis { get; set; }
    public decimal ValorPis { get; set; }

    // ── COFINS ───────────────────────────────────────────────────
    public string CstCofins { get; set; } = "49";
    public decimal BaseCofins { get; set; }
    public decimal AliquotaCofins { get; set; }
    public decimal ValorCofins { get; set; }

    // ── IPI ──────────────────────────────────────────────────────
    public string? CstIpi { get; set; }
    public string? EnquadramentoIpi { get; set; }
    public decimal BaseIpi { get; set; }
    public decimal AliquotaIpi { get; set; }
    public decimal ValorIpi { get; set; }

    // ── Total tributos (Lei 12.741) ─────────────────────────────
    public decimal ValorTotalTributos { get; set; }

    // ── Custo calculado na emissão (usado em transferência/NFe) ──
    public decimal? CustoUnitario { get; set; }
}
