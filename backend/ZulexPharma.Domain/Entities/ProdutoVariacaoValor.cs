namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Liga uma variação (SKU) aos valores de cada eixo
/// (Camiseta-M-Preto = Tamanho:M + Cor:Preto). N linhas por variação.
/// </summary>
public class ProdutoVariacaoValor : BaseEntity
{
    public long ProdutoVariacaoId { get; set; }
    public ProdutoVariacao ProdutoVariacao { get; set; } = null!;

    public long AtributoVariacaoId { get; set; }
    public AtributoVariacao AtributoVariacao { get; set; } = null!;

    public long ValorAtributoId { get; set; }
    public ValorAtributo ValorAtributo { get; set; } = null!;
}
