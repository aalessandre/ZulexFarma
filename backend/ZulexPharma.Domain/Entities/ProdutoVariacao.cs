namespace ZulexPharma.Domain.Entities;

/// <summary>
/// SKU real de um produto com grade (ex.: Camiseta-M-Preto). Tem código de barras
/// próprio. Estoque/preço ficam em <see cref="ProdutoDados"/> por variação e por filial.
/// </summary>
public class ProdutoVariacao : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public string? CodigoBarras { get; set; }
    /// <summary>Preço próprio da variação (opcional). Null = herda o preço do modelo.</summary>
    public decimal? PrecoProprio { get; set; }

    public ICollection<ProdutoVariacaoValor> Valores { get; set; } = new List<ProdutoVariacaoValor>();
}
