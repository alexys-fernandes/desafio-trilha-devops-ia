using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Domain.Services.Models;

namespace HabitApp.Application;

public class AnalyticsApplicationService(IAnalyticsService analyticsService) : IAnalyticsApplicationService
{
    private readonly IAnalyticsService _analyticsService = analyticsService;

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(int userId)
        => ToOverviewDto(await _analyticsService.GetOverviewAsync(userId));

    public async Task<HabitAnalyticsDto> GetHabitAnalyticsAsync(int userId, int habitId)
        => ToHabitAnalyticsDto(await _analyticsService.GetHabitAnalyticsAsync(userId, habitId));

    public async Task<CalendarAnalyticsDto> GetCalendarAsync(int userId)
        => ToCalendarAnalyticsDto(await _analyticsService.GetCalendarAsync(userId));

    public async Task<TrendAnalyticsDto> GetTrendsAsync(int userId)
        => ToTrendAnalyticsDto(await _analyticsService.GetTrendsAsync(userId));

    private static AnalyticsOverviewDto ToOverviewDto(AnalyticsOverview overview)
        => new()
        {
            UserId = overview.UserId,
            Date = overview.Date,
            TotalActiveHabits = overview.TotalActiveHabits,
            TotalCompletions = overview.TotalCompletions,
            AverageCompletionRate = overview.AverageCompletionRate,
            BestHabit = overview.BestHabit is null ? null : ToHabitSummaryDto(overview.BestHabit),
            WeakestHabit = overview.WeakestHabit is null ? null : ToHabitSummaryDto(overview.WeakestHabit),
            BestDayOfWeek = overview.BestDayOfWeek is null ? null : ToWeekdayStatDto(overview.BestDayOfWeek),
            WeakestDayOfWeek = overview.WeakestDayOfWeek is null ? null : ToWeekdayStatDto(overview.WeakestDayOfWeek),
            CurrentOverallStreak = overview.CurrentOverallStreak,
            LongestOverallStreak = overview.LongestOverallStreak,
            WeeklyCompletionTrend = overview.WeeklyCompletionTrend.Select(ToTrendDayDto),
            MonthlyCompletionTrend = overview.MonthlyCompletionTrend.Select(ToTrendDayDto)
        };

    private static HabitAnalyticsDto ToHabitAnalyticsDto(HabitAnalytics habit)
        => new()
        {
            UserId = habit.UserId,
            HabitId = habit.HabitId,
            Title = habit.Title,
            Icon = habit.Icon,
            Color = habit.Color,
            Category = habit.Category,
            CurrentStreak = habit.CurrentStreak,
            LongestStreak = habit.LongestStreak,
            TotalCompletions = habit.TotalCompletions,
            CompletionRate = habit.CompletionRate,
            BestWeekday = habit.BestWeekday is null ? null : ToWeekdayStatDto(habit.BestWeekday),
            WeakestWeekday = habit.WeakestWeekday is null ? null : ToWeekdayStatDto(habit.WeakestWeekday),
            LastCompletedDate = habit.LastCompletedDate,
            MissedScheduledDates = habit.MissedScheduledDates,
            WeeklyTrend = habit.WeeklyTrend.Select(ToTrendDayDto),
            MonthlyTrend = habit.MonthlyTrend.Select(ToTrendDayDto)
        };

    private static CalendarAnalyticsDto ToCalendarAnalyticsDto(CalendarAnalytics calendar)
        => new()
        {
            UserId = calendar.UserId,
            StartDate = calendar.StartDate,
            EndDate = calendar.EndDate,
            Days = calendar.Days.Select(day => new CalendarAnalyticsDayDto
            {
                Date = day.Date,
                ScheduledCount = day.ScheduledCount,
                CompletedCount = day.CompletedCount,
                CompletionRate = day.CompletionRate,
                Status = day.Status
            })
        };

    private static TrendAnalyticsDto ToTrendAnalyticsDto(TrendAnalytics trends)
        => new()
        {
            UserId = trends.UserId,
            Date = trends.Date,
            Last7Days = ToTrendWindowDto(trends.Last7Days),
            Last30Days = ToTrendWindowDto(trends.Last30Days),
            Last90Days = ToTrendWindowDto(trends.Last90Days)
        };

    private static AnalyticsHabitSummaryDto ToHabitSummaryDto(AnalyticsHabitSummary habit)
        => new()
        {
            HabitId = habit.HabitId,
            Title = habit.Title,
            Icon = habit.Icon,
            Color = habit.Color,
            Category = habit.Category,
            CompletionRate = habit.CompletionRate,
            CurrentStreak = habit.CurrentStreak,
            LongestStreak = habit.LongestStreak,
            TotalCompletions = habit.TotalCompletions
        };

    private static AnalyticsWeekdayStatDto ToWeekdayStatDto(AnalyticsWeekdayStat stat)
        => new()
        {
            DayOfWeek = stat.DayOfWeek,
            ScheduledHabits = stat.ScheduledHabits,
            CompletedHabits = stat.CompletedHabits,
            CompletionRate = stat.CompletionRate
        };

    private static AnalyticsTrendWindowDto ToTrendWindowDto(AnalyticsTrendWindow window)
        => new()
        {
            Label = window.Label,
            StartDate = window.StartDate,
            EndDate = window.EndDate,
            ScheduledHabits = window.ScheduledHabits,
            CompletedHabits = window.CompletedHabits,
            CompletionRate = window.CompletionRate,
            DailyBreakdown = window.DailyBreakdown.Select(ToTrendDayDto)
        };

    private static AnalyticsTrendDayDto ToTrendDayDto(AnalyticsTrendDay day)
        => new()
        {
            Date = day.Date,
            ScheduledHabits = day.ScheduledHabits,
            CompletedHabits = day.CompletedHabits,
            CompletionRate = day.CompletionRate
        };
}
