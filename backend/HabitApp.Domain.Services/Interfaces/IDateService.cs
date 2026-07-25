namespace HabitApp.Domain.Services.Interfaces;

public interface IDateService
{
    DateTime Now { get; }
    DateOnly Today { get; }
}
