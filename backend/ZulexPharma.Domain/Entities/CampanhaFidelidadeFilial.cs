namespace ZulexPharma.Domain.Entities;

public class CampanhaFidelidadeFilial
{
    public long Id { get; set; }
    public long CampanhaFidelidadeId { get; set; }
    public CampanhaFidelidade CampanhaFidelidade { get; set; } = null!;
    public long FilialId { get; set; }
    public Filial Filial { get; set; } = null!;
}
