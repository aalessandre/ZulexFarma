namespace ZulexPharma.Application.DTOs.SelfCheckout;

/// <summary>
/// Produto consultado no ERP origem (Inovafarma) para o Self-Checkout.
/// Carrega dados básicos + preço final calculado conforme RN-19 da spec.
/// Dados fiscais detalhados (CST, alíquotas) só são carregados na finalização da venda (Fatia 5).
/// </summary>
public class ProdutoSelfCheckoutDto
{
    /// <summary>Código do produto no ERP origem (CodigoProduto do Inovafarma).</summary>
    public string CodigoExterno { get; set; } = string.Empty;

    /// <summary>EAN principal do produto.</summary>
    public string? CodigoBarras { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>NCM do produto (8 dígitos).</summary>
    public string? Ncm { get; set; }

    public string? Unidade { get; set; }

    /// <summary>Preço de venda cheio (sem promoção).</summary>
    public decimal PrecoCheio { get; set; }

    /// <summary>Preço final aplicando RN-19 (menor entre cheio e promoções vigentes).</summary>
    public decimal PrecoFinal { get; set; }

    /// <summary>True quando PrecoFinal &lt; PrecoCheio.</summary>
    public bool EmPromocao => PrecoFinal < PrecoCheio;

    /// <summary>Saldo de estoque informativo. Não bloqueia venda no MVP.</summary>
    public decimal? EstoqueAtual { get; set; }
}
