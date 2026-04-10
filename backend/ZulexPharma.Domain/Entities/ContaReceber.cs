using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class ContaReceber : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }

    // ── Origem ──────────────────────────────────────────────────
    public long? VendaId { get; set; }
    public Venda? Venda { get; set; }
    public long? VendaPagamentoId { get; set; }
    public VendaPagamento? VendaPagamento { get; set; }

    // ── Partes ──────────────────────────────────────────────────
    public long? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public long? PessoaId { get; set; }
    public Pessoa? Pessoa { get; set; }

    // ── Classificação ───────────────────────────────────────────
    public long? TipoPagamentoId { get; set; }
    public TipoPagamento? TipoPagamento { get; set; }
    public long? PlanoContaId { get; set; }
    public PlanoConta? PlanoConta { get; set; }

    // ── Descrição ───────────────────────────────────────────────
    public string Descricao { get; set; } = string.Empty;

    // ── Valores ─────────────────────────────────────────────────
    public decimal Valor { get; set; }
    public decimal ValorLiquido { get; set; }
    public decimal Tarifa { get; set; }
    public decimal ValorTarifa { get; set; }
    public decimal ValorRecebido { get; set; }
    public decimal ValorJuros { get; set; }
    public decimal ValorDesconto { get; set; }

    // ── Datas ────────────────────────────────────────────────────
    public DateTime DataEmissao { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataRecebimento { get; set; }

    // ── Parcelas ─────────────────────────────────────────────────
    public int NumParcela { get; set; } = 1;
    public int TotalParcelas { get; set; } = 1;

    // ── Status ──────────────────────────────────────────────────
    public StatusContaReceber Status { get; set; } = StatusContaReceber.Aberta;

    // ── Conta Bancária ──────────────────────────────────────────
    public long? ContaBancariaId { get; set; }
    public ContaBancaria? ContaBancaria { get; set; }

    // ── Cartão (quando tipo = cartão) ───────────────────────────
    public long? AdquirenteBandeiraId { get; set; }
    public AdquirenteBandeira? AdquirenteBandeira { get; set; }
    public long? AdquirenteTarifaId { get; set; }
    public AdquirenteTarifa? AdquirenteTarifa { get; set; }
    public string? Modalidade { get; set; }
    public string? NSU { get; set; }

    // ── PIX ─────────────────────────────────────────────────────
    public string? TxId { get; set; }

    // ── Voucher ─────────────────────────────────────────────────
    public long? VoucherId { get; set; }
    public Voucher? Voucher { get; set; }

    public string? Observacao { get; set; }
}
