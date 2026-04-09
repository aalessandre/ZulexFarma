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
}
