using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Domain.Services.Models;
using HabitApp.Infrastructure.Data.Interfaces;

namespace HabitApp.Domain.Services;

public class ReminderService(
    IHabitRepository habitRepository,
    INotificationPreferenceRepository preferenceRepository,
    IRecurrenceService recurrenceService,
    IAnalyticsService analyticsService,
    IDateService dateService) : IReminderService
{
    private readonly IHabitRepository _habitRepository = habitRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository = preferenceRepository;
    private readonly IRecurrenceService _recurrenceService = recurrenceService;
    private readonly IAnalyticsService _analyticsService = analyticsService;
    private readonly IDateService _dateService = dateService;

    public async Task<UserNotificationPreference> GetPreferencesAsync(int userId)
    {
        var existingPreference = await _preferenceRepository.GetByUserIdAsync(userId);

        if (existingPreference is not null)
        {
            return existingPreference;
        }

        try
        {
            return await _preferenceRepository.AddAsync(new UserNotificationPreference
            {
                UserId = userId
            });
        }
        catch
        {
            var preferenceCreatedByConcurrentRequest = await _preferenceRepository.GetByUserIdAsync(userId);

            if (preferenceCreatedByConcurrentRequest is not null)
            {
                return preferenceCreatedByConcurrentRequest;
            }

            throw;
        }
    }

    public async Task<UserNotificationPreference> UpdatePreferencesAsync(UserNotificationPreference preferences)
    {
        var existingPreference = await _preferenceRepository.GetByUserIdAsync(preferences.UserId);

        preferences.DefaultReminderType = NormalizeReminderType(preferences.DefaultReminderType);

        if (existingPreference is null)
        {
            return await _preferenceRepository.AddAsync(preferences);
        }

        existingPreference.NotificationsEnabled = preferences.NotificationsEnabled;
        existingPreference.QuietHoursStart = preferences.QuietHoursStart;
        existingPreference.QuietHoursEnd = preferences.QuietHoursEnd;
        existingPreference.ReminderSoundEnabled = preferences.ReminderSoundEnabled;
        existingPreference.MotivationalNotificationsEnabled = preferences.MotivationalNotificationsEnabled;
        existingPreference.AchievementNotificationsEnabled = preferences.AchievementNotificationsEnabled;
        existingPreference.StreakRiskNotificationsEnabled = preferences.StreakRiskNotificationsEnabled;
        existingPreference.DefaultReminderTime = preferences.DefaultReminderTime;
        existingPreference.DefaultReminderType = preferences.DefaultReminderType;

        return await _preferenceRepository.UpdateAsync(existingPreference);
    }

    public async Task<ReminderDashboard> GetDashboardAsync(int userId)
    {
        var options = new ReminderGenerationOptions();
        var preferences = await GetPreferencesAsync(userId);
        var upcomingReminders = await GetUpcomingRemindersAsync(userId, options);
        var missedHabits = await GetMissedHabitsAsync(userId);
        var habitsAtRisk = await GetHabitsAtRiskAsync(userId, options);
        var smartMotivations = await BuildSmartMotivationsAsync(userId, habitsAtRisk);
        var payloads = await GenerateNotificationPayloadsAsync(userId, options);

        return new ReminderDashboard(
            userId,
            _dateService.Today,
            _dateService.Now,
            preferences,
            upcomingReminders.FirstOrDefault(reminder => !reminder.IsSuppressedByQuietHours),
            upcomingReminders,
            missedHabits,
            habitsAtRisk,
            smartMotivations,
            payloads);
    }

    public async Task<IReadOnlyCollection<HabitReminderCandidate>> GetUpcomingRemindersAsync(
        int userId,
        ReminderGenerationOptions? options = null)
    {
        var resolvedOptions = options ?? new ReminderGenerationOptions();
        var preferences = await GetPreferencesAsync(userId);
        var now = _dateService.Now;
        var today = _dateService.Today;
        var activeHabits = await GetActiveHabitsAsync(userId);
        var reminders = new List<HabitReminderCandidate>();

        foreach (var habit in activeHabits)
        {
            if (!habit.ReminderEnabled || habit.ReminderTime is null)
            {
                continue;
            }

            var completionDates = GetCompletionDates(habit);

            for (var offset = 0; offset <= resolvedOptions.SearchDays; offset++)
            {
                var scheduledDate = today.AddDays(offset);

                if (!_recurrenceService.IsHabitScheduledForDate(habit, scheduledDate))
                {
                    continue;
                }

                var isCompleted = completionDates.Contains(scheduledDate);
                var scheduledAt = scheduledDate.ToDateTime(TimeOnly.FromTimeSpan(habit.ReminderTime.Value));

                if (scheduledDate == today && isCompleted)
                {
                    continue;
                }

                if (scheduledAt < now)
                {
                    continue;
                }

                reminders.Add(new HabitReminderCandidate(
                    habit.Id,
                    habit.Title,
                    habit.Icon,
                    habit.Color,
                    habit.Category,
                    scheduledDate,
                    habit.ReminderTime.Value,
                    scheduledAt,
                    GetReminderTimezone(habit),
                    NormalizeReminderType(habit.ReminderType),
                    BuildStandardReminderMessage(habit),
                    isCompleted,
                    IsInQuietHours(habit.ReminderTime.Value, preferences)));
                break;
            }
        }

        return reminders
            .OrderBy(reminder => reminder.ScheduledAt)
            .ThenBy(reminder => reminder.Title)
            .ToList();
    }

    public async Task<IReadOnlyCollection<MissedHabitReminder>> GetMissedHabitsAsync(int userId)
    {
        var today = _dateService.Today;
        var now = _dateService.Now.TimeOfDay;
        var activeHabits = await GetActiveHabitsAsync(userId);

        return activeHabits
            .Where(habit => _recurrenceService.IsHabitScheduledForDate(habit, today))
            .Where(habit => !GetCompletionDates(habit).Contains(today))
            .Where(habit => habit.ReminderTime is not null && habit.ReminderTime <= now)
            .Select(habit =>
            {
                var completionDates = GetCompletionDates(habit);
                var streak = CalculateLiveStreak(habit, completionDates, today);

                return new MissedHabitReminder(
                    habit.Id,
                    habit.Title,
                    habit.Icon,
                    habit.Color,
                    habit.Category,
                    habit.ReminderTime,
                    streak,
                    streak > 0
                        ? $"Conclua {habit.Title} hoje para proteger sua sequência de {streak} dias."
                        : $"{habit.Title} ainda está aberto hoje.");
            })
            .OrderByDescending(habit => habit.CurrentStreak)
            .ThenBy(habit => habit.Title)
            .ToList();
    }

    public async Task<IReadOnlyCollection<HabitStreakRiskReminder>> GetHabitsAtRiskAsync(
        int userId,
        ReminderGenerationOptions? options = null)
    {
        var resolvedOptions = options ?? new ReminderGenerationOptions();
        var today = _dateService.Today;
        var activeHabits = await GetActiveHabitsAsync(userId);
        var risks = new List<HabitStreakRiskReminder>();

        foreach (var habit in activeHabits)
        {
            var completionDates = GetCompletionDates(habit);
            var scheduledToday = _recurrenceService.IsHabitScheduledForDate(habit, today);
            var completedToday = completionDates.Contains(today);
            var liveStreak = CalculateLiveStreak(habit, completionDates, today);
            var longestStreak = CalculateLongestStreak(habit, completionDates, today);
            var daysUntilRecord = GetDaysUntilPersonalRecord(liveStreak, longestStreak);
            var isCloseToRecord = daysUntilRecord is not null
                && daysUntilRecord <= resolvedOptions.PersonalRecordWarningDays;
            var hasMeaningfulStreak = liveStreak >= resolvedOptions.StreakRiskThreshold;

            if (!scheduledToday || completedToday)
            {
                continue;
            }

            if (!hasMeaningfulStreak && !isCloseToRecord)
            {
                risks.Add(BuildOpenHabitRisk(habit, liveStreak, longestStreak, daysUntilRecord));
                continue;
            }

            risks.Add(BuildStreakRisk(habit, liveStreak, longestStreak, daysUntilRecord, isCloseToRecord));
        }

        return risks
            .OrderByDescending(risk => RiskWeight(risk.RiskLevel))
            .ThenBy(risk => risk.DaysUntilPersonalRecord ?? int.MaxValue)
            .ThenByDescending(risk => risk.CurrentStreak)
            .ThenBy(risk => risk.Title)
            .ToList();
    }

    public async Task<IReadOnlyCollection<NotificationPayload>> GenerateNotificationPayloadsAsync(
        int userId,
        ReminderGenerationOptions? options = null)
    {
        var resolvedOptions = options ?? new ReminderGenerationOptions();
        var preferences = await GetPreferencesAsync(userId);

        if (!preferences.NotificationsEnabled)
        {
            return [];
        }

        var mode = string.IsNullOrWhiteSpace(resolvedOptions.Mode)
            ? "Smart"
            : resolvedOptions.Mode.Trim();
        var now = _dateService.Now;

        if (IsInQuietHours(now.TimeOfDay, preferences))
        {
            return [];
        }

        var upcomingReminders = (await GetUpcomingRemindersAsync(userId, resolvedOptions))
            .Where(reminder => !reminder.IsSuppressedByQuietHours)
            .Where(reminder => reminder.ScheduledAt <= now.AddHours(resolvedOptions.PayloadWindowHours))
            .ToList();
        var remainingToday = await GetRemainingTodayAsync(userId);
        var payloads = new List<NotificationPayload>();

        if (string.Equals(mode, "Smart", StringComparison.OrdinalIgnoreCase))
        {
            var riskPayloads = await BuildRiskPayloadsAsync(userId, resolvedOptions, preferences);
            payloads.AddRange(riskPayloads);

            if (remainingToday.Count > 1)
            {
                payloads.Add(BuildGroupedPayload(remainingToday, now, preferences));
            }

            if (payloads.Count == 0)
            {
                payloads.AddRange(upcomingReminders.Select(reminder => ToPayload(reminder)));
            }

            if (payloads.Count == 0 && preferences.MotivationalNotificationsEnabled)
            {
                payloads.AddRange((await BuildSmartMotivationsAsync(userId, []))
                    .Take(1)
                    .Select(message => BuildMotivationPayload(userId, message, now, preferences)));
            }

            return payloads
                .GroupBy(payload => payload.Id)
                .Select(group => group.First())
                .ToList();
        }

        if (string.Equals(mode, "Grouped", StringComparison.OrdinalIgnoreCase)
            && remainingToday.Count > 1)
        {
            return [BuildGroupedPayload(remainingToday, now, preferences)];
        }

        return upcomingReminders
            .Select(reminder => ToPayload(reminder))
            .ToList();
    }

    private async Task<List<NotificationPayload>> BuildRiskPayloadsAsync(
        int userId,
        ReminderGenerationOptions options,
        UserNotificationPreference preferences)
    {
        if (!preferences.StreakRiskNotificationsEnabled)
        {
            return [];
        }

        var risks = await GetHabitsAtRiskAsync(userId, options);

        return risks
            .Where(risk => risk.CurrentStreak >= options.StreakRiskThreshold
                || risk.DaysUntilPersonalRecord is not null)
            .Select(risk => new NotificationPayload(
                $"streak-risk-{risk.HabitId}-{_dateService.Today:yyyyMMdd}",
                "StreakRisk",
                risk.Title,
                risk.Message,
                risk.RiskLevel == "high" ? "high" : "normal",
                _dateService.Now,
                "America/Sao_Paulo",
                risk.HabitId,
                "streak-risk",
                BuildMetadata(("sound", preferences.ReminderSoundEnabled.ToString()))))
            .ToList();
    }

    private async Task<IReadOnlyCollection<Habit>> GetRemainingTodayAsync(int userId)
    {
        var today = _dateService.Today;
        var activeHabits = await GetActiveHabitsAsync(userId);

        return activeHabits
            .Where(habit => _recurrenceService.IsHabitScheduledForDate(habit, today))
            .Where(habit => !GetCompletionDates(habit).Contains(today))
            .OrderBy(habit => habit.ReminderTime ?? TimeSpan.MaxValue)
            .ThenBy(habit => habit.Title)
            .ToList();
    }

    private async Task<IReadOnlyCollection<string>> BuildSmartMotivationsAsync(
        int userId,
        IReadOnlyCollection<HabitStreakRiskReminder> risks)
    {
        var messages = new List<string>();
        var overview = await _analyticsService.GetOverviewAsync(userId);
        var trends = await _analyticsService.GetTrendsAsync(userId);

        if (overview.BestDayOfWeek is not null)
        {
            messages.Add($"Você tem mais sucesso em {PluralizeWeekday(overview.BestDayOfWeek.DayOfWeek)}.");
        }

        if (trends.Last7Days.ScheduledHabits > 0)
        {
            messages.Add($"Você concluiu {trends.Last7Days.CompletionRate}% dos seus hábitos na semana passada.");
        }

        var closestRecord = risks
            .Where(risk => risk.DaysUntilPersonalRecord is not null)
            .OrderBy(risk => risk.DaysUntilPersonalRecord)
            .FirstOrDefault();

        if (closestRecord is not null)
        {
            var dayLabel = closestRecord.DaysUntilPersonalRecord == 1 ? "dia" : "dias";
            messages.Add($"Você está a {closestRecord.DaysUntilPersonalRecord} {dayLabel} da sua maior sequência.");
        }
        else if (overview.LongestOverallStreak > overview.CurrentOverallStreak
            && overview.CurrentOverallStreak > 0)
        {
            var daysAway = overview.LongestOverallStreak - overview.CurrentOverallStreak;
            var dayLabel = daysAway == 1 ? "dia" : "dias";
            messages.Add($"Você está a {daysAway} {dayLabel} da sua maior sequência.");
        }

        if (overview.BestHabit is not null)
        {
            messages.Add($"{overview.BestHabit.Title} é seu hábito mais forte no momento.");
        }

        return messages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .Take(4)
            .ToList();
    }

    private NotificationPayload BuildGroupedPayload(
        IReadOnlyCollection<Habit> remainingToday,
        DateTime now,
        UserNotificationPreference preferences)
    {
        var count = remainingToday.Count;
        var title = count == 1 ? "1 hábito restante" : $"{count} hábitos restantes";

        return new NotificationPayload(
            $"grouped-reminder-{_dateService.Today:yyyyMMdd}",
            "GroupedReminder",
            title,
            count == 1 ? "Você tem 1 hábito restante hoje." : $"Você tem {count} hábitos restantes hoje.",
            "normal",
            now,
            "America/Sao_Paulo",
            null,
            "daily-reminders",
            BuildMetadata(
                ("habitCount", count.ToString()),
                ("sound", preferences.ReminderSoundEnabled.ToString())));
    }

    private static NotificationPayload BuildMotivationPayload(
        int userId,
        string message,
        DateTime now,
        UserNotificationPreference preferences)
        => new(
            $"motivation-{userId}-{now:yyyyMMddHHmm}",
            "Motivation",
            "Mantenha seu ritmo",
            message,
            "low",
            now,
            "America/Sao_Paulo",
            null,
            "motivation",
            BuildMetadata(("sound", preferences.ReminderSoundEnabled.ToString())));

    private static NotificationPayload ToPayload(HabitReminderCandidate reminder)
        => new(
            $"habit-reminder-{reminder.HabitId}-{reminder.ScheduledDate:yyyyMMdd}",
            reminder.ReminderType,
            reminder.Title,
            reminder.Message,
            reminder.ReminderType == "StreakProtection" ? "high" : "normal",
            reminder.ScheduledAt,
            reminder.Timezone,
            reminder.HabitId,
            "habit-reminders",
            BuildMetadata(
                ("scheduledDate", reminder.ScheduledDate.ToString("yyyy-MM-dd")),
                ("reminderTime", reminder.ReminderTime.ToString())));

    private HabitStreakRiskReminder BuildOpenHabitRisk(
        Habit habit,
        int liveStreak,
        int longestStreak,
        int? daysUntilRecord)
        => new(
            habit.Id,
            habit.Title,
            habit.Icon,
            habit.Color,
            habit.Category,
            liveStreak,
            longestStreak,
            daysUntilRecord,
            "medium",
            $"{habit.Title} está programado para hoje e ainda está aberto.");

    private HabitStreakRiskReminder BuildStreakRisk(
        Habit habit,
        int liveStreak,
        int longestStreak,
        int? daysUntilRecord,
        bool isCloseToRecord)
    {
        var riskLevel = liveStreak >= 14 || isCloseToRecord ? "high" : "medium";
        var message = isCloseToRecord && daysUntilRecord is not null
            ? BuildRecordMessage(daysUntilRecord.Value)
            : $"Sua sequência de {liveStreak} dias ainda está ativa. Conclua {habit.Title} hoje para mantê-la.";

        return new HabitStreakRiskReminder(
            habit.Id,
            habit.Title,
            habit.Icon,
            habit.Color,
            habit.Category,
            liveStreak,
            longestStreak,
            daysUntilRecord,
            riskLevel,
            message);
    }

    private static string BuildRecordMessage(int daysUntilRecord)
    {
        if (daysUntilRecord <= 1)
        {
            return "Falta apenas 1 dia para seu recorde histórico.";
        }

        return $"Faltam apenas {daysUntilRecord} dias para seu recorde histórico.";
    }

    private string BuildStandardReminderMessage(Habit habit)
    {
        var reminderType = NormalizeReminderType(habit.ReminderType);

        if (reminderType == "Custom" && !string.IsNullOrWhiteSpace(habit.ReminderMessage))
        {
            return habit.ReminderMessage.Trim();
        }

        if (reminderType == "Motivation")
        {
            return $"Uma pequena marcação mantém {habit.Title} em movimento.";
        }

        return $"Hora de {habit.Title}.";
    }

    private async Task<List<Habit>> GetActiveHabitsAsync(int userId)
        => (await _habitRepository.GetByUserIdWithCompletionsAsync(userId, activeOnly: true))
            .OrderBy(habit => habit.Title)
            .ToList();

    private int CalculateLiveStreak(Habit habit, IReadOnlySet<DateOnly> completionDates, DateOnly today)
    {
        var createdDate = GetCreatedDate(habit, today);
        var startDate = today;

        if (_recurrenceService.IsHabitScheduledForDate(habit, today) && !completionDates.Contains(today))
        {
            startDate = today.AddDays(-1);
        }

        var streak = 0;

        for (var date = startDate; date >= createdDate; date = date.AddDays(-1))
        {
            if (!_recurrenceService.IsHabitScheduledForDate(habit, date))
            {
                continue;
            }

            if (!completionDates.Contains(date))
            {
                break;
            }

            streak++;
        }

        return streak;
    }

    private int CalculateLongestStreak(Habit habit, IReadOnlySet<DateOnly> completionDates, DateOnly today)
    {
        var createdDate = GetCreatedDate(habit, today);
        var longestStreak = 0;
        var currentStreak = 0;

        for (var date = createdDate; date <= today; date = date.AddDays(1))
        {
            if (!_recurrenceService.IsHabitScheduledForDate(habit, date))
            {
                continue;
            }

            if (completionDates.Contains(date))
            {
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return longestStreak;
    }

    private static int? GetDaysUntilPersonalRecord(int currentStreak, int longestStreak)
    {
        if (currentStreak <= 0 || longestStreak <= currentStreak)
        {
            return null;
        }

        return longestStreak - currentStreak;
    }

    private static HashSet<DateOnly> GetCompletionDates(Habit habit)
        => habit.Completions
            .Where(completion => !completion.IsDeleted)
            .Select(completion => completion.CompletedDate)
            .ToHashSet();

    private static DateOnly GetCreatedDate(Habit habit, DateOnly fallback)
        => DateOnly.FromDateTime(habit.CreatedAt == default
            ? fallback.ToDateTime(TimeOnly.MinValue)
            : habit.CreatedAt);

    private static bool IsInQuietHours(TimeSpan time, UserNotificationPreference preferences)
    {
        if (preferences.QuietHoursStart is null || preferences.QuietHoursEnd is null)
        {
            return false;
        }

        var start = preferences.QuietHoursStart.Value;
        var end = preferences.QuietHoursEnd.Value;

        if (start == end)
        {
            return false;
        }

        return start < end
            ? time >= start && time < end
            : time >= start || time < end;
    }

    private static string GetReminderTimezone(Habit habit)
        => string.IsNullOrWhiteSpace(habit.ReminderTimezone)
            ? "America/Sao_Paulo"
            : habit.ReminderTimezone.Trim();

    private static string NormalizeReminderType(string? reminderType)
    {
        if (string.IsNullOrWhiteSpace(reminderType))
        {
            return "Standard";
        }

        return reminderType.Trim() switch
        {
            "StreakProtection" => "StreakProtection",
            "Motivation" => "Motivation",
            "Custom" => "Custom",
            _ => "Standard"
        };
    }

    private static int RiskWeight(string riskLevel)
        => string.Equals(riskLevel, "high", StringComparison.OrdinalIgnoreCase) ? 2 : 1;

    private static string PluralizeWeekday(string dayOfWeek)
        => dayOfWeek.Trim().ToLowerInvariant() switch
        {
            "sunday" or "sundays" or "domingo" or "domingos" => "domingos",
            "monday" or "mondays" or "segunda" or "segunda-feira" or "segundas-feiras" => "segundas-feiras",
            "tuesday" or "tuesdays" or "terça" or "terca" or "terça-feira" or "terca-feira" or "terças-feiras" or "tercas-feiras" => "terças-feiras",
            "wednesday" or "wednesdays" or "quarta" or "quarta-feira" or "quartas-feiras" => "quartas-feiras",
            "thursday" or "thursdays" or "quinta" or "quinta-feira" or "quintas-feiras" => "quintas-feiras",
            "friday" or "fridays" or "sexta" or "sexta-feira" or "sextas-feiras" => "sextas-feiras",
            "saturday" or "saturdays" or "sábado" or "sabado" or "sábados" or "sabados" => "sábados",
            _ => dayOfWeek
        };

    private static IReadOnlyDictionary<string, string> BuildMetadata(
        params (string Key, string Value)[] values)
        => values.ToDictionary(value => value.Key, value => value.Value);
}
