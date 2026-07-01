namespace ZulexPharma.Domain.Entities;

/// <summary>Quais eixos um produto-modelo usa (ex.: Camiseta usa Tamanho + Cor).</summary>
public class ProdutoAtributo : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public long AtributoVariacaoId { get; set; }
    public AtributoVariacao AtributoVariacao { get; set; } = null!;

    public int Ordem { get; set; }
}
