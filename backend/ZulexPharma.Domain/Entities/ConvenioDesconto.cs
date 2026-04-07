using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class ConvenioDesconto
{
    public long Id { get; set; }
    public long ConvenioId { get; set; }
    public Convenio Convenio { get; set; } = null!;
    public TipoAgrupador TipoAgrupador { get; set; }
    public long AgrupadorId { get; set; }
    public string AgrupadorNome { get; set; } = string.Empty;
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaxSemSenha { get; set; }
    public decimal DescontoMaxComSenha { get; set; }
}
