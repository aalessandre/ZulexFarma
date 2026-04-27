namespace ZulexPharma.Application.DTOs.SelfCheckout;

/// <summary>
/// Natureza de operação cadastrada no ERP origem (ex: Inovafarma.Fiscal_Natureza).
/// Usada para popular o dropdown do accordion Self-Checkout.
/// </summary>
public class NaturezaOperacaoErpDto
{
    public int Codigo { get; set; }
    public string Nome { get; set; } = string.Empty;
}
