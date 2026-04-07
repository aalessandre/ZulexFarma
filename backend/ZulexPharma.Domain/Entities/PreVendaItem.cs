namespace ZulexPharma.Domain.Entities;

public class PreVendaItem
{
    public long Id { get; set; }
    public long PreVendaId { get; set; }
    public PreVenda PreVenda { get; set; } = null!;
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public string ProdutoCodigo { get; set; } = string.Empty;
    public string ProdutoNome { get; set; } = string.Empty;
    public string? Fabricante { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal Quantidade { get; set; } = 1;
    public decimal PercentualDesconto { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
    public int Ordem { get; set; }
}
