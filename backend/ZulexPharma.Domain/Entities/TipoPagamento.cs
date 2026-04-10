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
    public int Ordem { get; set; }
    /// <summary>Se true, é padrão do sistema e não pode ser editado/excluído pelo usuário.</summary>
    public bool PadraoSistema { get; set; }

    /// <summary>Plano de contas associado a este tipo de pagamento.</summary>
    public long? PlanoContaId { get; set; }
    public PlanoConta? PlanoConta { get; set; }
}
