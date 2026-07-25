using HabitApp.Domain.Entities;
using HabitApp.Infrastructure.Data.Context;
using HabitApp.Infrastructure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HabitApp.Infrastructure.Data.Repositories;

public class HabitRepository(SqliteContext context) 
    : BaseRepository<Habit>(context), IHabitRepository
{
    public override async Task<IEnumerable<Habit>> GetAllAsync()
        => await _context.Habits
            .Include(habit => habit.Completions)
            .ToListAsync();

    public override async Task<Habit?> GetByIdAsync(int id)
        => await GetByIdWithCompletionsAsync(id);

    public async Task<IEnumerable<Habit>> GetByUserIdAsync(int userId)
        => await _context.Habits
            .Include(habit => habit.Completions)
            .Where(habit => habit.UserId == userId)
            .ToListAsync();

    public async Task<IEnumerable<Habit>> GetByUserIdWithCompletionsAsync(int userId, bool activeOnly = false)
    {
        var query = _context.Habits
            .Include(habit => habit.Completions)
            .Where(habit => habit.UserId == userId);

        if (activeOnly)
        {
            query = query.Where(habit => !habit.IsArchived);
        }

        return await query.ToListAsync();
    }

    public async Task<Habit?> GetByIdWithCompletionsAsync(int id)
        => await _context.Habits
            .Include(habit => habit.Completions)
            .FirstOrDefaultAsync(habit => habit.Id == id);

    public async Task<HabitCompletion?> GetCompletionAsync(int habitId, int userId, DateOnly completedDate)
        => await _context.HabitCompletions
            .FirstOrDefaultAsync(completion =>
                completion.HabitId == habitId
                && completion.UserId == userId
                && completion.CompletedDate == completedDate);

    public async Task<HabitCompletion> AddCompletionAsync(HabitCompletion completion)
    {
        await _context.HabitCompletions.AddAsync(completion);
        await _context.SaveChangesAsync();
        return completion;
    }

    public async Task SoftDeleteCompletionAsync(HabitCompletion completion)
    {
        completion.IsDeleted = true;
        await _context.SaveChangesAsync();
    }
}
