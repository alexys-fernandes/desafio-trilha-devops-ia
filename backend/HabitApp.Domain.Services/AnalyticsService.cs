using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Domain.Services.Models;
using HabitApp.Infrastructure.Data.Interfaces;

namespace HabitApp.Domain.Services;

public class AnalyticsService(
    IHabitRepository habitRepository,
    IRecurrenceService recurrenceService,
    IDateService dateService) : IAnalyticsService
{
    private const int CalendarDays = 90;

    private readonly IHabitRepository _habitRepository = habitRepository;
    private readonly IRecurrenceService _recurrenceService = recurrenceService;
    private readonly IDateService _dateService = dateService;

    public async Task<AnalyticsOverview> GetOverviewAsync(int userId)
    {
        var today = _dateService.Today;
        var habits = await GetActiveHabitsAsync(userId);
        var metrics = habits
            .Select(habit => BuildHabitMetric(habit, today))
            .ToList();
        var weekdayStats = BuildWeekdayStats(habits, GetAnalyticsStartDate(habits, today), today);
        var overallDailyBreakdown = BuildDailyBreakdown(
            habits,
            GetAnalyticsStartDate(habits, today),
            today);

        var totalScheduled = metrics.Sum(metric => metric.ScheduledCount);
        var totalCompleted = metrics.Sum(metric => metric.CompletedScheduledCount);

        return new AnalyticsOverview(
            userId,
            today,
            habits.Count,
            metrics.Sum(metric => metric.TotalCompletions),
            totalScheduled == 0 ? 0 : Percentage(totalCompleted, totalScheduled),
            metrics
                .Where(metric => metric.ScheduledCount > 0)
                .OrderByDescending(metric => metric.CompletionRate)
                .ThenByDescending(metric => metric.CompletedScheduledCount)
                .ThenBy(metric => metric.Habit.Title)
                .Select(ToHabitSummary)
                .FirstOrDefault(),
            metrics
                .Where(metric => metric.ScheduledCount > 0)
                .OrderBy(metric => metric.CompletionRate)
                .ThenByDescending(metric => metric.ScheduledCount)
                .ThenBy(metric => metric.Habit.Title)
                .Select(ToHabitSummary)
                .FirstOrDefault(),
            SelectBestWeekday(weekdayStats),
            SelectWeakestWeekday(weekdayStats),
            CalculateCurrentOverallStreak(overallDailyBreakdown),
            CalculateLongestOverallStreak(overallDailyBreakdown),
            BuildDailyBreakdown(habits, today.AddDays(-6), today),
            BuildDailyBreakdown(habits, today.AddDays(-29), today));
    }

    public async Task<HabitAnalytics> GetHabitAnalyticsAsync(int userId, int habitId)
    {
        var today = _dateService.Today;
        var habit = await _habitRepository.GetByIdWithCompletionsAsync(habitId);

        if (habit is null || habit.UserId != userId)
        {
            throw new KeyNotFoundException($"Hábito com ID {habitId} não encontrado para o usuário {userId}.");
        }

        var createdDate = GetCreatedDate(habit, today);
        var metric = BuildHabitMetric(habit, today);
        var completionDates = GetCompletionDates(habit);
        var missedScheduledDates = _recurrenceService
            .GetScheduledDates(habit, createdDate, today)
            .Where(date => !completionDates.Contains(date))
            .ToList();
        var weekdayStats = BuildWeekdayStats([habit], createdDate, today);

        return new HabitAnalytics(
            userId,
            habit.Id,
            habit.Title,
            habit.Icon,
            habit.Color,
            habit.Category,
            metric.CurrentStreak,
            metric.LongestStreak,
            metric.TotalCompletions,
            metric.CompletionRate,
            SelectBestWeekday(weekdayStats),
            SelectWeakestWeekday(weekdayStats),
            metric.LastCompletedDate,
            missedScheduledDates,
            BuildDailyBreakdown([habit], today.AddDays(-6), today),
            BuildDailyBreakdown([habit], today.AddDays(-29), today));
    }

    public async Task<CalendarAnalytics> GetCalendarAsync(int userId)
    {
        var today = _dateService.Today;
        var startDate = today.AddDays(-(CalendarDays - 1));
        var habits = await GetActiveHabitsAsync(userId);
        var days = BuildDailyBreakdown(habits, startDate, today)
            .Select(day => new CalendarAnalyticsDay(
                day.Date,
                day.ScheduledHabits,
                day.CompletedHabits,
                day.CompletionRate,
                GetCalendarStatus(day.ScheduledHabits, day.CompletedHabits)))
            .ToList();

        return new CalendarAnalytics(userId, startDate, today, days);
    }

    public async Task<TrendAnalytics> GetTrendsAsync(int userId)
    {
        var today = _dateService.Today;
        var habits = await GetActiveHabitsAsync(userId);

        return new TrendAnalytics(
            userId,
            today,
            BuildTrendWindow("Últimos 7 dias", habits, today, 7),
            BuildTrendWindow("Últimos 30 dias", habits, today, 30),
            BuildTrendWindow("Últimos 90 dias", habits, today, 90));
    }

    private async Task<List<Habit>> GetActiveHabitsAsync(int userId)
        => (await _habitRepository.GetByUserIdWithCompletionsAsync(userId, activeOnly: true))
            .OrderBy(habit => habit.Title)
            .ToList();

    private HabitAnalyticsMetric BuildHabitMetric(Habit habit, DateOnly today)
    {
        var createdDate = GetCreatedDate(habit, today);
        var completionDates = GetCompletionDates(habit);
        var scheduledDates = _recurrenceService.GetScheduledDates(habit, createdDate, today);
        var completedScheduledCount = scheduledDates.Count(completionDates.Contains);

        return new HabitAnalyticsMetric(
            habit,
            scheduledDates.Count,
            completedScheduledCount,
            scheduledDates.Count == 0 ? 0 : Percentage(completedScheduledCount, scheduledDates.Count),
            CalculateCurrentStreak(habit, completionDates, today),
            CalculateLongestStreak(habit, completionDates, today),
            completionDates.Count,
            completionDates.Count == 0 ? null : completionDates.Max());
    }

    private IReadOnlyCollection<AnalyticsWeekdayStat> BuildWeekdayStats(
        IReadOnlyCollection<Habit> habits,
        DateOnly startDate,
        DateOnly endDate)
    {
        var counters = Enum.GetValues<DayOfWeek>()
            .ToDictionary(day => day, _ => new WeekdayCounter());

        foreach (var habit in habits)
        {
            var habitStartDate = MaxDate(startDate, GetCreatedDate(habit, endDate));
            var completionDates = GetCompletionDates(habit);

            foreach (var date in _recurrenceService.GetScheduledDates(habit, habitStartDate, endDate))
            {
                counters[date.DayOfWeek].ScheduledHabits++;

                if (completionDates.Contains(date))
                {
                    counters[date.DayOfWeek].CompletedHabits++;
                }
            }
        }

        return counters
            .Where(counter => counter.Value.ScheduledHabits > 0)
            .OrderBy(counter => WeekdayOrder(counter.Key))
            .Select(counter => new AnalyticsWeekdayStat(
                counter.Key.ToString(),
                counter.Value.ScheduledHabits,
                counter.Value.CompletedHabits,
                Percentage(counter.Value.CompletedHabits, counter.Value.ScheduledHabits)))
            .ToList();
    }

    private IReadOnlyCollection<AnalyticsTrendDay> BuildDailyBreakdown(
        IReadOnlyCollection<Habit> habits,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (endDate < startDate)
        {
            return [];
        }

        var completionDatesByHabit = habits.ToDictionary(habit => habit.Id, GetCompletionDates);
        var days = new List<AnalyticsTrendDay>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var scheduledHabits = 0;
            var completedHabits = 0;

            foreach (var habit in habits)
            {
                if (!_recurrenceService.IsHabitScheduledForDate(habit, date))
                {
                    continue;
                }

                scheduledHabits++;

                if (completionDatesByHabit[habit.Id].Contains(date))
                {
                    completedHabits++;
                }
            }

            days.Add(new AnalyticsTrendDay(
                date,
                scheduledHabits,
                completedHabits,
                scheduledHabits == 0 ? 0 : Percentage(completedHabits, scheduledHabits)));
        }

        return days;
    }

    private AnalyticsTrendWindow BuildTrendWindow(
        string label,
        IReadOnlyCollection<Habit> habits,
        DateOnly endDate,
        int days)
    {
        var startDate = endDate.AddDays(-(days - 1));
        var dailyBreakdown = BuildDailyBreakdown(habits, startDate, endDate);
        var scheduledHabits = dailyBreakdown.Sum(day => day.ScheduledHabits);
        var completedHabits = dailyBreakdown.Sum(day => day.CompletedHabits);

        return new AnalyticsTrendWindow(
            label,
            startDate,
            endDate,
            scheduledHabits,
            completedHabits,
            scheduledHabits == 0 ? 0 : Percentage(completedHabits, scheduledHabits),
            dailyBreakdown);
    }

    private int CalculateCurrentStreak(Habit habit, IReadOnlySet<DateOnly> completionDates, DateOnly today)
    {
        var createdDate = GetCreatedDate(habit, today);
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

    private static int CalculateCurrentOverallStreak(IReadOnlyCollection<AnalyticsTrendDay> dailyBreakdown)
    {
        var streak = 0;

        foreach (var day in dailyBreakdown.OrderByDescending(day => day.Date))
        {
            if (day.ScheduledHabits == 0)
            {
                continue;
            }

            if (day.CompletedHabits != day.ScheduledHabits)
            {
                break;
            }

            streak++;
        }

        return streak;
    }

    private static int CalculateLongestOverallStreak(IReadOnlyCollection<AnalyticsTrendDay> dailyBreakdown)
    {
        var longestStreak = 0;
        var currentStreak = 0;

        foreach (var day in dailyBreakdown.OrderBy(day => day.Date))
        {
            if (day.ScheduledHabits == 0)
            {
                continue;
            }

            if (day.CompletedHabits == day.ScheduledHabits)
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

    private static AnalyticsHabitSummary ToHabitSummary(HabitAnalyticsMetric metric)
        => new(
            metric.Habit.Id,
            metric.Habit.Title,
            metric.Habit.Icon,
            metric.Habit.Color,
            metric.Habit.Category,
            metric.CompletionRate,
            metric.CurrentStreak,
            metric.LongestStreak,
            metric.TotalCompletions);

    private static AnalyticsWeekdayStat? SelectBestWeekday(IReadOnlyCollection<AnalyticsWeekdayStat> stats)
        => stats
            .OrderByDescending(stat => stat.CompletionRate)
            .ThenByDescending(stat => stat.CompletedHabits)
            .ThenByDescending(stat => stat.ScheduledHabits)
            .ThenBy(stat => WeekdayOrder(stat.DayOfWeek))
            .FirstOrDefault();

    private static AnalyticsWeekdayStat? SelectWeakestWeekday(IReadOnlyCollection<AnalyticsWeekdayStat> stats)
        => stats
            .OrderBy(stat => stat.CompletionRate)
            .ThenByDescending(stat => stat.ScheduledHabits)
            .ThenBy(stat => WeekdayOrder(stat.DayOfWeek))
            .FirstOrDefault();

    private static string GetCalendarStatus(int scheduledCount, int completedCount)
    {
        if (scheduledCount == 0)
        {
            return "none";
        }

        if (completedCount == scheduledCount)
        {
            return "perfect";
        }

        return completedCount == 0 ? "missed" : "partial";
    }

    private static DateOnly GetAnalyticsStartDate(IReadOnlyCollection<Habit> habits, DateOnly today)
    {
        if (habits.Count == 0)
        {
            return today;
        }

        return habits.Min(habit => GetCreatedDate(habit, today));
    }

    private static HashSet<DateOnly> GetCompletionDates(Habit habit)
        => habit.Completions
            .Where(completion => !completion.IsDeleted)
            .Select(completion => completion.CompletedDate)
            .ToHashSet();

    private static DateOnly GetCreatedDate(Habit habit, DateOnly fallbackDate)
        => DateOnly.FromDateTime(habit.CreatedAt == default
            ? fallbackDate.ToDateTime(TimeOnly.MinValue)
            : habit.CreatedAt);

    private static DateOnly MaxDate(DateOnly left, DateOnly right)
        => left > right ? left : right;

    private static int Percentage(int value, int total)
        => total == 0 ? 0 : (int)Math.Round(value * 100m / total, MidpointRounding.AwayFromZero);

    private static int WeekdayOrder(string dayOfWeek)
        => Enum.TryParse<DayOfWeek>(dayOfWeek, out var day)
            ? WeekdayOrder(day)
            : 7;

    private static int WeekdayOrder(DayOfWeek dayOfWeek)
        => dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - 1;

    private sealed class WeekdayCounter
    {
        public int ScheduledHabits { get; set; }
        public int CompletedHabits { get; set; }
    }
}
