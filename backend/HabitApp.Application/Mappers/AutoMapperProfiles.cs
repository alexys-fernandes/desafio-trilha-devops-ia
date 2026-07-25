using AutoMapper;
using HabitApp.Application.Dtos;
using HabitApp.Domain.Entities;

namespace HabitApp.Application.Mappers;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Habit, HabitDto>()
            .ForMember(dest => dest.Streak, opt => opt.Ignore())
            .ForMember(dest => dest.CompletedDays, opt => opt.Ignore());

        CreateMap<HabitDto, Habit>()
            .ForMember(dest => dest.Completions, opt => opt.Ignore());

        CreateMap<HabitCompletion, HabitCompletionDto>();

        CreateMap<UserNotificationPreference, NotificationPreferenceDto>();
        CreateMap<NotificationPreferenceDto, UserNotificationPreference>()
            .ForMember(dest => dest.User, opt => opt.Ignore());

        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Password, opt => opt.Ignore())
            .ForMember(dest => dest.Habits, opt => opt.Ignore());

        CreateMap<UserDto, User>()
            .ForMember(dest => dest.Habits, opt => opt.Ignore());

        CreateMap<User, UserResponseDto>();
    }
}
