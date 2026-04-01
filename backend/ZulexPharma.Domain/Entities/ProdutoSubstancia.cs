namespace ZulexPharma.Domain.Entities;

public class ProdutoSubstancia : BaseEntity
{
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public long SubstanciaId { get; set; }
    public Substancia Substancia { get; set; } = null!;
}
