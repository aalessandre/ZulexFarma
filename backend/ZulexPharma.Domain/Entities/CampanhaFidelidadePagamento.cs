namespace ZulexPharma.Domain.Entities;

public class CampanhaFidelidadePagamento
{
    public long Id { get; set; }
    public long CampanhaFidelidadeId { get; set; }
    public CampanhaFidelidade CampanhaFidelidade { get; set; } = null!;
    public long TipoPagamentoId { get; set; }
    public TipoPagamento TipoPagamento { get; set; } = null!;
}
