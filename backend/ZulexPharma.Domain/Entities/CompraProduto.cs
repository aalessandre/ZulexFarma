namespace ZulexPharma.Domain.Entities;

/// <summary>Item da nota fiscal de entrada (compra).</summary>
public class CompraProduto : BaseEntity
{
    public long CompraId { get; set; }
    public Compra Compra { get; set; } = null!;

    /// <summary>Produto vinculado. Null = ainda não vinculado na pré-entrada.</summary>
    public long? ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    // ── Dados do XML ────────────────────────────────────────────
    public int NumeroItem { get; set; }
    public string? CodigoProdutoFornecedor { get; set; }
    public string? CodigoBarrasXml { get; set; }
    public string? DescricaoXml { get; set; }
    public string? NcmXml { get; set; }
    public string? CestXml { get; set; }
    public string? CfopXml { get; set; }
    public string? UnidadeXml { get; set; }

    // ── Valores ─────────────────────────────────────────────────
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorOutros { get; set; }
    public decimal ValorItemNota { get; set; }

    // ── Rastreabilidade (lote/validade) ─────────────────────────
    public string? Lote { get; set; }
    public DateTime? DataFabricacao { get; set; }
    public DateTime? DataValidade { get; set; }

    // ── Medicamento ─────────────────────────────────────────────
    public string? CodigoAnvisa { get; set; }
    public decimal? PrecoMaximoConsumidor { get; set; }

    // ── Vinculação ──────────────────────────────────────────────
    public bool Vinculado { get; set; }
    public short Fracao { get; set; } = 1;
    public string? InfoAdicional { get; set; }

    // ── Sugestão de preço (preenchido na precificação, aplicado na finalização) ──
    public decimal? SugestaoVenda { get; set; }
    public decimal? SugestaoMarkup { get; set; }
    public decimal? SugestaoProjecao { get; set; }
    public decimal? SugestaoCustoMedio { get; set; }
    public bool PrecificacaoAplicada { get; set; }

    // ── Navigation ──────────────────────────────────────────────
    public CompraFiscal? Fiscal { get; set; }
}
