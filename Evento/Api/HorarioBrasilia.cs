public static class HorarioBrasilia
{
    private static readonly TimeZoneInfo Fuso =
        TimeZoneInfo.FindSystemTimeZoneById(
            "America/Sao_Paulo"
        );

    public static DateTimeOffset ParaUtc(DateTime data)
    {
        if (data.Kind != DateTimeKind.Unspecified)
            throw new ArgumentException(
                "Informe a data e hora local de Brasília, sem offset."
            );

        return new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(data, Fuso)
        );
    }

    public static DateTimeOffset ParaLocal(DateTimeOffset data)
    {
        return TimeZoneInfo.ConvertTime(data, Fuso);
    }

    public static DateTimeOffset? ParaLocal(DateTimeOffset? data)
    {
        return data.HasValue
            ? ParaLocal(data.Value)
            : null;
    }
}