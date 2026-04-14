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

    /// <summary>
    /// Lista da Portaria SVS/MS 344/1998 (A1, A2, A3, B1, B2, C1, C2, C3, C4, C5, D1, D2).
    /// Null = não enquadrada em nenhuma lista.
    /// </summary>
    public string? ListaPortaria344 { get; set; }

    /// <summary>
    /// Tipo de receita exigido para dispensação.
    /// 1=Controle Especial 2 vias (Branca), 2=Notificação B (Azul), 3=Notificação Especial (Branca),
    /// 4=Notificação A (Amarela), 5=Antimicrobiano 2 vias.
    /// </summary>
    public int? TipoReceita { get; set; }

    /// <summary>
    /// Validade da receita em dias (ex: 30, 60, 90).
    /// </summary>
    public int? ValidadeReceitaDias { get; set; }

    /// <summary>
    /// Se a receita pode conter adendo (outras substâncias na mesma receita).
    /// </summary>
    public bool Adendo { get; set; } = false;
}
