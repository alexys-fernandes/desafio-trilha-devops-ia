using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Infrastructure.Data.Interfaces;
using BCryptNet = BCrypt.Net.BCrypt;

namespace HabitApp.Domain.Services;

public class UserService(IUserRepository repository) : BaseService<User>(repository), IUserService
{
    private readonly new IUserRepository _repository = repository;

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _repository.GetByEmailAsync(email);

        if (user == null || !BCryptNet.Verify(password, user.Password))
            return null;

        return user;
    }

    public override async Task<User> AddAsync(User user)
    {
        if (!string.IsNullOrEmpty(user.Password))
        {
            user.Password = BCryptNet.HashPassword(user.Password);
        }

        return await base.AddAsync(user);
    }
}