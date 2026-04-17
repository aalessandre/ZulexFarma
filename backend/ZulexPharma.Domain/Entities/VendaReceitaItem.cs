namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Item de uma receita SNGPC. Guarda o lote dispensado e a quantidade.
/// Quando a receita é vinculada a uma venda, <c>VendaItemId</c> aponta pro item correspondente.
/// Quando a receita é manual (sem venda), <c>VendaItemId</c> é null e o produto vem direto via <c>ProdutoId</c>.
/// Em ambos os casos, <c>ProdutoId</c> é sempre preenchido para facilitar consultas.
/// </summary>
public class VendaReceitaItem : BaseEntity
{
    public long VendaReceitaId { get; set; }
    public VendaReceita VendaReceita { get; set; } = null!;

    /// <summary>Item da venda (null quando a receita é manual/solta).</summary>
    public long? VendaItemId { get; set; }
    public VendaItem? VendaItem { get; set; }

    /// <summary>Produto — sempre preenchido, mesmo quando há VendaItem (denormalizado).</summary>
    public long ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public long ProdutoLoteId { get; set; }
    public ProdutoLote ProdutoLote { get; set; } = null!;

    public decimal Quantidade { get; set; }
}
