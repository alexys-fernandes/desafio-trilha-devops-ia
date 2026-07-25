using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HabitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(IAnalyticsApplicationService applicationService) : ControllerBase
{
    [HttpGet("user/{userId}/overview")]
    public async Task<ActionResult<AnalyticsOverviewDto>> GetOverview(int userId)
        => Ok(await applicationService.GetOverviewAsync(userId));

    [HttpGet("user/{userId}/habit/{habitId}")]
    public async Task<ActionResult<HabitAnalyticsDto>> GetHabitAnalytics(int userId, int habitId)
    {
        try
        {
            return Ok(await applicationService.GetHabitAnalyticsAsync(userId, habitId));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("user/{userId}/calendar")]
    public async Task<ActionResult<CalendarAnalyticsDto>> GetCalendar(int userId)
        => Ok(await applicationService.GetCalendarAsync(userId));

    [HttpGet("user/{userId}/trends")]
    public async Task<ActionResult<TrendAnalyticsDto>> GetTrends(int userId)
        => Ok(await applicationService.GetTrendsAsync(userId));
}
