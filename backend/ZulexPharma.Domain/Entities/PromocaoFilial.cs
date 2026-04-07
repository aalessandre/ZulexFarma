namespace ZulexPharma.Domain.Entities;

public class PromocaoFilial
{
    public long Id { get; set; }
    public long PromocaoId { get; set; }
    public Promocao Promocao { get; set; } = null!;
    public long FilialId { get; set; }
    public Filial Filial { get; set; } = null!;
}
