namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Vínculo entre um item de venda (produto controlado) e a receita SNGPC que o cobre.
/// Guarda qual lote foi dispensado e a quantidade efetivamente baixada.
/// </summary>
public class VendaReceitaItem : BaseEntity
{
    public long VendaReceitaId { get; set; }
    public VendaReceita VendaReceita { get; set; } = null!;

    public long VendaItemId { get; set; }
    public VendaItem VendaItem { get; set; } = null!;

    public long ProdutoLoteId { get; set; }
    public ProdutoLote ProdutoLote { get; set; } = null!;

    public decimal Quantidade { get; set; }
}
