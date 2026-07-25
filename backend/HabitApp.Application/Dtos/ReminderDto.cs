namespace HabitApp.Application.Dtos;

public class NotificationPreferenceDto : BaseDto
{
    public int UserId { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public TimeSpan? QuietHoursStart { get; set; } = TimeSpan.FromHours(22);
    public TimeSpan? QuietHoursEnd { get; set; } = TimeSpan.FromHours(7);
    public bool ReminderSoundEnabled { get; set; } = true;
    public bool MotivationalNotificationsEnabled { get; set; } = true;
    public bool AchievementNotificationsEnabled { get; set; } = true;
    public bool StreakRiskNotificationsEnabled { get; set; } = true;
    public TimeSpan? DefaultReminderTime { get; set; } = TimeSpan.FromHours(9);
    public string DefaultReminderType { get; set; } = "Standard";
}

public class HabitReminderCandidateDto
{
    public int HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateOnly ScheduledDate { get; set; }
    public TimeSpan ReminderTime { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Timezone { get; set; } = "America/Sao_Paulo";
    public string ReminderType { get; set; } = "Standard";
    public string Message { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public bool IsSuppressedByQuietHours { get; set; }
}

public class MissedHabitReminderDto
{
    public int HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TimeSpan? ReminderTime { get; set; }
    public int CurrentStreak { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class HabitStreakRiskReminderDto
{
    public int HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int? DaysUntilPersonalRecord { get; set; }
    public string RiskLevel { get; set; } = "medium";
    public string Message { get; set; } = string.Empty;
}

public class NotificationPayloadDto
{
    public string Id { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal";
    public DateTime ScheduledFor { get; set; }
    public string Timezone { get; set; } = "America/Sao_Paulo";
    public int? HabitId { get; set; }
    public string? GroupKey { get; set; }
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

public class ReminderDashboardDto
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime GeneratedAt { get; set; }
    public NotificationPreferenceDto Preferences { get; set; } = new();
    public HabitReminderCandidateDto? NextReminder { get; set; }
    public IEnumerable<HabitReminderCandidateDto> UpcomingReminders { get; set; } = [];
    public IEnumerable<MissedHabitReminderDto> MissedHabits { get; set; } = [];
    public IEnumerable<HabitStreakRiskReminderDto> HabitsAtRisk { get; set; } = [];
    public IEnumerable<string> SmartMotivations { get; set; } = [];
    public IEnumerable<NotificationPayloadDto> Payloads { get; set; } = [];
}
