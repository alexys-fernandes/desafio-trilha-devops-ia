using HabitApp.Application.Dtos;

namespace HabitApp.Application.Interfaces;

public interface IAICoachApplicationService
{
    Task<AICoachResponseDto> SendMessageAsync(AICoachRequestDto request);
    Task<object> CheckHealthAsync();
}
