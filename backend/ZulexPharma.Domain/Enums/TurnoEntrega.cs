namespace ZulexPharma.Domain.Enums;

/// <summary>
/// Turnos fixos de entrega. Diurno 06:00–17:59:59; Noturno 18:00–05:59:59.
/// Hardcoded conforme RN-04 da spec de precificação.
/// </summary>
public enum TurnoEntrega
{
    Diurno = 1,
    Noturno = 2
}
