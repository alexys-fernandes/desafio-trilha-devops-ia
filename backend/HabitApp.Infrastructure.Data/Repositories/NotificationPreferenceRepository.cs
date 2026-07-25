using HabitApp.Domain.Entities;
using HabitApp.Infrastructure.Data.Context;
using HabitApp.Infrastructure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HabitApp.Infrastructure.Data.Repositories;

public class NotificationPreferenceRepository(SqliteContext context)
    : BaseRepository<UserNotificationPreference>(context), INotificationPreferenceRepository
{
    public async Task<UserNotificationPreference?> GetByUserIdAsync(int userId)
        => await _context.UserNotificationPreferences
            .FirstOrDefaultAsync(preference => preference.UserId == userId);
}
