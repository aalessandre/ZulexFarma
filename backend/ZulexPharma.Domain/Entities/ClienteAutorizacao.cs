namespace ZulexPharma.Domain.Entities;

public class ClienteAutorizacao
{
    public long Id { get; set; }
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
}
