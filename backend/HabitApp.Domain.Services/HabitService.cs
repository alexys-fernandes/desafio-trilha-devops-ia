using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Domain.Services.Models;
using HabitApp.Infrastructure.Data.Interfaces;

namespace HabitApp.Domain.Services;

public class HabitService(
    IHabitRepository repository,
    IRecurrenceService recurrenceService,
    IDateService dateService) 
    : BaseService<Habit>(repository), IHabitService
{
    private readonly IHabitRepository _habitRepository = repository;
    private readonly IRecurrenceService _recurrenceService = recurrenceService;
    private readonly IDateService _dateService = dateService;

    public async Task<IEnumerable<Habit>> GetByUserIdAsync(int userId)
        => await _habitRepository.GetByUserIdAsync(userId);

    public override async Task<Habit> AddAsync(Habit entity)
    {
        NormalizeHabitSchedule(entity);

        return await base.AddAsync(entity);
    }

    public override async Task<Habit> UpdateAsync(Habit entity)
    {
        NormalizeHabitSchedule(entity);
        return await base.UpdateAsync(entity);
    }

    public async Task<HabitToggleResult> ToggleCompletionAsync(int habitId)
    {
        var habit = await GetRequiredHabitAsync(habitId);
        var today = _dateService.Today;

        if (!_recurrenceService.IsHabitScheduledForDate(habit, today))
        {
            throw new InvalidOperationException("O hábito não está programado para hoje.");
        }

        var now = _dateService.Now;
        var existingCompletion = await _habitRepository.GetCompletionAsync(habit.Id, habit.UserId, today);
        HabitCompletion? completion = null;

        if (existingCompletion is null)
        {
            completion = await _habitRepository.AddCompletionAsync(new HabitCompletion
            {
                HabitId = habit.Id,
                UserId = habit.UserId,
                CompletedDate = today,
                CompletedAt = now
            });
        }
        else
        {
            await _habitRepository.SoftDeleteCompletionAsync(existingCompletion);
        }

        var updatedHabit = await GetRequiredHabitAsync(habitId);
        var stats = BuildStats(updatedHabit, today);
        var weeklyIndicators = BuildWeeklyIndicators(updatedHabit, today);

        return new HabitToggleResult(
            updatedHabit,
            today,
            completion is not null,
            completion?.CompletedAt,
            stats,
            weeklyIndicators);
    }

    public async Task<HabitDashboard> GetDashboardAsync(int userId)
    {
        var today = _dateService.Today;
        var activeHabits = (await _habitRepository.GetByUserIdWithCompletionsAsync(userId, activeOnly: true))
            .OrderBy(habit => habit.Title)
            .ToList();

        var items = activeHabits
            .Where(habit => _recurrenceService.IsHabitScheduledForDate(habit, today))
            .Select(habit => BuildDashboardItem(habit, today))
            .ToList();

        var dueToday = items.Count(item => item.IsDueToday);
        var completedToday = items.Count(item => item.IsDueToday && item.CompletedToday);
        var completionRate = dueToday == 0 ? 0 : Percentage(completedToday, dueToday);
        var weekStart = GetWeekStart(today);
        var weeklyDueCount = activeHabits.Sum(habit =>
            _recurrenceService.GetExpectedOccurrences(habit, weekStart, today));
        var weeklyCompletedCount = activeHabits.Sum(habit =>
            CountCompletedOccurrences(habit, weekStart, today));

        return new HabitDashboard(
            userId,
            today,
            items,
            new DashboardProgress(
                items.Count,
                dueToday,
                completedToday,
                completionRate),
            weeklyDueCount == 0 ? 0 : Percentage(weeklyCompletedCount, weeklyDueCount),
            activeHabits.Sum(habit => GetCompletionDates(habit).Count));
    }

    public async Task<HabitHistory> GetHistoryAsync(int habitId)
    {
        var today = _dateService.Today;
        var habit = await GetRequiredHabitAsync(habitId);
        var completions = habit.Completions
            .Where(completion => !completion.IsDeleted)
            .OrderByDescending(completion => completion.CompletedDate)
            .ThenByDescending(completion => completion.CompletedAt)
            .ToList();

        return new HabitHistory(
            habit,
            completions,
            BuildStats(habit, today),
            BuildWeeklyIndicators(habit, today));
    }

    public async Task<Habit> UpdateRecurrenceAsync(
        int habitId,
        string recurrenceType,
        string? recurrenceConfig,
        bool? reminderEnabled,
        TimeSpan? reminderTime,
        string? reminderTimezone,
        string? reminderMessage,
        string? reminderType)
    {
        var habit = await GetRequiredHabitAsync(habitId);
        habit.RecurrenceType = _recurrenceService.NormalizeRecurrenceTypeForUpdate(recurrenceType);
        habit.RecurrenceConfig = string.IsNullOrWhiteSpace(recurrenceConfig)
            ? null
            : recurrenceConfig.Trim();
        if (reminderEnabled is not null)
        {
            habit.ReminderEnabled = reminderEnabled.Value;
            habit.ReminderTime = reminderEnabled.Value ? reminderTime : null;
            habit.ReminderTimezone = string.IsNullOrWhiteSpace(reminderTimezone)
                ? "America/Sao_Paulo"
                : reminderTimezone.Trim();
            habit.ReminderMessage = string.IsNullOrWhiteSpace(reminderMessage)
                ? null
                : reminderMessage.Trim();
            habit.ReminderType = NormalizeReminderType(reminderType);
        }

        await _habitRepository.UpdateAsync(habit);
        return await GetRequiredHabitAsync(habitId);
    }

    public async Task<Habit> UpdateArchiveAsync(int habitId, bool isArchived)
    {
        var habit = await GetRequiredHabitAsync(habitId);
        habit.IsArchived = isArchived;

        await _habitRepository.UpdateAsync(habit);
        return await GetRequiredHabitAsync(habitId);
    }

    public async Task<Habit> SyncLegacyCompletedDaysAsync(int habitId, bool[] completedDays)
    {
        if (completedDays.Length != 7)
        {
            throw new ArgumentException("CompletedDays deve conter exatamente sete valores.");
        }

        var habit = await GetRequiredHabitAsync(habitId);
        var weekStart = GetWeekStart(_dateService.Today);

        for (var index = 0; index < completedDays.Length; index++)
        {
            var completedDate = weekStart.AddDays(index);
            var existingCompletion = await _habitRepository.GetCompletionAsync(
                habit.Id,
                habit.UserId,
                completedDate);

            if (completedDays[index] && existingCompletion is null)
            {
                var completedAt = _dateService.Now;
                await _habitRepository.AddCompletionAsync(new HabitCompletion
                {
                    HabitId = habit.Id,
                    UserId = habit.UserId,
                    CompletedDate = completedDate,
                    CompletedAt = completedAt
                });
            }
            else if (!completedDays[index] && existingCompletion is not null)
            {
                await _habitRepository.SoftDeleteCompletionAsync(existingCompletion);
            }
        }

        return await GetRequiredHabitAsync(habitId);
    }

    private async Task<Habit> GetRequiredHabitAsync(int habitId)
    {
        return await _habitRepository.GetByIdWithCompletionsAsync(habitId)
            ?? throw new KeyNotFoundException($"Hábito com ID {habitId} não encontrado.");
    }

    private HabitDashboardItem BuildDashboardItem(Habit habit, DateOnly today)
    {
        var completionDates = GetCompletionDates(habit);
        var stats = BuildStats(habit, today);
        var weeklyIndicators = BuildWeeklyIndicators(habit, today);

        return new HabitDashboardItem(
            habit,
            _recurrenceService.IsHabitScheduledForDate(habit, today),
            completionDates.Contains(today),
            stats,
            weeklyIndicators);
    }

    private HabitStats BuildStats(Habit habit, DateOnly today)
    {
        var completionDates = GetCompletionDates(habit);
        var weekStart = GetWeekStart(today);
        var weeklyDueCount = _recurrenceService.GetExpectedOccurrences(habit, weekStart, today);
        var weeklyCompletedCount = CountCompletedOccurrences(habit, weekStart, today);

        return new HabitStats(
            CalculateCurrentStreak(habit, completionDates, today),
            CalculateLongestStreak(habit, completionDates, today),
            weeklyDueCount == 0 ? 0 : Percentage(weeklyCompletedCount, weeklyDueCount),
            completionDates.Count);
    }

    private IReadOnlyCollection<WeeklyIndicator> BuildWeeklyIndicators(Habit habit, DateOnly today)
    {
        var completionDates = GetCompletionDates(habit);
        var weekStart = GetWeekStart(today);

        return Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = weekStart.AddDays(offset);
                return new WeeklyIndicator(
                    date,
                    date.DayOfWeek.ToString(),
                    _recurrenceService.IsHabitScheduledForDate(habit, date),
                    completionDates.Contains(date));
            })
            .ToList();
    }

    private int CalculateCurrentStreak(Habit habit, IReadOnlySet<DateOnly> completionDates, DateOnly today)
    {
        var createdDate = GetCreatedDate(habit);
        var streak = 0;

        for (var date = today; date >= createdDate; date = date.AddDays(-1))
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
        var createdDate = GetCreatedDate(habit);
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

    private int CountCompletedOccurrences(Habit habit, DateOnly startDate, DateOnly endDate)
        => GetCompletionDates(habit).Count(date =>
            date >= startDate
            && date <= endDate
            && _recurrenceService.IsHabitScheduledForDate(habit, date));

    private static HashSet<DateOnly> GetCompletionDates(Habit habit)
        => habit.Completions
            .Where(completion => !completion.IsDeleted)
            .Select(completion => completion.CompletedDate)
            .ToHashSet();

    private static DateOnly GetCreatedDate(Habit habit)
        => DateOnly.FromDateTime(habit.CreatedAt == default
            ? DateTime.Today
            : habit.CreatedAt);

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Sunday + 7) % 7;
        return date.AddDays(-offset);
    }

    private static int Percentage(int value, int total)
        => (int)Math.Round(value * 100m / total, MidpointRounding.AwayFromZero);

    private void NormalizeHabitSchedule(Habit habit)
    {
        habit.RecurrenceType = _recurrenceService.NormalizeRecurrenceTypeForUpdate(
            string.IsNullOrWhiteSpace(habit.RecurrenceType) ? "Daily" : habit.RecurrenceType);
        habit.RecurrenceConfig = string.IsNullOrWhiteSpace(habit.RecurrenceConfig)
            ? null
            : habit.RecurrenceConfig.Trim();
        habit.ReminderTime = habit.ReminderEnabled ? habit.ReminderTime : null;
        habit.ReminderTimezone = string.IsNullOrWhiteSpace(habit.ReminderTimezone)
            ? "America/Sao_Paulo"
            : habit.ReminderTimezone.Trim();
        habit.ReminderMessage = string.IsNullOrWhiteSpace(habit.ReminderMessage)
            ? null
            : habit.ReminderMessage.Trim();
        habit.ReminderType = NormalizeReminderType(habit.ReminderType);
    }

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
}
