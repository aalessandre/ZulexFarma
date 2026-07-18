namespace ZulexPharma.Domain.Entities;

// FASE 6 (b+c): promovido a BaseEntity (uniao por cliente — ver ClienteConvenio).
public class ClienteUsoContinuo : BaseEntity
{
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public string? Fabricante { get; set; }
    public int Apresentacao { get; set; }
    public int QtdeAoDia { get; set; }
    public DateTime? UltimaCompra { get; set; }
    public DateTime? ProximaCompra { get; set; }
    public string? ColaboradorNome { get; set; }
}
