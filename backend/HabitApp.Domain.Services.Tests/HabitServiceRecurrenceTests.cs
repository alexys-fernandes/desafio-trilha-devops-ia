using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Infrastructure.Data.Interfaces;

namespace HabitApp.Domain.Services.Tests;

public class HabitServiceRecurrenceTests
{
    [Fact]
    public async Task DashboardShowsOnlyHabitsScheduledForToday()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 2));
        var dailyHabit = CreateHabit(1, "Daily");
        var mondayWednesdayFridayHabit = CreateHabit(
            2,
            "SpecificDaysOfWeek",
            """{"daysOfWeek":["Monday","Wednesday","Friday"]}""");
        var weekendHabit = CreateHabit(3, "Weekends");
        var service = CreateService(dateService, [dailyHabit, mondayWednesdayFridayHabit, weekendHabit]);

        var dashboard = await service.GetDashboardAsync(1);

        Assert.Single(dashboard.ActiveHabits);
        Assert.Equal(dailyHabit.Id, dashboard.ActiveHabits.Single().Habit.Id);
        Assert.Equal(1, dashboard.DailyProgress.TotalHabits);
        Assert.Equal(1, dashboard.DailyProgress.DueToday);
    }

    [Fact]
    public async Task WeeklyStatsUseExpectedOccurrencesFromRecurrence()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 5));
        var habit = CreateHabit(
            1,
            "SpecificDaysOfWeek",
            """{"daysOfWeek":["Monday","Wednesday","Friday"]}""");
        habit.Completions =
        [
            CreateCompletion(habit, new DateOnly(2026, 6, 1)),
            CreateCompletion(habit, new DateOnly(2026, 6, 5))
        ];
        var service = CreateService(dateService, [habit]);

        var dashboard = await service.GetDashboardAsync(1);
        var item = dashboard.ActiveHabits.Single();

        Assert.Equal(67, item.Stats.WeeklySuccessRate);
        Assert.Equal(67, dashboard.WeeklySuccessRate);
    }

    [Fact]
    public async Task ToggleCompletionRejectsUnscheduledHabit()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 2));
        var habit = CreateHabit(
            1,
            "SpecificDaysOfWeek",
            """{"daysOfWeek":["Monday","Wednesday","Friday"]}""");
        var repository = new InMemoryHabitRepository([habit]);
        var service = CreateService(dateService, repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ToggleCompletionAsync(habit.Id));
        Assert.Empty(repository.Completions);
    }

    [Fact]
    public async Task ToggleCompletionCreatesCompletionForScheduledHabit()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 3));
        var habit = CreateHabit(
            1,
            "SpecificDaysOfWeek",
            """{"daysOfWeek":["Monday","Wednesday","Friday"]}""");
        var repository = new InMemoryHabitRepository([habit]);
        var service = CreateService(dateService, repository);

        var result = await service.ToggleCompletionAsync(habit.Id);

        Assert.True(result.CompletedToday);
        Assert.Contains(repository.Completions, completion => completion.CompletedDate == dateService.Today);
    }

    private static HabitService CreateService(FixedDateService dateService, IReadOnlyCollection<Habit> habits)
        => CreateService(dateService, new InMemoryHabitRepository(habits));

    private static HabitService CreateService(FixedDateService dateService, InMemoryHabitRepository repository)
        => new(repository, new RecurrenceService(), dateService);

    private static Habit CreateHabit(int id, string recurrenceType, string? recurrenceConfig = null)
    {
        return new Habit
        {
            Id = id,
            UserId = 1,
            Title = $"Habit {id}",
            Icon = "check",
            RecurrenceType = recurrenceType,
            RecurrenceConfig = recurrenceConfig,
            CreatedAt = new DateTime(2026, 6, 1, 8, 0, 0),
            IsArchived = false
        };
    }

    private static HabitCompletion CreateCompletion(Habit habit, DateOnly completedDate)
    {
        return new HabitCompletion
        {
            Id = completedDate.Day,
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
