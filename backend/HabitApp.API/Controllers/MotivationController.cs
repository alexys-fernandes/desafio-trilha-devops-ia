using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HabitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MotivationController(IMotivationApplicationService applicationService) : ControllerBase
{
    [HttpGet("user/{userId}/summary")]
    public async Task<ActionResult<MotivationSummaryDto>> GetSummary(int userId)
        => Ok(await applicationService.GetSummaryAsync(userId));

    [HttpGet("user/{userId}/streaks")]
    public async Task<ActionResult<StreakCenterDto>> GetStreakCenter(int userId)
        => Ok(await applicationService.GetStreakCenterAsync(userId));

    [HttpGet("user/{userId}/achievements")]
    public async Task<ActionResult<AchievementSetDto>> GetAchievements(int userId)
        => Ok(await applicationService.GetAchievementsAsync(userId));

    [HttpGet("user/{userId}/monthly-challenges")]
    public async Task<ActionResult<MonthlyChallengeSetDto>> GetMonthlyChallenges(int userId)
        => Ok(await applicationService.GetMonthlyChallengesAsync(userId));
}
