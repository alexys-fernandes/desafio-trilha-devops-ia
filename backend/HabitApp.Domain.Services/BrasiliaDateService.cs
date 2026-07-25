using HabitApp.Domain.Services.Interfaces;
using HabitApp.Infrastructure.Data.Utils;

namespace HabitApp.Domain.Services;

public class BrasiliaDateService : IDateService
{
    public DateTime Now => DateTimeUtils.GetHorarioBrasilia();
    public DateOnly Today => DateOnly.FromDateTime(Now);
}
