using HabitApp.Domain.Entities;

namespace HabitApp.Domain.Services.Models;

public record ReminderGenerationOptions(
    string Mode = "Smart",
    int SearchDays = 7,
    int StreakRiskThreshold = 3,
    int PersonalRecordWarningDays = 2,
    int PayloadWindowHours = 24);

public record HabitReminderCandidate(
    int HabitId,
    string Title,
    string Icon,
    string Color,
    string Category,
    DateOnly ScheduledDate,
    TimeSpan ReminderTime,
    DateTime ScheduledAt,
    string Timezone,
    string ReminderType,
    string Message,
    bool IsCompleted,
    bool IsSuppressedByQuietHours);

public record MissedHabitReminder(
    int HabitId,
    string Title,
    string Icon,
    string Color,
    string Category,
    TimeSpan? ReminderTime,
    int CurrentStreak,
    string Message);

public record HabitStreakRiskReminder(
    int HabitId,
    string Title,
    string Icon,
    string Color,
    string Category,
    int CurrentStreak,
    int LongestStreak,
    int? DaysUntilPersonalRecord,
    string RiskLevel,
    string Message);

public record NotificationPayload(
    string Id,
    string NotificationType,
    string Title,
    string Body,
    string Priority,
    DateTime ScheduledFor,
    string Timezone,
    int? HabitId,
    string? GroupKey,
    IReadOnlyDictionary<string, string> Metadata);

public record ReminderDashboard(
    int UserId,
    DateOnly Date,
    DateTime GeneratedAt,
    UserNotificationPreference Preferences,
    HabitReminderCandidate? NextReminder,
    IReadOnlyCollection<HabitReminderCandidate> UpcomingReminders,
    IReadOnlyCollection<MissedHabitReminder> MissedHabits,
    IReadOnlyCollection<HabitStreakRiskReminder> HabitsAtRisk,
    IReadOnlyCollection<string> SmartMotivations,
    IReadOnlyCollection<NotificationPayload> Payloads);
