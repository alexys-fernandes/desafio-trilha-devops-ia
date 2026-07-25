using HabitApp.Domain.Entities;

namespace HabitApp.Infrastructure.Data.Interfaces;

public interface INotificationPreferenceRepository : IBaseRepository<UserNotificationPreference>
{
    Task<UserNotificationPreference?> GetByUserIdAsync(int userId);
}
