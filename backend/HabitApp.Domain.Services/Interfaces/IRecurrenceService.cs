using HabitApp.Domain.Entities;

namespace HabitApp.Domain.Services.Interfaces;

public interface IRecurrenceService
{
    bool IsHabitScheduledForDate(Habit habit, DateOnly date);
    IReadOnlyCollection<DateOnly> GetScheduledDates(Habit habit, DateOnly startDate, DateOnly endDate);
    int GetExpectedOccurrences(Habit habit, DateOnly startDate, DateOnly endDate);
    string NormalizeRecurrenceTypeForUpdate(string recurrenceType);
}
