using HabitApp.Domain.Entities;

namespace HabitApp.Domain.Services.Tests;

public class RecurrenceServiceTests
{
    private readonly RecurrenceService _service = new();

    [Fact]
    public void DailySchedulesEveryDate()
    {
        var habit = CreateHabit("Daily");

        var dates = _service.GetScheduledDates(
            habit,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 7));

        Assert.Equal(7, dates.Count);
    }

    [Fact]
    public void WeekdaysSchedulesMondayThroughFriday()
    {
        var habit = CreateHabit("Weekdays");

        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 5)));
        Assert.False(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 6)));
    }

    [Fact]
    public void WeekendsSchedulesSaturdayAndSunday()
    {
        var habit = CreateHabit("Weekends");

        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 6)));
        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 7)));
        Assert.False(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 5)));
    }

    [Fact]
    public void SpecificDaysOfWeekUsesConfiguredDays()
    {
        var habit = CreateHabit(
            "SpecificDaysOfWeek",
            """{"daysOfWeek":["Monday","Wednesday","Friday"]}""");

        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 1)));
        Assert.False(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 2)));
        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 3)));
    }

    [Fact]
    public void SpecificDaysOfWeekAcceptsBrazilianPortugueseDays()
    {
        var habit = CreateHabit(
            "SpecificDaysOfWeek",
            """{"daysOfWeek":["segunda-feira","quarta-feira","sexta-feira"]}""");

        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 1)));
        Assert.False(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 2)));
        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 5)));
    }

    [Fact]
    public void EveryXDaysUsesCreatedDateAsAnchor()
    {
        var habit = CreateHabit("EveryXDays", """{"interval":2}""");

        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 1)));
        Assert.False(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 2)));
        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 6, 3)));
    }

    [Fact]
    public void MonthlyClampsToLastDayOfShortMonth()
    {
        var habit = CreateHabit("Monthly", """{"dayOfMonth":31}""", new DateTime(2026, 1, 31, 8, 0, 0));

        Assert.True(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 2, 28)));
        Assert.False(_service.IsHabitScheduledForDate(habit, new DateOnly(2026, 2, 27)));
    }

    private static Habit CreateHabit(
        string recurrenceType,
        string? recurrenceConfig = null,
        DateTime? createdAt = null)
    {
        return new Habit
        {
            Id = 1,
            UserId = 1,
            Title = "Habit",
            Icon = "check",
            RecurrenceType = recurrenceType,
            RecurrenceConfig = recurrenceConfig,
            CreatedAt = createdAt ?? new DateTime(2026, 6, 1, 8, 0, 0)
        };
    }
}
