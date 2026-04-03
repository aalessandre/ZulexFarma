namespace ZulexPharma.Domain.Entities;

/// <summary>Cada produto alterado em uma atualização de preços (para reversão).</summary>
public class AtualizacaoPrecoItem : BaseEntity
{
    public long AtualizacaoPrecoId { get; set; }
    public AtualizacaoPreco AtualizacaoPreco { get; set; } = null!;

    public long ProdutoId { get; set; }
    public long ProdutoDadosId { get; set; }
    public string? ProdutoNome { get; set; }

    public decimal ValorVendaAnterior { get; set; }
    public decimal ValorVendaNovo { get; set; }
    public decimal PmcAnterior { get; set; }
    public decimal PmcNovo { get; set; }
    public decimal CustoMedioAnterior { get; set; }
    public decimal MarkupAnterior { get; set; }
    public decimal ProjecaoLucroAnterior { get; set; }
}
