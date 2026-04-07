namespace ZulexPharma.Domain.Entities;

public class PromocaoPagamento
{
    public long Id { get; set; }
    public long PromocaoId { get; set; }
    public Promocao Promocao { get; set; } = null!;
    public long TipoPagamentoId { get; set; }
    public TipoPagamento TipoPagamento { get; set; } = null!;
}
