using HabitApp.Application;
using HabitApp.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HabitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AICoachController(AICoachApplicationService aiCoachApplicationService) : ControllerBase
{
    private readonly AICoachApplicationService _aiCoachApplicationService = aiCoachApplicationService;

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
