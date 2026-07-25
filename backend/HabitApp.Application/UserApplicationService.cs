using AutoMapper;
using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;

namespace HabitApp.Application;

public class UserApplicationService(IUserService userService, IMapper mapper)
    : BaseApplicationService<User, UserDto>(userService, mapper), IUserApplicationService
{
    private readonly IUserService _userService = userService;

    public async Task<UserResponseDto?> AuthenticateAsync(LoginRequestDto loginDto)
    {
        var user = await _userService.AuthenticateAsync(loginDto.Email, loginDto.Password);
        return _mapper.Map<UserResponseDto>(user);
    }
}