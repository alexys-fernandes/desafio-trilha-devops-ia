using HabitApp.Domain.Entities;

namespace HabitApp.Domain.Services.Interfaces;

public interface IUserService : IBaseService<User>
{
    Task<User?> AuthenticateAsync(string email, string password);
}
