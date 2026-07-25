using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Infrastructure.Data.Interfaces;

namespace HabitApp.Domain.Services.Tests;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task HabitCompletionRateUsesOnlyScheduledOccurrences()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 5));
        var habit = CreateHabit(
            1,
            "Reading",
            "SpecificDaysOfWeek",
            """{"daysOfWeek":["Monday","Wednesday","Friday"]}""");
        habit.Completions =
        [
            CreateCompletion(habit, new DateOnly(2026, 6, 1)),
            CreateCompletion(habit, new DateOnly(2026, 6, 5))
        ];
        var service = CreateService(dateService, [habit]);

        var habitAnalytics = await service.GetHabitAnalyticsAsync(1, habit.Id);
        var overview = await service.GetOverviewAsync(1);

        Assert.Equal(67, habitAnalytics.CompletionRate);
        Assert.Equal(67, overview.AverageCompletionRate);
        Assert.Contains(new DateOnly(2026, 6, 3), habitAnalytics.MissedScheduledDates);
        Assert.DoesNotContain(new DateOnly(2026, 6, 2), habitAnalytics.MissedScheduledDates);
        Assert.DoesNotContain(new DateOnly(2026, 6, 4), habitAnalytics.MissedScheduledDates);
    }

    [Fact]
    public async Task OverviewIdentifiesBestAndWeakestHabits()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 5));
        var reading = CreateHabit(1, "Reading", "Daily");
        var workout = CreateHabit(2, "Workout", "Daily");
        AddCompletions(reading, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2),
            new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 4), new DateOnly(2026, 6, 5));
        AddCompletions(workout, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2));
        var service = CreateService(dateService, [reading, workout]);

        var overview = await service.GetOverviewAsync(1);

        Assert.Equal("Reading", overview.BestHabit?.Title);
        Assert.Equal(100, overview.BestHabit?.CompletionRate);
        Assert.Equal("Workout", overview.WeakestHabit?.Title);
        Assert.Equal(40, overview.WeakestHabit?.CompletionRate);
    }

    [Fact]
    public async Task OverviewIdentifiesBestAndWeakestWeekdays()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 2));
        var habit = CreateHabit(
            1,
            "Focused work",
            "SpecificDaysOfWeek",
            """{"daysOfWeek":["Monday","Tuesday"]}""");
        AddCompletions(habit, new DateOnly(2026, 6, 1));
        var service = CreateService(dateService, [habit]);

        var overview = await service.GetOverviewAsync(1);

        Assert.Equal("Monday", overview.BestDayOfWeek?.DayOfWeek);
        Assert.Equal(100, overview.BestDayOfWeek?.CompletionRate);
        Assert.Equal("Tuesday", overview.WeakestDayOfWeek?.DayOfWeek);
        Assert.Equal(0, overview.WeakestDayOfWeek?.CompletionRate);
    }

    [Fact]
    public async Task CalendarGeneratesPerfectPartialMissedAndNoneStatuses()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 3));
        var reading = CreateHabit(1, "Reading", "Daily");
        var workout = CreateHabit(2, "Workout", "Daily");
        AddCompletions(reading, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2));
        AddCompletions(workout, new DateOnly(2026, 6, 1));
        var service = CreateService(dateService, [reading, workout]);

        var calendar = await service.GetCalendarAsync(1);

        var beforeHabitCreation = calendar.Days.Single(day => day.Date == new DateOnly(2026, 5, 31));
        var perfectDay = calendar.Days.Single(day => day.Date == new DateOnly(2026, 6, 1));
        var partialDay = calendar.Days.Single(day => day.Date == new DateOnly(2026, 6, 2));
        var missedDay = calendar.Days.Single(day => day.Date == new DateOnly(2026, 6, 3));

        Assert.Equal("none", beforeHabitCreation.Status);
        Assert.Equal("perfect", perfectDay.Status);
        Assert.Equal(100, perfectDay.CompletionRate);
        Assert.Equal("partial", partialDay.Status);
        Assert.Equal(50, partialDay.CompletionRate);
        Assert.Equal("missed", missedDay.Status);
        Assert.Equal(0, missedDay.CompletionRate);
    }

    [Fact]
    public async Task TrendsReturnGroupedWindowsWithDailyBreakdown()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 3));
        var reading = CreateHabit(1, "Reading", "Daily");
        var workout = CreateHabit(2, "Workout", "Daily");
        AddCompletions(reading, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2));
        AddCompletions(workout, new DateOnly(2026, 6, 1));
        var service = CreateService(dateService, [reading, workout]);

        var trends = await service.GetTrendsAsync(1);

        Assert.Equal(7, trends.Last7Days.DailyBreakdown.Count);
        Assert.Equal(6, trends.Last7Days.ScheduledHabits);
        Assert.Equal(3, trends.Last7Days.CompletedHabits);
        Assert.Equal(50, trends.Last7Days.CompletionRate);
        Assert.Equal(30, trends.Last30Days.DailyBreakdown.Count);
        Assert.Equal(90, trends.Last90Days.DailyBreakdown.Count);
    }

    private static AnalyticsService CreateService(
        FixedDateService dateService,
        IReadOnlyCollection<Habit> habits)
        => new(new InMemoryHabitRepository(habits), new RecurrenceService(), dateService);

    private static Habit CreateHabit(
        int id,
        string title,
        string recurrenceType,
        string? recurrenceConfig = null)
    {
        return new Habit
        {
            Id = id,
            UserId = 1,
            Title = title,
            Icon = "check",
            Color = "#fc6976",
            Category = "Personal",
            RecurrenceType = recurrenceType,
            RecurrenceConfig = recurrenceConfig,
            CreatedAt = new DateTime(2026, 6, 1, 8, 0, 0),
            IsArchived = false
        };
    }

    private static void AddCompletions(Habit habit, params DateOnly[] completedDates)
    {
        habit.Completions = completedDates
            .Select(date => CreateCompletion(habit, date))
            .ToList();
    }

    private static HabitCompletion CreateCompletion(Habit habit, DateOnly completedDate)
    {
        return new HabitCompletion
        {
            Id = habit.Id * 100 + completedDate.Day,
            HabitId = habit.Id,
            UserId = habit.UserId,
            CompletedDate = completedDate,
            CompletedAt = completedDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(9))),
            CreatedAt = completedDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)))
        };
    }

    private sealed class FixedDateService(DateOnly today) : IDateService
    {
        public DateTime Now { get; } = today.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(13)));
        public DateOnly Today { get; } = today;
    }

    private sealed class InMemoryHabitRepository : IHabitRepository
    {
        private readonly List<Habit> _habits;

        public InMemoryHabitRepository(IReadOnlyCollection<Habit> habits)
        {
            _habits = habits.ToList();
            Completions = _habits.SelectMany(habit => habit.Completions).ToList();
        }

        public List<HabitCompletion> Completions { get; }

        public Task<Habit> AddAsync(Habit entity)
        {
            _habits.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<HabitCompletion> AddCompletionAsync(HabitCompletion completion)
        {
            completion.Id = Completions.Count + 1;
            Completions.Add(completion);
            _habits.First(habit => habit.Id == completion.HabitId).Completions.Add(completion);
            return Task.FromResult(completion);
        }

        public Task<bool> DeleteAsync(int id)
        {
            var habit = _habits.FirstOrDefault(habit => habit.Id == id);
            if (habit is null)
            {
                return Task.FromResult(false);
            }

            habit.IsDeleted = true;
            return Task.FromResult(true);
        }

        public Task<IEnumerable<Habit>> GetAllAsync()
            => Task.FromResult<IEnumerable<Habit>>(_habits.Where(habit => !habit.IsDeleted));

        public Task<Habit?> GetByIdAsync(int id)
            => Task.FromResult(_habits.FirstOrDefault(habit => habit.Id == id && !habit.IsDeleted));

        public Task<Habit?> GetByIdWithCompletionsAsync(int id)
            => GetByIdAsync(id);

        public Task<IEnumerable<Habit>> GetByUserIdAsync(int userId)
            => Task.FromResult<IEnumerable<Habit>>(_habits.Where(habit =>
                habit.UserId == userId && !habit.IsDeleted));

        public Task<IEnumerable<Habit>> GetByUserIdWithCompletionsAsync(int userId, bool activeOnly = false)
            => Task.FromResult<IEnumerable<Habit>>(_habits.Where(habit =>
                habit.UserId == userId
                && !habit.IsDeleted
                && (!activeOnly || !habit.IsArchived)));

        public Task<HabitCompletion?> GetCompletionAsync(int habitId, int userId, DateOnly completedDate)
            => Task.FromResult(Completions.FirstOrDefault(completion =>
                completion.HabitId == habitId
                && completion.UserId == userId
                && completion.CompletedDate == completedDate
                && !completion.IsDeleted));

        public Task SoftDeleteCompletionAsync(HabitCompletion completion)
        {
            completion.IsDeleted = true;
            return Task.CompletedTask;
        }

        public Task<Habit> UpdateAsync(Habit entity)
        {
            var index = _habits.FindIndex(habit => habit.Id == entity.Id);
            if (index >= 0)
            {
                _habits[index] = entity;
            }

            return Task.FromResult(entity);
        }
    }
}
