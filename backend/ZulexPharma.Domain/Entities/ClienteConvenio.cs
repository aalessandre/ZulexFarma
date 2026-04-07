namespace ZulexPharma.Domain.Entities;

public class ClienteConvenio
{
    public long Id { get; set; }
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public long ConvenioId { get; set; }
    public Convenio Convenio { get; set; } = null!;
    public string? Matricula { get; set; }
    public string? Cartao { get; set; }
    public decimal Limite { get; set; }
}
