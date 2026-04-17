namespace ZulexPharma.Domain.Enums;

/// <summary>
/// Modo de contabilização de pontos/cashback.
/// <c>PorVenda</c>: cada venda é avaliada isolada. Se o valor não bate o <c>ValorBase</c>, não gera crédito.
/// <c>Geral</c>: considera todo o histórico elegível do cliente na campanha (soma acumulada).
/// </summary>
public enum ModoContagemFidelidade
{
    PorVenda = 1,
    Geral = 2
}
