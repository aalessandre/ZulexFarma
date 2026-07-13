namespace ZulexPharma.Domain.Enums;

/// <summary>
/// Periodicidade de um lancamento recorrente/parcelado (Contas a Pagar).
/// Semanal/Quinzenal e PersonalizadoDias somam DIAS; as demais somam MESES.
/// </summary>
public enum Periodicidade
{
    Semanal = 1,
    Quinzenal = 2,
    Mensal = 3,
    Trimestral = 4,
    Semestral = 5,
    Anual = 6,
    /// <summary>A cada X dias (X = IntervaloPersonalizado).</summary>
    PersonalizadoDias = 7,
    /// <summary>A cada X meses (X = IntervaloPersonalizado).</summary>
    PersonalizadoMeses = 8
}
