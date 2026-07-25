namespace HabitApp.Domain.Entities;

public class Habit : BaseEntity
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

    public int UserId { get; set; }
    public User? User { get; set; }
    public ICollection<HabitCompletion> Completions { get; set; } = [];
}
