namespace ZulexPharma.Domain.Entities;

public class ProdutoBarras : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public string Barras { get; set; } = string.Empty;
}
