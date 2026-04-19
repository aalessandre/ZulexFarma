using ZulexPharma.Domain.Enums;

namespace ZulexPharma.Domain.Helpers;

/// <summary>
/// Resolve turno fixo (RN-04): Diurno 06:00-17:59:59, Noturno 18:00-05:59:59.
/// </summary>
public static class TurnoHelper
{
    public static TurnoEntrega Resolver(DateTime dataHora)
    {
        var hora = dataHora.Hour;
        return hora >= 6 && hora < 18 ? TurnoEntrega.Diurno : TurnoEntrega.Noturno;
    }

    /// <summary>Retorna 1=Domingo, 2=Segunda, ..., 7=Sábado.</summary>
    public static int DiaSemana(DateTime dataHora) => (int)dataHora.DayOfWeek + 1;
}
