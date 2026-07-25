using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using System.Text.Json;

namespace HabitApp.Domain.Services;

public class RecurrenceService : IRecurrenceService
{
    public bool IsHabitScheduledForDate(Habit habit, DateOnly date)
    {
        var createdDate = GetCreatedDate(habit);

        if (date < createdDate)
        {
            return false;
        }

        return NormalizeRecurrenceTypeOrDefault(habit.RecurrenceType) switch
        {
            "Daily" => true,
            "SpecificDaysOfWeek" => IsSpecificDayDue(habit.RecurrenceConfig, date),
            "Weekdays" => date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday,
            "Weekends" => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            "EveryXDays" => IsIntervalDue(habit, date),
            "Weekly" => IsWeeklyDue(habit, date),
            "Monthly" => IsMonthlyDue(habit, date),
            "Custom" => IsCustomDue(habit, date),
            _ => true
        };
    }

    public IReadOnlyCollection<DateOnly> GetScheduledDates(Habit habit, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            return [];
        }

        var dates = new List<DateOnly>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (IsHabitScheduledForDate(habit, date))
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    public int GetExpectedOccurrences(Habit habit, DateOnly startDate, DateOnly endDate)
        => GetScheduledDates(habit, startDate, endDate).Count;

    public string NormalizeRecurrenceTypeForUpdate(string recurrenceType)
    {
        if (string.IsNullOrWhiteSpace(recurrenceType))
        {
            throw new ArgumentException("RecurrenceType é obrigatório.");
        }

        return NormalizeRecurrenceType(recurrenceType)
            ?? throw new ArgumentException($"Tipo de recorrência não aceito: {recurrenceType}.");
    }

    private static bool IsSpecificDayDue(string? recurrenceConfig, DateOnly date)
    {
        var days = ReadDaysOfWeek(recurrenceConfig);
        return days.Count > 0 && days.Contains(date.DayOfWeek);
    }

    private static bool IsIntervalDue(Habit habit, DateOnly date)
    {
        var interval = ReadIntConfig(habit.RecurrenceConfig, ["interval", "everyXDays", "days"], 1);
        var daysSinceCreated = date.DayNumber - GetCreatedDate(habit).DayNumber;

        return interval > 0 && daysSinceCreated >= 0 && daysSinceCreated % interval == 0;
    }

    private static bool IsWeeklyDue(Habit habit, DateOnly date)
    {
        var configuredDays = ReadDaysOfWeek(habit.RecurrenceConfig);
        if (configuredDays.Count > 0)
        {
            return configuredDays.Contains(date.DayOfWeek);
        }

        return date.DayOfWeek == GetCreatedDate(habit).DayOfWeek;
    }

    private static bool IsMonthlyDue(Habit habit, DateOnly date)
    {
        var createdDate = GetCreatedDate(habit);
        var configuredDay = ReadIntConfig(habit.RecurrenceConfig, ["dayOfMonth", "day"], createdDate.Day);
        var lastDayOfMonth = DateTime.DaysInMonth(date.Year, date.Month);

        return date.Day == Math.Clamp(configuredDay, 1, lastDayOfMonth);
    }

    private static bool IsCustomDue(Habit habit, DateOnly date)
    {
        var configuredDays = ReadDaysOfWeek(habit.RecurrenceConfig);
        if (configuredDays.Count > 0)
        {
            return configuredDays.Contains(date.DayOfWeek);
        }

        var interval = ReadIntConfig(habit.RecurrenceConfig, ["interval", "everyXDays", "days"], 0);
        if (interval > 0)
        {
            return IsIntervalDue(habit, date);
        }

        return true;
    }

    private static HashSet<DayOfWeek> ReadDaysOfWeek(string? recurrenceConfig)
    {
        var days = new HashSet<DayOfWeek>();

        if (string.IsNullOrWhiteSpace(recurrenceConfig))
        {
            return days;
        }

        try
        {
            using var document = JsonDocument.Parse(recurrenceConfig);
            AddJsonDays(document.RootElement, days);

            if (days.Count > 0)
            {
                return days;
            }
        }
        catch (JsonException)
        {
            // Compact configs like "Monday,Wednesday,Friday" are accepted.
        }

        foreach (var token in recurrenceConfig.Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseDayOfWeek(token, out var day))
            {
                days.Add(day);
            }
        }

