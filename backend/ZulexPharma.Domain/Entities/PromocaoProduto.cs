namespace ZulexPharma.Domain.Entities;

public class PromocaoProduto
{
    public long Id { get; set; }
    public long PromocaoId { get; set; }
    public Promocao Promocao { get; set; } = null!;
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    // ── Preços ──────────────────────────────────────────────────
    public decimal PercentualPromocao { get; set; }
    public decimal ValorPromocao { get; set; }
    public decimal PercentualLucro { get; set; }

    // ── Quantidade limitada (por produto) ───────────────────────
    public int? QtdeLimite { get; set; }
    public int QtdeVendida { get; set; }
    public decimal? PercentualAposLimite { get; set; }
    public decimal? ValorAposLimite { get; set; }
}
