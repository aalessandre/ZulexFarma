namespace ZulexPharma.Domain.Enums;

/// <summary>Como o cliente pode trocar os pontos acumulados.</summary>
public enum FormaRetiradaPontos
{
    /// <summary>Troca por prêmio do catálogo <c>PremioFidelidade</c>.</summary>
    Premio = 1,

    /// <summary>Aplica como desconto direto na próxima venda (conversão via <c>ValorPorPonto</c>).</summary>
    DescontoNaVenda = 2
}
