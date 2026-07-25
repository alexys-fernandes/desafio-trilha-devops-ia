namespace HabitApp.Application.Dtos;

public class AnalyticsHabitSummaryDto
{
    public int HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CompletionRate { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalCompletions { get; set; }
}

public class AnalyticsWeekdayStatDto
{
    public string DayOfWeek { get; set; } = string.Empty;
    public int ScheduledHabits { get; set; }
    public int CompletedHabits { get; set; }
    public int CompletionRate { get; set; }
}

public class AnalyticsTrendDayDto
{
    public DateOnly Date { get; set; }
    public int ScheduledHabits { get; set; }
    public int CompletedHabits { get; set; }
    public int CompletionRate { get; set; }
}

public class AnalyticsTrendWindowDto
{
    public string Label { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int ScheduledHabits { get; set; }
    public int CompletedHabits { get; set; }
    public int CompletionRate { get; set; }
    public IEnumerable<AnalyticsTrendDayDto> DailyBreakdown { get; set; } = [];
}

public class AnalyticsOverviewDto
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public int TotalActiveHabits { get; set; }
    public int TotalCompletions { get; set; }
    public int AverageCompletionRate { get; set; }
    public AnalyticsHabitSummaryDto? BestHabit { get; set; }
    public AnalyticsHabitSummaryDto? WeakestHabit { get; set; }
    public AnalyticsWeekdayStatDto? BestDayOfWeek { get; set; }
    public AnalyticsWeekdayStatDto? WeakestDayOfWeek { get; set; }
    public int CurrentOverallStreak { get; set; }
    public int LongestOverallStreak { get; set; }
    public IEnumerable<AnalyticsTrendDayDto> WeeklyCompletionTrend { get; set; } = [];
    public IEnumerable<AnalyticsTrendDayDto> MonthlyCompletionTrend { get; set; } = [];
}

public class HabitAnalyticsDto
{
    public int UserId { get; set; }
    public int HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalCompletions { get; set; }
    public int CompletionRate { get; set; }
    public AnalyticsWeekdayStatDto? BestWeekday { get; set; }
    public AnalyticsWeekdayStatDto? WeakestWeekday { get; set; }
    public DateOnly? LastCompletedDate { get; set; }
    public IEnumerable<DateOnly> MissedScheduledDates { get; set; } = [];
    public IEnumerable<AnalyticsTrendDayDto> WeeklyTrend { get; set; } = [];
    public IEnumerable<AnalyticsTrendDayDto> MonthlyTrend { get; set; } = [];
}

public class CalendarAnalyticsDayDto
{
    public DateOnly Date { get; set; }
    public int ScheduledCount { get; set; }
    public int CompletedCount { get; set; }
    public int CompletionRate { get; set; }
    public string Status { get; set; } = "none";
}

public class CalendarAnalyticsDto
{
    public int UserId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public IEnumerable<CalendarAnalyticsDayDto> Days { get; set; } = [];
}

public class TrendAnalyticsDto
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public AnalyticsTrendWindowDto Last7Days { get; set; } = new();
    public AnalyticsTrendWindowDto Last30Days { get; set; } = new();
    public AnalyticsTrendWindowDto Last90Days { get; set; } = new();
}
