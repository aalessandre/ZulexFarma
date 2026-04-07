namespace ZulexPharma.Domain.Entities;

public class ConvenioBloqueio
{
    public long Id { get; set; }
    public long ConvenioId { get; set; }
    public Convenio Convenio { get; set; } = null!;
    public long TipoPagamentoId { get; set; }
    public TipoPagamento TipoPagamento { get; set; } = null!;
}
