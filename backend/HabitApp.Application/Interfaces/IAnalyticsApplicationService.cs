using HabitApp.Application.Dtos;

namespace HabitApp.Application.Interfaces;

public interface IAnalyticsApplicationService
{
    Task<AnalyticsOverviewDto> GetOverviewAsync(int userId);
    Task<HabitAnalyticsDto> GetHabitAnalyticsAsync(int userId, int habitId);
    Task<CalendarAnalyticsDto> GetCalendarAsync(int userId);
    Task<TrendAnalyticsDto> GetTrendsAsync(int userId);
}
