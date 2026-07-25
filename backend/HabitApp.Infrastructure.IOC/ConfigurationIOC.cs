using Microsoft.Extensions.DependencyInjection;

namespace HabitApp.Infrastructure.IOC;

public static class ConfigurationIOC
{
    public static void ConfigureServices(IServiceCollection services)
        => ModuleIOC.RegisterServices(services);
}