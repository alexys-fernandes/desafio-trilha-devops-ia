using AutoMapper;
using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Domain.Services.Models;

namespace HabitApp.Application;

public class HabitApplicationService(IHabitService habitService, IMapper mapper)
    : BaseApplicationService<Habit, HabitDto>(habitService, mapper), IHabitApplicationService
{
    private readonly IHabitService _habitService = habitService;

    public async Task<IEnumerable<HabitDto>> GetByUserIdAsync(int userId)
    {
        var habits = await _habitService.GetByUserIdAsync(userId);
        return await ToHabitDtosAsync(habits);
    }

    public override async Task<IEnumerable<HabitDto>> GetAllAsync()
    {
        var habits = await _habitService.GetAllAsync();
        return await ToHabitDtosAsync(habits);
    }

    public override async Task<HabitDto?> GetByIdAsync(int id)
    {
        try
        {
            var history = await _habitService.GetHistoryAsync(id);
            return ToHabitDto(history.Habit, history.Stats, history.WeeklyIndicators);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    public override async Task<HabitDto> AddAsync(HabitDto entityDto)
    {
        var habit = _mapper.Map<Habit>(entityDto);

        if (string.IsNullOrWhiteSpace(habit.RecurrenceType))
        {
            habit.RecurrenceType = "Daily";
        }

        var createdHabit = await _habitService.AddAsync(habit);

        if (entityDto.CompletedDays?.Length == 7 && entityDto.CompletedDays.Any(completed => completed))
        {
            createdHabit = await _habitService.SyncLegacyCompletedDaysAsync(
                createdHabit.Id,
                entityDto.CompletedDays);
        }

        return await ToHabitDtoAsync(createdHabit.Id);
    }

    public override async Task<HabitDto> UpdateAsync(HabitDto entityDto)
    {
        var history = await _habitService.GetHistoryAsync(entityDto.Id);
        var habit = history.Habit;

        habit.Title = entityDto.Title;
        habit.Icon = entityDto.Icon;
        habit.RecurrenceType = string.IsNullOrWhiteSpace(entityDto.RecurrenceType)
            ? habit.RecurrenceType
            : entityDto.RecurrenceType;
        habit.RecurrenceConfig = string.IsNullOrWhiteSpace(entityDto.RecurrenceConfig)
            ? null
            : entityDto.RecurrenceConfig.Trim();
        habit.ReminderEnabled = entityDto.ReminderEnabled;
        habit.ReminderTime = entityDto.ReminderEnabled ? entityDto.ReminderTime : null;
        habit.ReminderTimezone = string.IsNullOrWhiteSpace(entityDto.ReminderTimezone)
            ? "America/Sao_Paulo"
            : entityDto.ReminderTimezone.Trim();
        habit.ReminderMessage = string.IsNullOrWhiteSpace(entityDto.ReminderMessage)
            ? null
            : entityDto.ReminderMessage.Trim();
        habit.ReminderType = string.IsNullOrWhiteSpace(entityDto.ReminderType)
            ? "Standard"
            : entityDto.ReminderType.Trim();

        if (!string.IsNullOrWhiteSpace(entityDto.Color))
        {
            habit.Color = entityDto.Color;
        }

        if (!string.IsNullOrWhiteSpace(entityDto.Category))
        {
            habit.Category = entityDto.Category;
        }

        await _habitService.UpdateAsync(habit);

        if (entityDto.CompletedDays?.Length == 7)
        {
            var currentWeek = history.WeeklyIndicators
                .OrderBy(indicator => indicator.Date)
                .Select(indicator => indicator.IsCompleted)
                .ToArray();

            if (!currentWeek.SequenceEqual(entityDto.CompletedDays))
            {
                await _habitService.SyncLegacyCompletedDaysAsync(entityDto.Id, entityDto.CompletedDays);
            }
        }

        var updatedHistory = await _habitService.GetHistoryAsync(entityDto.Id);
        return ToHabitDto(updatedHistory.Habit, updatedHistory.Stats, updatedHistory.WeeklyIndicators);
    }

    public async Task<HabitToggleCompletionDto> ToggleCompletionAsync(int habitId)
    {
        var result = await _habitService.ToggleCompletionAsync(habitId);
        return new HabitToggleCompletionDto
        {
            HabitId = result.Habit.Id,
            CompletedDate = result.CompletedDate,
            CompletedToday = result.CompletedToday,
            CompletedAt = result.CompletedAt,
            Habit = ToDashboardItemDto(
                new HabitDashboardItem(
                    result.Habit,
                    result.WeeklyIndicators.Any(indicator =>
                        indicator.Date == result.CompletedDate && indicator.IsDue),
                    result.CompletedToday,
                    result.Stats,
                    result.WeeklyIndicators))
        };
    }

    public async Task<HabitDashboardDto> GetDashboardAsync(int userId)
    {
        var dashboard = await _habitService.GetDashboardAsync(userId);

        return new HabitDashboardDto
        {
            UserId = dashboard.UserId,
            Date = dashboard.Date,
            ActiveHabits = dashboard.ActiveHabits.Select(ToDashboardItemDto),
            DailyProgress = new HabitDashboardProgressDto
            {
                TotalHabits = dashboard.DailyProgress.TotalHabits,
                DueToday = dashboard.DailyProgress.DueToday,
                CompletedToday = dashboard.DailyProgress.CompletedToday,
                CompletionRate = dashboard.DailyProgress.CompletionRate
            },
            WeeklySuccessRate = dashboard.WeeklySuccessRate,
            TotalCompletions = dashboard.TotalCompletions
        };
    }

    public async Task<HabitHistoryDto> GetHistoryAsync(int habitId)
    {
        var history = await _habitService.GetHistoryAsync(habitId);

        return new HabitHistoryDto
        {
            HabitId = history.Habit.Id,
            UserId = history.Habit.UserId,
            Title = history.Habit.Title,
            RecurrenceType = history.Habit.RecurrenceType,
            RecurrenceConfig = history.Habit.RecurrenceConfig,
            Stats = ToStatsDto(history.Stats),
            WeeklyIndicators = history.WeeklyIndicators.Select(ToWeeklyIndicatorDto),
            Completions = _mapper.Map<IEnumerable<HabitCompletionDto>>(history.Completions)
        };
    }

    public async Task<HabitDto> UpdateRecurrenceAsync(int habitId, HabitRecurrenceUpdateDto recurrence)
    {
        var habit = await _habitService.UpdateRecurrenceAsync(
            habitId,
            recurrence.RecurrenceType,
            recurrence.RecurrenceConfig,
            recurrence.ReminderEnabled,
            recurrence.ReminderTime,
            recurrence.ReminderTimezone,
            recurrence.ReminderMessage,
            recurrence.ReminderType);

        return await ToHabitDtoAsync(habit.Id);
    }

    public async Task<HabitDto> UpdateArchiveAsync(int habitId, HabitArchiveUpdateDto archive)
    {
        var habit = await _habitService.UpdateArchiveAsync(habitId, archive.IsArchived);
        return await ToHabitDtoAsync(habit.Id);
    }

    private async Task<IEnumerable<HabitDto>> ToHabitDtosAsync(IEnumerable<Habit> habits)
    {
        var dtos = new List<HabitDto>();

        foreach (var habit in habits)
        {
            dtos.Add(await ToHabitDtoAsync(habit.Id));
        }

        return dtos;
    }

    private async Task<HabitDto> ToHabitDtoAsync(int habitId)
    {
        var history = await _habitService.GetHistoryAsync(habitId);
        return ToHabitDto(history.Habit, history.Stats, history.WeeklyIndicators);
    }

    private HabitDto ToHabitDto(
        Habit habit,
        HabitStats stats,
        IReadOnlyCollection<WeeklyIndicator> weeklyIndicators)
    {
        var dto = _mapper.Map<HabitDto>(habit);
        dto.Streak = stats.CurrentStreak;
        dto.CompletedDays = weeklyIndicators
            .OrderBy(indicator => indicator.Date)
            .Select(indicator => indicator.IsCompleted)
            .ToArray();

        return dto;
    }

    private static HabitDashboardItemDto ToDashboardItemDto(HabitDashboardItem item)
    {
        return new HabitDashboardItemDto
        {
            Id = item.Habit.Id,
            UserId = item.Habit.UserId,
            Title = item.Habit.Title,
            Icon = item.Habit.Icon,
            Color = item.Habit.Color,
            Category = item.Habit.Category,
            RecurrenceType = item.Habit.RecurrenceType,
            RecurrenceConfig = item.Habit.RecurrenceConfig,
            ReminderEnabled = item.Habit.ReminderEnabled,
            ReminderTime = item.Habit.ReminderTime,
            ReminderTimezone = item.Habit.ReminderTimezone,
            ReminderMessage = item.Habit.ReminderMessage,
            ReminderType = item.Habit.ReminderType,
            IsArchived = item.Habit.IsArchived,
            IsDueToday = item.IsDueToday,
            CompletedToday = item.CompletedToday,
            CurrentStreak = item.Stats.CurrentStreak,
            LongestStreak = item.Stats.LongestStreak,
            WeeklySuccessRate = item.Stats.WeeklySuccessRate,
            TotalCompletions = item.Stats.TotalCompletions,
            WeeklyIndicators = item.WeeklyIndicators.Select(ToWeeklyIndicatorDto)
        };
    }

    private static HabitStatsDto ToStatsDto(HabitStats stats)
    {
        return new HabitStatsDto
        {
            CurrentStreak = stats.CurrentStreak,
            LongestStreak = stats.LongestStreak,
            WeeklySuccessRate = stats.WeeklySuccessRate,
            TotalCompletions = stats.TotalCompletions
        };
    }

    private static HabitWeeklyIndicatorDto ToWeeklyIndicatorDto(WeeklyIndicator indicator)
    {
        return new HabitWeeklyIndicatorDto
        {
            Date = indicator.Date,
            DayOfWeek = indicator.DayOfWeek,
            IsDue = indicator.IsDue,
            IsCompleted = indicator.IsCompleted
        };
    }
}
