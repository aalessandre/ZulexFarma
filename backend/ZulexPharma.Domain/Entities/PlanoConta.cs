using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class PlanoConta : BaseEntity
{
    public string Descricao { get; set; } = string.Empty;
    public NivelConta Nivel { get; set; }
    public NaturezaConta Natureza { get; set; }
    public long? ContaPaiId { get; set; }
    public PlanoConta? ContaPai { get; set; }
    public int Ordem { get; set; } = 1;
    public bool VisivelRelatorio { get; set; } = true;
}
