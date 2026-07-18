namespace ZulexPharma.Domain.Entities;

/// <summary>Condições de pagamento bloqueadas para o cliente.</summary>
// FASE 6 (b+c): promovido a BaseEntity (uniao por cliente — ver ClienteConvenio).
public class ClienteBloqueio : BaseEntity
{
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public long TipoPagamentoId { get; set; }
    public TipoPagamento TipoPagamento { get; set; } = null!;
}
