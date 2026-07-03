namespace ZulexPharma.Domain.Helpers;

public static class DataHoraHelper
{
    private static readonly TimeZoneInfo FusoBrasilia =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");

    /// <summary>Retorna a data/hora atual no fuso horário de Brasília.</summary>
    public static DateTime Agora() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasilia);

    /// <summary>Retorna a data atual (sem hora) no fuso horário de Brasília.</summary>
    public static DateTime Hoje() => Agora().Date;

    /// <summary>
    /// Converte um DateTime que representa a hora LOCAL de Brasília num
    /// DateTimeOffset com o offset correto (ex.: -03:00). Necessário pra
    /// serializar data-hora com fuso certo na SEFAZ (dhEmi/dhSaiEnt/dhEvento) —
    /// senão o formato "zzz" usa o offset do servidor (UTC no Railway) e a nota
    /// sai 3h fora ("Data-Hora de emissão atrasada").
    /// </summary>
    public static DateTimeOffset ComOffset(DateTime brasilia)
    {
        var dt = DateTime.SpecifyKind(brasilia, DateTimeKind.Unspecified);
        return new DateTimeOffset(dt, FusoBrasilia.GetUtcOffset(dt));
    }
}
