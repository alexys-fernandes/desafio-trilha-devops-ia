namespace HabitApp.Infrastructure.Data.Utils;

public static class DateTimeUtils
{
    public static DateTime GetHorarioBrasilia()
    {
        var utcNow = DateTime.UtcNow;
        try
        {
            var brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, brasiliaTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                var brasiliaTimeZoneLinux = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, brasiliaTimeZoneLinux);
            }
            catch
            {
                return utcNow.AddHours(-3);
            }
        }
    }
}