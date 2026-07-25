using AutoMapper;
using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;

namespace HabitApp.Application;

public abstract class BaseApplicationService<TEntity, TEntityDto>(
    IBaseService<TEntity> service,
    IMapper mapper)
    : IBaseApplicationService<TEntityDto>
    where TEntity : BaseEntity
    where TEntityDto : BaseDto
{
    protected readonly IBaseService<TEntity> _service = service;
    protected readonly IMapper _mapper = mapper;

    public virtual async Task<IEnumerable<TEntityDto>> GetAllAsync()
    {
        var entities = await _service.GetAllAsync();
        return _mapper.Map<IEnumerable<TEntityDto>>(entities);
    }

    public virtual async Task<TEntityDto?> GetByIdAsync(int id)
    {
        var entity = await _service.GetByIdAsync(id);
        return _mapper.Map<TEntityDto>(entity);
    }

    public virtual async Task<TEntityDto> AddAsync(TEntityDto entityDto)
    {
        var entity = _mapper.Map<TEntity>(entityDto);
        var result = await _service.AddAsync(entity);
        return _mapper.Map<TEntityDto>(result);
    }

    public virtual async Task<TEntityDto> UpdateAsync(TEntityDto entityDto)
    {
        var entity = _mapper.Map<TEntity>(entityDto);
        var result = await _service.UpdateAsync(entity);
        return _mapper.Map<TEntityDto>(result);
    }

    public virtual async Task<bool> DeleteAsync(int id)
        => await _service.DeleteAsync(id);
}