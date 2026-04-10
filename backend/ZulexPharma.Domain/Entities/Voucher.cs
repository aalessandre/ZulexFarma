using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class Voucher : BaseEntity
{
    public long ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public long? VendaOrigemId { get; set; }
    public Venda? VendaOrigem { get; set; }

    public decimal Valor { get; set; }
    public decimal ValorUtilizado { get; set; }

    public DateTime DataEmissao { get; set; }
    public DateTime? DataValidade { get; set; }

    public StatusVoucher Status { get; set; } = StatusVoucher.Ativo;
    public string? Observacao { get; set; }
}
