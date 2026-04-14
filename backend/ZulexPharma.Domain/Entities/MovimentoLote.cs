using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Histórico de todas as entradas, saídas e ajustes de um lote específico.
/// Cada linha é imutável (auditoria) e tem o saldo pós-movimento snapshot para rastrear
/// reconstrução histórica sem precisar somar tudo.
/// </summary>
public class MovimentoLote : BaseEntity
{
    public long ProdutoLoteId { get; set; }
    public ProdutoLote ProdutoLote { get; set; } = null!;

    public TipoMovimentoLote Tipo { get; set; }

    /// <summary>Quantidade movimentada (sempre positiva — o sinal é implícito no Tipo).</summary>
    public decimal Quantidade { get; set; }

    public DateTime DataMovimento { get; set; }

    public long? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    // ── Origem do movimento (uma dessas FK é preenchida conforme o tipo) ──
    public long? CompraId { get; set; }
    public Compra? Compra { get; set; }

    public long? VendaId { get; set; }
    public Venda? Venda { get; set; }

    public long? CompraProdutoLoteId { get; set; }
    public CompraProdutoLote? CompraProdutoLote { get; set; }

    /// <summary>Saldo do lote após este movimento (snapshot para auditoria).</summary>
    public decimal SaldoAposMovimento { get; set; }

    public string? Observacao { get; set; }
}
