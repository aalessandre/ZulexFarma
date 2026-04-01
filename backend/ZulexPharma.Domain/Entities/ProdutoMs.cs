namespace ZulexPharma.Domain.Entities;

public class ProdutoMs : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public string NumeroMs { get; set; } = string.Empty;
}
