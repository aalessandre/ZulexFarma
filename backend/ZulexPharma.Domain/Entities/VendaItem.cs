namespace ZulexPharma.Domain.Entities;

public class VendaItem
{
    public long Id { get; set; }
    public long VendaId { get; set; }
    public Venda Venda { get; set; } = null!;
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public string ProdutoCodigo { get; set; } = string.Empty;
    public string ProdutoNome { get; set; } = string.Empty;
    public string? Fabricante { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal Quantidade { get; set; } = 1;
    public decimal PercentualDesconto { get; set; }
    public decimal PercentualPromocao { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
    public int Ordem { get; set; }

    /// <summary>Vendedor específico do item (quando múltiplos vendedores habilitado).</summary>
    public long? ColaboradorId { get; set; }
    public Colaborador? Colaborador { get; set; }

    // ── Navigation ─────────────────────────────────────────────
    public ICollection<VendaItemDesconto> Descontos { get; set; } = new List<VendaItemDesconto>();
}
