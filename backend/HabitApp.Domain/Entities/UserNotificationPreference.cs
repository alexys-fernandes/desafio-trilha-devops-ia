namespace HabitApp.Domain.Entities;

public class UserNotificationPreference : BaseEntity
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

    public User? User { get; set; }
}
