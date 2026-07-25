using HabitApp.Application.Dtos;

namespace HabitApp.Application.Interfaces;

public interface IBaseApplicationService<TEntityDto> where TEntityDto : BaseDto
{
    Task<IEnumerable<TEntityDto>> GetAllAsync();
    Task<TEntityDto?> GetByIdAsync(int id);
    Task<TEntityDto> AddAsync(TEntityDto entityDto);
    Task<TEntityDto> UpdateAsync(TEntityDto entityDto);
    Task<bool> DeleteAsync(int id);
}
