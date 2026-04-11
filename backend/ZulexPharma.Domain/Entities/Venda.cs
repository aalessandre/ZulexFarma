using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class Venda : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }
    public long? CaixaId { get; set; }
    public Caixa? Caixa { get; set; }
    public long? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public long? ColaboradorId { get; set; }
    public Colaborador? Colaborador { get; set; }
    public long? TipoPagamentoId { get; set; }
    public TipoPagamento? TipoPagamento { get; set; }
    public long? ConvenioId { get; set; }

    /// <summary>Número da cesta informado pelo usuário (opcional, configurável).</summary>
    public string? NrCesta { get; set; }

    /// <summary>Origem: PreVenda ou Caixa.</summary>
    public VendaOrigem Origem { get; set; } = VendaOrigem.PreVenda;

    // ── Totais ──────────────────────────────────────────────────
    public decimal TotalBruto { get; set; }
    public decimal TotalDesconto { get; set; }
    public decimal TotalLiquido { get; set; }
    public int TotalItens { get; set; }

    // ── Datas do ciclo de vida ────────────────────────────────
    /// <summary>Data da pré-venda (quando o registro foi criado). Igual a CriadoEm.</summary>
    public DateTime DataPreVenda { get; set; } = Helpers.DataHoraHelper.Agora();

    /// <summary>Data em que a venda foi finalizada no caixa.</summary>
    public DateTime? DataFinalizacao { get; set; }

    /// <summary>Data em que o cupom fiscal (NFC-e) foi emitido.</summary>
    public DateTime? DataEmissaoCupom { get; set; }

    // ── Status ──────────────────────────────────────────────────
    public VendaStatus Status { get; set; } = VendaStatus.Aberta;
    public string? Observacao { get; set; }

    // ── Navigation ─────────────────────────────────────────────
    public ICollection<VendaItem> Itens { get; set; } = new List<VendaItem>();
    public ICollection<VendaPagamento> Pagamentos { get; set; } = new List<VendaPagamento>();
}
