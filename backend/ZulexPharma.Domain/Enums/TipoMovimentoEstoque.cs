namespace ZulexPharma.Domain.Enums;

/// <summary>
/// Tipo/origem de um movimento no ledger universal de estoque (MovimentoEstoque).
/// O SENTIDO (entrada/saida) vem do SINAL de Quantidade (delta), nao do tipo.
/// </summary>
public enum TipoMovimentoEstoque
{
    /// <summary>Entrada por finalizacao de compra.</summary>
    Compra = 1,
    /// <summary>Saida por exclusao de compra finalizada (estorno).</summary>
    EstornoCompra = 2,
    /// <summary>Saida por venda.</summary>
    Venda = 3,
    /// <summary>Transferencia entre filiais (saida na origem).</summary>
    Transferencia = 4,
    /// <summary>Saida por perda/quebra.</summary>
    Perda = 5,
    /// <summary>Entrada por estorno de perda.</summary>
    EstornoPerda = 6,
    /// <summary>Ajuste manual de estoque no cadastro do produto.</summary>
    Ajuste = 7,
    /// <summary>Ajuste de estoque pela edicao da grade (SKU).</summary>
    Grade = 8
}
