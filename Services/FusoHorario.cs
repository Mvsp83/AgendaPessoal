namespace AgendaPessoal.Services;

/// <summary>
/// Hora do Brasil independente do fuso do servidor (hospedagens gratuitas rodam em UTC).
/// Todas as datas do sistema são gravadas e comparadas neste fuso.
/// </summary>
public static class FusoHorario
{
    private static readonly TimeZoneInfo Fuso = Obter();

    private static TimeZoneInfo Obter()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.Local;
    }

    public static DateTime Agora() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Fuso);
}
