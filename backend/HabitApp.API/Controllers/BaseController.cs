using Microsoft.AspNetCore.Mvc;
using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;

namespace HabitApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController<TEntityDto>(IBaseApplicationService<TEntityDto> applicationService) 
    : ControllerBase
    where TEntityDto : BaseDto
{
    protected readonly IBaseApplicationService<TEntityDto> _applicationService = applicationService;

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<TEntityDto>>> Get()
    {
        var entities = await _applicationService.GetAllAsync();
        return Ok(entities);
    }

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<TEntityDto>> Get(int id)
    {
        var entity = await _applicationService.GetByIdAsync(id);
        if (entity == null) return NotFound();
        return Ok(entity);
    }

    [HttpPost]
    public virtual async Task<ActionResult<TEntityDto>> Post([FromBody] TEntityDto entityDto)
    {
        if (entityDto == null) return BadRequest();
        var result = await _applicationService.AddAsync(entityDto);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public virtual async Task<ActionResult<TEntityDto>> Put(int id, [FromBody] TEntityDto entityDto)
    {
        if (entityDto == null || id != entityDto.Id) return BadRequest();
        var result = await _applicationService.UpdateAsync(entityDto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public virtual async Task<ActionResult> Delete(int id)
    {
        var deleted = await _applicationService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}