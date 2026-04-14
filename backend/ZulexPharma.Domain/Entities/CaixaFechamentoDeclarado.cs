namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Valores declarados pelo operador no fechamento do caixa, por forma de pagamento.
/// Usado no modo "Conferência Simples" (onde o operador precisa declarar o que tem).
/// No modo "Confirmação de Posse" este registro não é criado — os canhotos já provam os valores.
/// </summary>
public class CaixaFechamentoDeclarado : BaseEntity
{
    public long CaixaId { get; set; }
    public Caixa Caixa { get; set; } = null!;

    public long TipoPagamentoId { get; set; }
    public TipoPagamento TipoPagamento { get; set; } = null!;

    public decimal ValorDeclarado { get; set; }
}
