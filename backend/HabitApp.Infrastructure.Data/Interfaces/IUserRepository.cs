using HabitApp.Domain.Entities;

namespace HabitApp.Infrastructure.Data.Interfaces;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
