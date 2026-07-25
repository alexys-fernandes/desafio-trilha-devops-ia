using HabitApp.Application;
using HabitApp.Application.Interfaces;
using HabitApp.Domain.Services;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Infrastructure.Data.Interfaces;
using HabitApp.Infrastructure.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HabitApp.Infrastructure.IOC;

public static class ModuleIOC
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IUserApplicationService, UserApplicationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IHabitApplicationService, HabitApplicationService>();
        services.AddScoped<IHabitService, HabitService>();
        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddScoped<IAnalyticsApplicationService, AnalyticsApplicationService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IMotivationApplicationService, MotivationApplicationService>();
        services.AddScoped<IMotivationService, MotivationService>();
        services.AddScoped<IReminderApplicationService, ReminderApplicationService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<IRecurrenceService, RecurrenceService>();
        services.AddScoped<IDateService, BrasiliaDateService>();
        services.AddScoped<AICoachApplicationService>();
    }
}
