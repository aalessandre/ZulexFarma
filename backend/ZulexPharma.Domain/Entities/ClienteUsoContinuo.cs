namespace ZulexPharma.Domain.Entities;

public class ClienteUsoContinuo
{
    public long Id { get; set; }
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
