namespace ZulexPharma.Domain.Entities;

public class Adquirente : BaseEntity
{
    public string Nome { get; set; } = string.Empty;

    // ── Navigation ─────────────────────────────────────────────
    public ICollection<AdquirenteBandeira> Bandeiras { get; set; } = new List<AdquirenteBandeira>();
}
