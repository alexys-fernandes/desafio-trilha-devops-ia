using HabitApp.Domain.Entities;

namespace HabitApp.Application.Dtos;

public class HabitDto : BaseDto
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RecurrenceType { get; set; } = "Daily";
    public string? RecurrenceConfig { get; set; }
    public bool ReminderEnabled { get; set; }
    public TimeSpan? ReminderTime { get; set; }
    public string ReminderTimezone { get; set; } = "America/Sao_Paulo";
    public string? ReminderMessage { get; set; }
    public string ReminderType { get; set; } = "Standard";
    public bool IsArchived { get; set; }

    // Compatibility fields for the current Angular UI. New endpoints use dated completions.
    public int Streak { get; set; }
    public bool[]? CompletedDays { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }
}

public class HabitCompletionDto : BaseDto
{
    public int HabitId { get; set; }
    public int UserId { get; set; }
    public DateOnly CompletedDate { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class HabitStatsDto
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int WeeklySuccessRate { get; set; }
    public int TotalCompletions { get; set; }
}

public class HabitWeeklyIndicatorDto
{
    public DateOnly Date { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public bool IsDue { get; set; }
    public bool IsCompleted { get; set; }
}

public class HabitDashboardProgressDto
{
    public int TotalHabits { get; set; }
    public int DueToday { get; set; }
    public int CompletedToday { get; set; }
    public int CompletionRate { get; set; }
}

public class HabitDashboardItemDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RecurrenceType { get; set; } = "Daily";
    public string? RecurrenceConfig { get; set; }
    public bool ReminderEnabled { get; set; }
    public TimeSpan? ReminderTime { get; set; }
    public string ReminderTimezone { get; set; } = "America/Sao_Paulo";
    public string? ReminderMessage { get; set; }
    public string ReminderType { get; set; } = "Standard";
    public bool IsArchived { get; set; }
    public bool IsDueToday { get; set; }
    public bool CompletedToday { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int WeeklySuccessRate { get; set; }
    public int TotalCompletions { get; set; }
    public IEnumerable<HabitWeeklyIndicatorDto> WeeklyIndicators { get; set; } = [];
}

public class HabitDashboardDto
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public IEnumerable<HabitDashboardItemDto> ActiveHabits { get; set; } = [];
    public HabitDashboardProgressDto DailyProgress { get; set; } = new();
    public int WeeklySuccessRate { get; set; }
    public int TotalCompletions { get; set; }
}

public class HabitToggleCompletionDto
{
    public int HabitId { get; set; }
    public DateOnly CompletedDate { get; set; }
    public bool CompletedToday { get; set; }
    public DateTime? CompletedAt { get; set; }
    public HabitDashboardItemDto Habit { get; set; } = new();
}

public class HabitHistoryDto
{
    public int HabitId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string RecurrenceType { get; set; } = "Daily";
    public string? RecurrenceConfig { get; set; }
    public HabitStatsDto Stats { get; set; } = new();
    public IEnumerable<HabitWeeklyIndicatorDto> WeeklyIndicators { get; set; } = [];
    public IEnumerable<HabitCompletionDto> Completions { get; set; } = [];
}

public class HabitRecurrenceUpdateDto
{
    public string RecurrenceType { get; set; } = "Daily";
    public string? RecurrenceConfig { get; set; }
    public TimeSpan? ReminderTime { get; set; }
    public bool? ReminderEnabled { get; set; }
    public string ReminderTimezone { get; set; } = "America/Sao_Paulo";
    public string? ReminderMessage { get; set; }
    public string ReminderType { get; set; } = "Standard";
}

public class HabitArchiveUpdateDto
{
    public bool IsArchived { get; set; } = true;
}
