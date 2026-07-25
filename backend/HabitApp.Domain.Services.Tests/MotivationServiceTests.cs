using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Infrastructure.Data.Interfaces;

namespace HabitApp.Domain.Services.Tests;

public class MotivationServiceTests
{
    [Fact]
    public async Task AchievementsTrackProgressAndUnlockedState()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 5));
        var habit = CreateHabit(1, "Reading", "Daily");
        AddCompletions(habit, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2));
        var service = CreateService(dateService, [habit]);

        var achievements = await service.GetAchievementsAsync(1);

        var firstCheck = achievements.Achievements.Single(achievement => achievement.Id == "first-check");
        var tenChecks = achievements.Achievements.Single(achievement => achievement.Id == "ten-checks");

        Assert.True(firstCheck.IsUnlocked);
        Assert.Equal(100, firstCheck.ProgressPercent);
        Assert.False(tenChecks.IsUnlocked);
        Assert.Equal(20, tenChecks.ProgressPercent);
        Assert.Equal(2, tenChecks.CurrentValue);
    }

    [Fact]
    public async Task StreakCenterSurfacesHabitsAtRiskWhenTodayIsScheduledAndIncomplete()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 5));
        var workout = CreateHabit(1, "Workout", "Daily");
        AddCompletions(workout, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2),
            new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 4));
        var service = CreateService(dateService, [workout]);

        var streakCenter = await service.GetStreakCenterAsync(1);

        var risk = Assert.Single(streakCenter.HabitsAtRisk);
        var streak = Assert.Single(streakCenter.HabitStreaks);

        Assert.Equal("Workout", risk.Title);
        Assert.Equal("high", risk.RiskLevel);
        Assert.Equal(new DateOnly(2026, 6, 5), risk.NextScheduledDate);
        Assert.Equal("at-risk", streak.Status);
    }

    [Fact]
    public async Task ConsistencyScoreUsesOverallRateWeeklyRateAndStreakBonus()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 5));
        var reading = CreateHabit(1, "Reading", "Daily");
        AddCompletions(reading, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2),
            new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 4), new DateOnly(2026, 6, 5));
        var service = CreateService(dateService, [reading]);

        var streakCenter = await service.GetStreakCenterAsync(1);

        Assert.Equal(95, streakCenter.ConsistencyScore);
        Assert.Equal(5, streakCenter.CurrentOverallStreak);
        Assert.Equal("protected", streakCenter.HabitStreaks.Single().Status);
    }

    [Fact]
    public async Task MonthlyChallengesTrackCurrentMonthProgress()
    {
        var dateService = new FixedDateService(new DateOnly(2026, 6, 5));
        var reading = CreateHabit(1, "Reading", "Daily");
        var workout = CreateHabit(2, "Workout", "Weekdays");
        AddCompletions(reading, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2),
            new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 4), new DateOnly(2026, 6, 5));
        AddCompletions(workout, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2));
        var service = CreateService(dateService, [reading, workout]);

        var challenges = await service.GetMonthlyChallengesAsync(1);

        var consistency = challenges.Challenges.Single(challenge => challenge.Id == "monthly-consistency");
        var perfectDays = challenges.Challenges.Single(challenge => challenge.Id == "perfect-days");
        var volume = challenges.Challenges.Single(challenge => challenge.Id == "completion-volume");

        Assert.Equal("junho 2026", challenges.MonthLabel);
        Assert.Equal(70, consistency.CurrentValue);
        Assert.False(consistency.IsCompleted);
        Assert.Equal(2, perfectDays.CurrentValue);
        Assert.Equal(7, volume.CurrentValue);
    }

    private static MotivationService CreateService(
        FixedDateService dateService,
        IReadOnlyCollection<Habit> habits)
    {
        var repository = new InMemoryHabitRepository(habits);
        var recurrenceService = new RecurrenceService();
        var analyticsService = new AnalyticsService(repository, recurrenceService, dateService);

        return new MotivationService(repository, analyticsService, recurrenceService, dateService);
    }

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
