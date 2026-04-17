namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Regra de CFOP/CST/Benefício por cenário tributário de uma Natureza de Operação.
/// Cada natureza pode ter até 7 regras (uma por cenário).
/// </summary>
public class NaturezaOperacaoRegra : BaseEntity
{
    public long NaturezaOperacaoId { get; set; }
    public NaturezaOperacao NaturezaOperacao { get; set; } = null!;

    /// <summary>
    /// Cenário tributário:
    /// 1 = Tributados para PF ou PJ sem IE
    /// 2 = Tributados para PJ com IE
    /// 3 = Produtos com Substituição Tributária
    /// 4 = Produtos isentos de ICMS
    /// 5 = Produtos não tributados pelo ICMS
    /// 6 = Operações com outros tipos de tributação
    /// 7 = Documento fiscal referenciado em NFe
    /// </summary>
    public int CenarioTributario { get; set; }

    public string? CfopInterno { get; set; }
    public string? CfopInterestadual { get; set; }
    public string? CstIcmsInterno { get; set; }
    public string? CstIcmsInterestadual { get; set; }
    public string? CodigoBeneficioInterno { get; set; }
    public string? CodigoBeneficioInterestadual { get; set; }
}
