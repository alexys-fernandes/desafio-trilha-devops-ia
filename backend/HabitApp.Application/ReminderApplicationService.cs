using AutoMapper;
using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Domain.Services.Models;

namespace HabitApp.Application;

public class ReminderApplicationService(IReminderService reminderService, IMapper mapper)
    : IReminderApplicationService
{
    private readonly IReminderService _reminderService = reminderService;
    private readonly IMapper _mapper = mapper;

    public async Task<NotificationPreferenceDto> GetPreferencesAsync(int userId)
        => _mapper.Map<NotificationPreferenceDto>(await _reminderService.GetPreferencesAsync(userId));

    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        int userId,
        NotificationPreferenceDto preferences)
    {
        preferences.UserId = userId;
        var entity = _mapper.Map<UserNotificationPreference>(preferences);
        var updated = await _reminderService.UpdatePreferencesAsync(entity);

        return _mapper.Map<NotificationPreferenceDto>(updated);
    }

    public async Task<ReminderDashboardDto> GetDashboardAsync(int userId)
        => ToDashboardDto(await _reminderService.GetDashboardAsync(userId));

    public async Task<IEnumerable<NotificationPayloadDto>> GeneratePayloadsAsync(
        int userId,
        string? mode = null)
    {
        var payloads = await _reminderService.GenerateNotificationPayloadsAsync(
            userId,
            string.IsNullOrWhiteSpace(mode) ? null : new ReminderGenerationOptions(Mode: mode));

        return payloads.Select(ToPayloadDto);
    }

    private ReminderDashboardDto ToDashboardDto(ReminderDashboard dashboard)
        => new()
        {
            UserId = dashboard.UserId,
            Date = dashboard.Date,
            GeneratedAt = dashboard.GeneratedAt,
            Preferences = _mapper.Map<NotificationPreferenceDto>(dashboard.Preferences),
            NextReminder = dashboard.NextReminder is null
                ? null
                : ToReminderCandidateDto(dashboard.NextReminder),
            UpcomingReminders = dashboard.UpcomingReminders.Select(ToReminderCandidateDto),
            MissedHabits = dashboard.MissedHabits.Select(ToMissedHabitDto),
            HabitsAtRisk = dashboard.HabitsAtRisk.Select(ToHabitRiskDto),
            SmartMotivations = dashboard.SmartMotivations,
            Payloads = dashboard.Payloads.Select(ToPayloadDto)
        };

    private static HabitReminderCandidateDto ToReminderCandidateDto(HabitReminderCandidate reminder)
        => new()
        {
            HabitId = reminder.HabitId,
            Title = reminder.Title,
            Icon = reminder.Icon,
            Color = reminder.Color,
            Category = reminder.Category,
            ScheduledDate = reminder.ScheduledDate,
            ReminderTime = reminder.ReminderTime,
            ScheduledAt = reminder.ScheduledAt,
            Timezone = reminder.Timezone,
            ReminderType = reminder.ReminderType,
            Message = reminder.Message,
            IsCompleted = reminder.IsCompleted,
            IsSuppressedByQuietHours = reminder.IsSuppressedByQuietHours
        };

    private static MissedHabitReminderDto ToMissedHabitDto(MissedHabitReminder habit)
        => new()
        {
            HabitId = habit.HabitId,
            Title = habit.Title,
            Icon = habit.Icon,
            Color = habit.Color,
            Category = habit.Category,
            ReminderTime = habit.ReminderTime,
            CurrentStreak = habit.CurrentStreak,
            Message = habit.Message
        };

    private static HabitStreakRiskReminderDto ToHabitRiskDto(HabitStreakRiskReminder habit)
        => new()
        {
            HabitId = habit.HabitId,
            Title = habit.Title,
            Icon = habit.Icon,
            Color = habit.Color,
            Category = habit.Category,
            CurrentStreak = habit.CurrentStreak,
            LongestStreak = habit.LongestStreak,
            DaysUntilPersonalRecord = habit.DaysUntilPersonalRecord,
            RiskLevel = habit.RiskLevel,
            Message = habit.Message
        };

    private static NotificationPayloadDto ToPayloadDto(NotificationPayload payload)
        => new()
        {
            Id = payload.Id,
            NotificationType = payload.NotificationType,
            Title = payload.Title,
            Body = payload.Body,
            Priority = payload.Priority,
            ScheduledFor = payload.ScheduledFor,
            Timezone = payload.Timezone,
            HabitId = payload.HabitId,
            GroupKey = payload.GroupKey,
            Metadata = payload.Metadata
        };
}
