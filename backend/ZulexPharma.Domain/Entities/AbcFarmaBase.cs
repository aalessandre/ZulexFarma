namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Base de preços ABCFarma. Tabela centralizada no Railway, NÃO replica entre filiais.
/// Populada via upload de arquivo JSON ou futuramente via API ABCFarma.
/// </summary>
public class AbcFarmaBase
{
    public long Id { get; set; }
    public string Ean { get; set; } = string.Empty;
    public string? RegistroAnvisa { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Composicao { get; set; }
    public string? NomeFabricante { get; set; }
    public string? ClasseTerapeutica { get; set; }
    public string? Ncm { get; set; }

    // Preços por faixa de ICMS
    public decimal Pf0 { get; set; }
    public decimal Pmc0 { get; set; }
    public decimal Pf12 { get; set; }
    public decimal Pmc12 { get; set; }
    public decimal Pf17 { get; set; }
    public decimal Pmc17 { get; set; }
    public decimal Pf18 { get; set; }
    public decimal Pmc18 { get; set; }
    public decimal Pf19 { get; set; }
    public decimal Pmc19 { get; set; }
    public decimal Pf195 { get; set; }
    public decimal Pmc195 { get; set; }
    public decimal Pf20 { get; set; }
    public decimal Pmc20 { get; set; }
    public decimal Pf205 { get; set; }
    public decimal Pmc205 { get; set; }
    public decimal Pf21 { get; set; }
    public decimal Pmc21 { get; set; }
    public decimal Pf22 { get; set; }
    public decimal Pmc22 { get; set; }
    public decimal Pf225 { get; set; }
    public decimal Pmc225 { get; set; }
    public decimal Pf23 { get; set; }
    public decimal Pmc23 { get; set; }

    public decimal PercentualIpi { get; set; }
    public DateTime? DataVigencia { get; set; }
    public DateTime AtualizadoEm { get; set; } = Helpers.DataHoraHelper.Agora();
}
