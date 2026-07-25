using HabitApp.Application.Dtos;

namespace HabitApp.Application.Interfaces;

public interface IHabitApplicationService : IBaseApplicationService<HabitDto>
{
    Task<IEnumerable<HabitDto>> GetByUserIdAsync(int userId);
    Task<HabitToggleCompletionDto> ToggleCompletionAsync(int habitId);
    Task<HabitDashboardDto> GetDashboardAsync(int userId);
    Task<HabitHistoryDto> GetHistoryAsync(int habitId);
    Task<HabitDto> UpdateRecurrenceAsync(int habitId, HabitRecurrenceUpdateDto recurrence);
    Task<HabitDto> UpdateArchiveAsync(int habitId, HabitArchiveUpdateDto archive);
}
