using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Movimento unificado do caixa: abertura, fechamento, venda, sangria, suprimento, recebimento, pagamento.
/// Cada registro representa uma única operação. Os pagamentos de uma venda geram múltiplos registros (um por VendaPagamento).
/// </summary>
public class CaixaMovimento : BaseEntity
{
    public long CaixaId { get; set; }
    public Caixa Caixa { get; set; } = null!;

    public TipoMovimentoCaixa Tipo { get; set; }
    public DateTime DataMovimento { get; set; }
    public decimal Valor { get; set; }

    /// <summary>Tipo de pagamento (modalidade) quando aplicável: Dinheiro, Cartão, PIX, A Prazo.</summary>
    public long? TipoPagamentoId { get; set; }
    public TipoPagamento? TipoPagamento { get; set; }

    public string Descricao { get; set; } = string.Empty;
    public string? Observacao { get; set; }

    // ── Vínculos de origem ──────────────────────────────────────
    /// <summary>Se o movimento veio de uma venda no caixa.</summary>
    public long? VendaPagamentoId { get; set; }
    public VendaPagamento? VendaPagamento { get; set; }

    /// <summary>Para movimentos de Recebimento: qual conta a receber foi baixada.</summary>
    public long? ContaReceberId { get; set; }
    public ContaReceber? ContaReceber { get; set; }

    /// <summary>Para movimentos de Pagamento: qual conta a pagar foi baixada.</summary>
    public long? ContaPagarId { get; set; }
    public ContaPagar? ContaPagar { get; set; }

    /// <summary>Usuário que registrou o movimento.</summary>
    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    // ── Conferência ─────────────────────────────────────────────
    public StatusConferenciaMovimento StatusConferencia { get; set; } = StatusConferenciaMovimento.Pendente;
    public DateTime? DataConferencia { get; set; }
    public long? ConferidoPorUsuarioId { get; set; }

    // ── Dupla confirmação de sangria ────────────────────────────
    /// <summary>Supervisor que recebeu o dinheiro da sangria (segunda confirmação).</summary>
    public long? ConferenteUsuarioId { get; set; }
    public DateTime? DataConferenteSangria { get; set; }
}
