namespace ZulexPharma.Domain.Entities;

public class AdquirenteBandeira
{
    public long Id { get; set; }
    public long AdquirenteId { get; set; }
    public Adquirente Adquirente { get; set; } = null!;
    public string Bandeira { get; set; } = string.Empty;

    // ── Navigation ─────────────────────────────────────────────
    public ICollection<AdquirenteTarifa> Tarifas { get; set; } = new List<AdquirenteTarifa>();
}
