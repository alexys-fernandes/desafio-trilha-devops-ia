namespace HabitApp.Application.Dtos;

public record LoginRequestDto(string Email, string Password);
public record UserResponseDto(int Id, string Name, string Email);
