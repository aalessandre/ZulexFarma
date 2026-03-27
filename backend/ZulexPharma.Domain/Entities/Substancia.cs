namespace ZulexPharma.Domain.Entities;

/// <summary>
/// Substância farmacêutica. Controla DCB, CAS, SNGPC e classe terapêutica.
/// </summary>
public class Substancia : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Dcb { get; set; } = string.Empty;
    public string Cas { get; set; } = string.Empty;
    public bool ControleEspecialSngpc { get; set; } = false;
    public string? ClasseTerapeutica { get; set; }
}
