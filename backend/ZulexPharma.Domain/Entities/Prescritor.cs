namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Prescritor (médico/dentista/veterinário) que emite receitas controladas.
/// Reusável via autocomplete no lançamento de receitas SNGPC.
/// </summary>
public class Prescritor : BaseEntity
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>CRM, CRO, CRMV, CRF, etc.</summary>
    public string TipoConselho { get; set; } = "CRM";

    public string NumeroConselho { get; set; } = string.Empty;

    public string Uf { get; set; } = string.Empty;

    public string? Cpf { get; set; }

    public string? Especialidade { get; set; }

    public string? Telefone { get; set; }
}
