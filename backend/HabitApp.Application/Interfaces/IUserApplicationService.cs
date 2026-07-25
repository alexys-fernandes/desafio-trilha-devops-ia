using HabitApp.Application.Dtos;

namespace HabitApp.Application.Interfaces;

public interface IUserApplicationService : IBaseApplicationService<UserDto>
{
    Task<UserResponseDto?> AuthenticateAsync(LoginRequestDto loginDto);
}
