using HabitApp.Domain.Entities;
using HabitApp.Infrastructure.Data.Context;
using HabitApp.Infrastructure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HabitApp.Infrastructure.Data.Repositories;

public class UserRepository(SqliteContext context) 
    : BaseRepository<User>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users
                    .Where(u => u.Email == email && !u.IsDeleted)
                    .FirstOrDefaultAsync();
}