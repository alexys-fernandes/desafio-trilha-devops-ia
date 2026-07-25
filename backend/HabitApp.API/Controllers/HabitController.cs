using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HabitApp.API.Controllers;

public class HabitController(IHabitApplicationService applicationService)
    : BaseController<HabitDto>(applicationService)
{
    [HttpGet("user/{userId:int}")]
    public async Task<ActionResult<IEnumerable<HabitDto>>> GetByUserId(int userId)
    {
        var habits = await applicationService.GetByUserIdAsync(userId);
        return Ok(habits);
    }

    [HttpPost("{id}/toggle-completion")]
    public async Task<ActionResult<HabitToggleCompletionDto>> ToggleCompletion(int id)
    {
        try
        {
            var result = await applicationService.ToggleCompletionAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("user/{userId}/dashboard")]
    public async Task<ActionResult<HabitDashboardDto>> GetDashboard(int userId)
    {
        var dashboard = await applicationService.GetDashboardAsync(userId);
        return Ok(dashboard);
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<HabitHistoryDto>> GetHistory(int id)
    {
        try
        {
            var history = await applicationService.GetHistoryAsync(id);
            return Ok(history);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}/recurrence")]
    public async Task<ActionResult<HabitDto>> UpdateRecurrence(
        int id,
        [FromBody] HabitRecurrenceUpdateDto recurrence)
    {
        try
        {
            var habit = await applicationService.UpdateRecurrenceAsync(id, recurrence);
            return Ok(habit);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id}/archive")]
    public async Task<ActionResult<HabitDto>> UpdateArchive(
        int id,
        [FromBody] HabitArchiveUpdateDto? archive)
    {
        try
        {
            var habit = await applicationService.UpdateArchiveAsync(
                id,
                archive ?? new HabitArchiveUpdateDto());
            return Ok(habit);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
