using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Models;

namespace HabitApp.Domain.Services.Interfaces;

public interface IHabitService : IBaseService<Habit>
{
    Task<IEnumerable<Habit>> GetByUserIdAsync(int userId);
    Task<HabitToggleResult> ToggleCompletionAsync(int habitId);
    Task<HabitDashboard> GetDashboardAsync(int userId);
    Task<HabitHistory> GetHistoryAsync(int habitId);
    Task<Habit> UpdateRecurrenceAsync(
        int habitId,
        string recurrenceType,
        string? recurrenceConfig,
        bool? reminderEnabled,
        TimeSpan? reminderTime,
        string? reminderTimezone,
        string? reminderMessage,
        string? reminderType);
    Task<Habit> UpdateArchiveAsync(int habitId, bool isArchived);
    Task<Habit> SyncLegacyCompletedDaysAsync(int habitId, bool[] completedDays);
}
