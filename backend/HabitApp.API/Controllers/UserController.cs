using Microsoft.AspNetCore.Mvc;
using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;

namespace HabitApp.API.Controllers;

public class UserController(IUserApplicationService userApplicationService)
    : BaseController<UserDto>(userApplicationService)
{
    private readonly IUserApplicationService _userApplicationService = userApplicationService;

    [HttpPost("login")]
    public async Task<ActionResult<UserResponseDto>> Login([FromBody] LoginRequestDto loginDto)
    {
        if (loginDto == null) return BadRequest();
        var result = await _userApplicationService.AuthenticateAsync(loginDto);
        if (result == null) return Unauthorized("Credenciais inválidas.");
        return Ok(result);
    }
}