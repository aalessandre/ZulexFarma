namespace ZulexPharma.Domain.Entities;

// FASE 6 (b+c): promovido a BaseEntity (uniao por cliente — ver ClienteConvenio).
public class ClienteAutorizacao : BaseEntity
{
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
}
