using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

public class PreVenda : BaseEntity
{
    public long FilialId { get; set; }
    public Filial? Filial { get; set; }
    public long? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public long? ColaboradorId { get; set; }
    public Colaborador? Colaborador { get; set; }
    public long? TipoPagamentoId { get; set; }
    public TipoPagamento? TipoPagamento { get; set; }
    public long? ConvenioId { get; set; }

    // ── Totais ──────────────────────────────────────────────────
    public decimal TotalBruto { get; set; }
    public decimal TotalDesconto { get; set; }
    public decimal TotalLiquido { get; set; }
    public int TotalItens { get; set; }

    // ── Status ──────────────────────────────────────────────────
    public PreVendaStatus Status { get; set; } = PreVendaStatus.Aberta;
    public string? Observacao { get; set; }

    // ── Navigation ─────────────────────────────────────────────
    public ICollection<PreVendaItem> Itens { get; set; } = new List<PreVendaItem>();
}
