namespace HabitApp.Application.Dtos;

public class AICoachRequestDto
{
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AICoachResponseDto
{
    public bool Success { get; set; }
    public string Provider { get; set; } = "mock";
    public string Response { get; set; } = string.Empty;
    public string? Error { get; set; }
}
