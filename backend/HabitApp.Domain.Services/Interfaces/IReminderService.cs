using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Models;

namespace HabitApp.Domain.Services.Interfaces;

public interface IReminderService
{
    Task<UserNotificationPreference> GetPreferencesAsync(int userId);
    Task<UserNotificationPreference> UpdatePreferencesAsync(UserNotificationPreference preferences);
    Task<ReminderDashboard> GetDashboardAsync(int userId);
    Task<IReadOnlyCollection<HabitReminderCandidate>> GetUpcomingRemindersAsync(
        int userId,
        ReminderGenerationOptions? options = null);
    Task<IReadOnlyCollection<MissedHabitReminder>> GetMissedHabitsAsync(int userId);
    Task<IReadOnlyCollection<HabitStreakRiskReminder>> GetHabitsAtRiskAsync(
        int userId,
        ReminderGenerationOptions? options = null);
    Task<IReadOnlyCollection<NotificationPayload>> GenerateNotificationPayloadsAsync(
        int userId,
        ReminderGenerationOptions? options = null);
}
