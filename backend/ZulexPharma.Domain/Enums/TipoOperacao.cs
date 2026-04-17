namespace ZulexPharma.Domain.Enums;

/// <summary>
/// Tipo de operação registrada em Venda. Unifica vendas comerciais e
/// movimentações de estoque (transferência, perda, ajuste, etc.).
/// </summary>
public enum TipoOperacao
{
    Venda = 1,
    Devolucao = 2,
    Transferencia = 3,
    Perda = 4,
    AjusteEntrada = 5,
    AjusteSaida = 6,
    Bonificacao = 7,
    Inventario = 8
}
