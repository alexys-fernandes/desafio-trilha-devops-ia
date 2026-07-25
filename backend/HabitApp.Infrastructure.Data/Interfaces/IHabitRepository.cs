using HabitApp.Domain.Entities;

namespace HabitApp.Infrastructure.Data.Interfaces;

public interface IHabitRepository : IBaseRepository<Habit>
{
    Task<IEnumerable<Habit>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Habit>> GetByUserIdWithCompletionsAsync(int userId, bool activeOnly = false);
    Task<Habit?> GetByIdWithCompletionsAsync(int id);
    Task<HabitCompletion?> GetCompletionAsync(int habitId, int userId, DateOnly completedDate);
    Task<HabitCompletion> AddCompletionAsync(HabitCompletion completion);
    Task SoftDeleteCompletionAsync(HabitCompletion completion);
}
