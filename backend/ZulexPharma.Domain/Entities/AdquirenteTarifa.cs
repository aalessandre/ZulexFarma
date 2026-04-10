using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class AdquirenteTarifa
{
    public long Id { get; set; }
    public long AdquirenteBandeiraId { get; set; }
    public AdquirenteBandeira AdquirenteBandeira { get; set; } = null!;

    public ModalidadeCartao Modalidade { get; set; }

    /// <summary>Tarifa cobrada pela adquirente em %.</summary>
    public decimal Tarifa { get; set; }

    /// <summary>Prazo em dias para recebimento.</summary>
    public int PrazoRecebimento { get; set; }

    /// <summary>Conta bancária onde o valor será depositado.</summary>
    public long? ContaBancariaId { get; set; }
    public ContaBancaria? ContaBancaria { get; set; }
}