        return days;
    }

    private static void AddJsonDays(JsonElement element, ISet<DayOfWeek> days)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                AddJsonDay(item, days);
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            AddJsonDay(element, days);
            return;
        }

        foreach (var propertyName in new[] { "daysOfWeek", "weekDays", "days", "dayOfWeek" })
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                AddJsonDays(property, days);
            }
        }
    }

    private static void AddJsonDay(JsonElement element, ISet<DayOfWeek> days)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
        {
            if (TryParseDayOfWeek(value.ToString(), out var day))
            {
                days.Add(day);
            }
        }
        else if (element.ValueKind == JsonValueKind.String
            && TryParseDayOfWeek(element.GetString(), out var day))
        {
            days.Add(day);
        }
    }

    private static int ReadIntConfig(string? recurrenceConfig, string[] propertyNames, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(recurrenceConfig))
        {
            return defaultValue;
        }

        try
        {
            using var document = JsonDocument.Parse(recurrenceConfig);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Number && root.TryGetInt32(out var number))
            {
                return number;
            }

            if (root.ValueKind == JsonValueKind.String
                && int.TryParse(root.GetString(), out var stringNumber))
            {
                return stringNumber;
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var propertyName in propertyNames)
                {
                    if (!root.TryGetProperty(propertyName, out var property))
                    {
                        continue;
                    }

                    if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var propertyNumber))
                    {
                        return propertyNumber;
                    }

                    if (property.ValueKind == JsonValueKind.String
                        && int.TryParse(property.GetString(), out var propertyStringNumber))
                    {
                        return propertyStringNumber;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Fall through to compact integer parsing.
        }

        return int.TryParse(recurrenceConfig, out var compactNumber)
            ? compactNumber
            : defaultValue;
    }

    private static bool TryParseDayOfWeek(string? value, out DayOfWeek dayOfWeek)
    {
        dayOfWeek = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();

        if (int.TryParse(normalized, out var dayNumber) && dayNumber is >= 0 and <= 6)
        {
            dayOfWeek = (DayOfWeek)dayNumber;
            return true;
        }

        if (Enum.TryParse(normalized, ignoreCase: true, out dayOfWeek))
        {
            return true;
        }

        dayOfWeek = normalized.ToLowerInvariant() switch
        {
            "sun" => DayOfWeek.Sunday,
            "mon" => DayOfWeek.Monday,
            "tue" or "tues" => DayOfWeek.Tuesday,
            "wed" => DayOfWeek.Wednesday,
            "thu" or "thur" or "thurs" => DayOfWeek.Thursday,
            "fri" => DayOfWeek.Friday,
            "sat" => DayOfWeek.Saturday,
            "domingo" or "domingos" => DayOfWeek.Sunday,
            "segunda" or "segunda-feira" or "segundas-feiras" => DayOfWeek.Monday,
            "terça" or "terca" or "terça-feira" or "terca-feira" or "terças-feiras" or "tercas-feiras" => DayOfWeek.Tuesday,
            "quarta" or "quarta-feira" or "quartas-feiras" => DayOfWeek.Wednesday,
            "quinta" or "quinta-feira" or "quintas-feiras" => DayOfWeek.Thursday,
            "sexta" or "sexta-feira" or "sextas-feiras" => DayOfWeek.Friday,
            "sábado" or "sabado" or "sábados" or "sabados" => DayOfWeek.Saturday,
            _ => default
        };

        return normalized.ToLowerInvariant() is "sun" or "mon" or "tue" or "tues" or "wed"
            or "thu" or "thur" or "thurs" or "fri" or "sat"
            or "domingo" or "domingos"
            or "segunda" or "segunda-feira" or "segundas-feiras"
            or "terça" or "terca" or "terça-feira" or "terca-feira" or "terças-feiras" or "tercas-feiras"
            or "quarta" or "quarta-feira" or "quartas-feiras"
            or "quinta" or "quinta-feira" or "quintas-feiras"
            or "sexta" or "sexta-feira" or "sextas-feiras"
            or "sábado" or "sabado" or "sábados" or "sabados";
    }

    private static string NormalizeRecurrenceTypeOrDefault(string? recurrenceType)
        => NormalizeRecurrenceType(recurrenceType) ?? "Daily";

    private static string? NormalizeRecurrenceType(string? recurrenceType)
    {
        if (string.IsNullOrWhiteSpace(recurrenceType))
        {
            return null;
        }

        return recurrenceType.Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty)
            .ToLowerInvariant() switch
            {
                "everyday" or "daily" => "Daily",
                "specificdaysofweek" or "specificdays" => "SpecificDaysOfWeek",
                "weekdays" => "Weekdays",
                "weekends" => "Weekends",
                "everyxdays" or "interval" => "EveryXDays",
                "weekly" => "Weekly",
                "monthly" => "Monthly",
                "custom" => "Custom",
                _ => null
            };
    }

    private static DateOnly GetCreatedDate(Habit habit)
        => DateOnly.FromDateTime(habit.CreatedAt == default ? DateTime.Today : habit.CreatedAt);
}
