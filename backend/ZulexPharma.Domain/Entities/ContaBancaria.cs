using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class ContaBancaria : BaseEntity
{
    public string Descricao { get; set; } = string.Empty;
    public TipoConta TipoConta { get; set; }
    public string? Banco { get; set; }
    public string? Agencia { get; set; }
    public string? AgenciaDigito { get; set; }
    public string? NumeroConta { get; set; }
    public string? ContaDigito { get; set; }
    public string? ChavePix { get; set; }
    public decimal SaldoInicial { get; set; }
    public DateTime? DataSaldoInicial { get; set; }
    public long? PlanoContaId { get; set; }
    public PlanoConta? PlanoConta { get; set; }
    public long? FilialId { get; set; }
    public Filial? Filial { get; set; }
    public string? Observacao { get; set; }
}
