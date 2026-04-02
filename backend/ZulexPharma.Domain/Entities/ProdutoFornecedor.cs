namespace ZulexPharma.Domain.Entities;

public class ProdutoFornecedor : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public long FilialId { get; set; }

    public long FornecedorId { get; set; }
    public Fornecedor Fornecedor { get; set; } = null!;

    /// <summary>Código do produto no fornecedor.</summary>
    public string? CodigoProdutoFornecedor { get; set; }

    /// <summary>Nome do produto no fornecedor.</summary>
    public string? NomeProduto { get; set; }

    public short Fracao { get; set; } = 1;
}
