using HabitApp.Domain.Entities;

namespace HabitApp.Domain.Services.Models;

public record AnalyticsHabitSummary(
    int HabitId,
    string Title,
    string Icon,
    string Color,
    string Category,
    int CompletionRate,
    int CurrentStreak,
    int LongestStreak,
    int TotalCompletions);

public record AnalyticsWeekdayStat(
    string DayOfWeek,
    int ScheduledHabits,
    int CompletedHabits,
    int CompletionRate);

public record AnalyticsTrendDay(
    DateOnly Date,
    int ScheduledHabits,
    int CompletedHabits,
    int CompletionRate);

public record AnalyticsTrendWindow(
    string Label,
    DateOnly StartDate,
    DateOnly EndDate,
    int ScheduledHabits,
    int CompletedHabits,
    int CompletionRate,
    IReadOnlyCollection<AnalyticsTrendDay> DailyBreakdown);

public record AnalyticsOverview(
    int UserId,
    DateOnly Date,
    int TotalActiveHabits,
    int TotalCompletions,
    int AverageCompletionRate,
    AnalyticsHabitSummary? BestHabit,
    AnalyticsHabitSummary? WeakestHabit,
    AnalyticsWeekdayStat? BestDayOfWeek,
    AnalyticsWeekdayStat? WeakestDayOfWeek,
    int CurrentOverallStreak,
    int LongestOverallStreak,
    IReadOnlyCollection<AnalyticsTrendDay> WeeklyCompletionTrend,
    IReadOnlyCollection<AnalyticsTrendDay> MonthlyCompletionTrend);

public record HabitAnalytics(
    int UserId,
    int HabitId,
    string Title,
    string Icon,
    string Color,
    string Category,
    int CurrentStreak,
    int LongestStreak,
    int TotalCompletions,
    int CompletionRate,
    AnalyticsWeekdayStat? BestWeekday,
    AnalyticsWeekdayStat? WeakestWeekday,
    DateOnly? LastCompletedDate,
    IReadOnlyCollection<DateOnly> MissedScheduledDates,
    IReadOnlyCollection<AnalyticsTrendDay> WeeklyTrend,
    IReadOnlyCollection<AnalyticsTrendDay> MonthlyTrend);

public record CalendarAnalyticsDay(
    DateOnly Date,
    int ScheduledCount,
    int CompletedCount,
    int CompletionRate,
    string Status);

public record CalendarAnalytics(
    int UserId,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyCollection<CalendarAnalyticsDay> Days);

public record TrendAnalytics(
    int UserId,
    DateOnly Date,
    AnalyticsTrendWindow Last7Days,
    AnalyticsTrendWindow Last30Days,
    AnalyticsTrendWindow Last90Days);

internal record HabitAnalyticsMetric(
    Habit Habit,
    int ScheduledCount,
    int CompletedScheduledCount,
    int CompletionRate,
    int CurrentStreak,
    int LongestStreak,
    int TotalCompletions,
    DateOnly? LastCompletedDate);
