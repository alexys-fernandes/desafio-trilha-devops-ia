namespace HabitApp.Application.Dtos;

public class UserDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public ICollection<HabitDto> Habits { get; set; } = [];
}
