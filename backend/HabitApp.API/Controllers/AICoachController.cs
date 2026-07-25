using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HabitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AICoachController(IAICoachApplicationService aiCoachApplicationService) : ControllerBase
{
    private readonly IAICoachApplicationService _aiCoachApplicationService = aiCoachApplicationService;

    [HttpGet("health")]
    public async Task<ActionResult<object>> Health()
    {
        var result = await _aiCoachApplicationService.CheckHealthAsync();
        return Ok(result);
    }

    [HttpPost("sendMessage")]
    public async Task<ActionResult<AICoachResponseDto>> SendMessage([FromBody] AICoachRequestDto request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new AICoachResponseDto
            {
                Success = false,
                Provider = "mock",
                Response = "A mensagem é obrigatória.",
                Error = "message is required"
            });
        }

        var result = await _aiCoachApplicationService.SendMessageAsync(request);
        return Ok(result);
    }
}
