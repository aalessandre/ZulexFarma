namespace ZulexPharma.Domain.Entities;

public class PromocaoConvenio
{
    public long Id { get; set; }
    public long PromocaoId { get; set; }
    public Promocao Promocao { get; set; } = null!;
    public long ConvenioId { get; set; }
    public Convenio Convenio { get; set; } = null!;
}
