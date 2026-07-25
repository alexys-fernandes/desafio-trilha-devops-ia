using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HabitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReminderController(IReminderApplicationService applicationService) : ControllerBase
{
    [HttpGet("user/{userId}/preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> GetPreferences(int userId)
        => Ok(await applicationService.GetPreferencesAsync(userId));

    [HttpPut("user/{userId}/preferences")]
    public async Task<ActionResult<NotificationPreferenceDto>> UpdatePreferences(
        int userId,
        [FromBody] NotificationPreferenceDto preferences)
        => Ok(await applicationService.UpdatePreferencesAsync(userId, preferences));

    [HttpGet("user/{userId}/dashboard")]
    public async Task<ActionResult<ReminderDashboardDto>> GetDashboard(int userId)
        => Ok(await applicationService.GetDashboardAsync(userId));

    [HttpGet("user/{userId}/payloads")]
    public async Task<ActionResult<IEnumerable<NotificationPayloadDto>>> GeneratePayloads(
        int userId,
        [FromQuery] string? mode)
        => Ok(await applicationService.GeneratePayloadsAsync(userId, mode));
}
