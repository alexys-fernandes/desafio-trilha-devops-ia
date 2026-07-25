using HabitApp.Domain.Services.Models;

namespace HabitApp.Domain.Services.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsOverview> GetOverviewAsync(int userId);
    Task<HabitAnalytics> GetHabitAnalyticsAsync(int userId, int habitId);
    Task<CalendarAnalytics> GetCalendarAsync(int userId);
    Task<TrendAnalytics> GetTrendsAsync(int userId);
}
