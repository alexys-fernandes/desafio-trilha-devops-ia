using HabitApp.Application.Dtos;

namespace HabitApp.Application.Interfaces;

public interface IReminderApplicationService
{
    Task<NotificationPreferenceDto> GetPreferencesAsync(int userId);
    Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        int userId,
        NotificationPreferenceDto preferences);
    Task<ReminderDashboardDto> GetDashboardAsync(int userId);
    Task<IEnumerable<NotificationPayloadDto>> GeneratePayloadsAsync(int userId, string? mode = null);
}
