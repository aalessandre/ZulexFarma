using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class TipoPagamento : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public ModalidadePagamento Modalidade { get; set; }
    public decimal DescontoMinimo { get; set; }
    public decimal DescontoMaxSemSenha { get; set; }
    public decimal DescontoMaxComSenha { get; set; }
    public bool AceitaPromocao { get; set; } = true;
}
