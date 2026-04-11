namespace ZulexPharma.Domain.Entities;

/// <summary>Condições de pagamento bloqueadas para o cliente.</summary>
public class ClienteBloqueio
{
    public long Id { get; set; }
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public long TipoPagamentoId { get; set; }
    public TipoPagamento TipoPagamento { get; set; } = null!;
}
