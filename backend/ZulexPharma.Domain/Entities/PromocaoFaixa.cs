namespace ZulexPharma.Domain.Entities;

public class PromocaoFaixa
{
    public long Id { get; set; }
    public long PromocaoId { get; set; }
    public Promocao Promocao { get; set; } = null!;
    public int Quantidade { get; set; }
    public decimal PercentualDesconto { get; set; }
}
