namespace ZulexPharma.Domain.Entities;

public class VendaPagamento
{
    public long Id { get; set; }
    public long VendaId { get; set; }
    public Venda Venda { get; set; } = null!;
    public long TipoPagamentoId { get; set; }
    public TipoPagamento TipoPagamento { get; set; } = null!;
    public decimal Valor { get; set; }
    public decimal Troco { get; set; }

    /// <summary>Para onde foi o troco: "Dinheiro", "PIX", etc.</summary>
    public string? TrocoPara { get; set; }
}
