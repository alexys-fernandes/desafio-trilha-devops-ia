using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Domain.Services.Models;
using HabitApp.Infrastructure.Data.Interfaces;

namespace HabitApp.Domain.Services.Tests;

public class ReminderServiceTests
{
    [Fact]
    public async Task UpcomingRemindersRespectRecurrenceRulesAndCompletionHistory()
    {
        var dateService = new FixedDateService(new DateTime(2026, 6, 5, 8, 0, 0));
        var reading = CreateHabit(1, "Reading", "Daily", reminderTime: TimeSpan.FromHours(9));
        var weekend = CreateHabit(2, "Workout", "Weekends", reminderTime: TimeSpan.FromHours(10));
        var meditation = CreateHabit(3, "Meditation", "Daily", reminderTime: TimeSpan.FromHours(11));
        AddCompletions(meditation, new DateOnly(2026, 6, 5));
        var service = CreateService(dateService, [reading, weekend, meditation]);

        var reminders = await service.GetUpcomingRemindersAsync(
            1,
            new ReminderGenerationOptions(SearchDays: 0));

        var reminder = Assert.Single(reminders);
        Assert.Equal("Reading", reminder.Title);
        Assert.Equal(new DateOnly(2026, 6, 5), reminder.ScheduledDate);
        Assert.Equal("Hora de Reading.", reminder.Message);
    }

    [Fact]
    public async Task QuietHoursSuppressGeneratedPayloads()
    {
        var dateService = new FixedDateService(new DateTime(2026, 6, 5, 23, 0, 0));
        var reading = CreateHabit(1, "Reading", "Daily", reminderTime: TimeSpan.FromHours(23.5));
        var preferences = new UserNotificationPreference
        {
            UserId = 1,
            QuietHoursStart = TimeSpan.FromHours(22),
            QuietHoursEnd = TimeSpan.FromHours(7)
        };
        var service = CreateService(dateService, [reading], [preferences]);

        var payloads = await service.GenerateNotificationPayloadsAsync(
            1,
            new ReminderGenerationOptions(Mode: "Simple"));

        Assert.Empty(payloads);
    }

    [Fact]
    public async Task StreakRiskUsesLiveStreakBeforeTodayBreaks()
    {
        var dateService = new FixedDateService(new DateTime(2026, 6, 5, 13, 0, 0));
        var reading = CreateHabit(1, "Reading", "Daily", reminderTime: TimeSpan.FromHours(9));
        AddCompletions(
            reading,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 2),
            new DateOnly(2026, 6, 3),
            new DateOnly(2026, 6, 4));
        var service = CreateService(dateService, [reading]);

        var risks = await service.GetHabitsAtRiskAsync(
            1,
            new ReminderGenerationOptions(StreakRiskThreshold: 3));

        var risk = Assert.Single(risks);
        Assert.Equal(4, risk.CurrentStreak);
        Assert.Contains("Sua sequência de 4 dias ainda está ativa", risk.Message);
    }

    [Fact]
    public async Task OpenHabitRiskMessageUsesBrazilianPortuguese()
    {
        var dateService = new FixedDateService(new DateTime(2026, 6, 5, 13, 0, 0));
        var hydration = CreateHabit(1, "Beber Água", "Daily", reminderTime: TimeSpan.FromHours(9));
        var service = CreateService(dateService, [hydration]);

        var risks = await service.GetHabitsAtRiskAsync(1);

        var risk = Assert.Single(risks);
        Assert.Equal("Beber Água está programado para hoje e ainda está aberto.", risk.Message);
        Assert.DoesNotContain("is scheduled today", risk.Message);
    }

    [Fact]
    public async Task SmartMotivationBestWeekdayUsesBrazilianPortuguese()
    {
        var dateService = new FixedDateService(new DateTime(2026, 6, 6, 13, 0, 0));
        var hydration = CreateHabit(
            1,
            "Beber Água",
            "SpecificDaysOfWeek",
            """{"daysOfWeek":["Friday"]}""",
            TimeSpan.FromHours(9));
        hydration.CreatedAt = new DateTime(2026, 6, 5, 8, 0, 0);
        AddCompletions(hydration, new DateOnly(2026, 6, 5));
        var service = CreateService(dateService, [hydration]);

        var dashboard = await service.GetDashboardAsync(1);

        Assert.Contains("Você tem mais sucesso em sextas-feiras.", dashboard.SmartMotivations);
        Assert.DoesNotContain(dashboard.SmartMotivations, message => message.Contains("You are"));
        Assert.DoesNotContain(dashboard.SmartMotivations, message => message.Contains("Fridays"));
    }

    [Fact]
    public async Task SmartPayloadCallsOutPersonalRecordProximity()
    {
        var dateService = new FixedDateService(new DateTime(2026, 6, 5, 12, 0, 0));
        var workout = CreateHabit(1, "Workout", "Daily", reminderTime: TimeSpan.FromHours(9));
        workout.CreatedAt = new DateTime(2026, 5, 20, 8, 0, 0);
        AddCompletions(
            workout,
            new DateOnly(2026, 5, 20),
            new DateOnly(2026, 5, 21),
            new DateOnly(2026, 5, 22),
            new DateOnly(2026, 5, 23),
            new DateOnly(2026, 5, 24),
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 2),
            new DateOnly(2026, 6, 3),
            new DateOnly(2026, 6, 4));
        var service = CreateService(dateService, [workout]);

        var payloads = await service.GenerateNotificationPayloadsAsync(
            1,
            new ReminderGenerationOptions(Mode: "Smart", PersonalRecordWarningDays: 2));

        var payload = Assert.Single(payloads, item => item.NotificationType == "StreakRisk");
        Assert.Equal("Falta apenas 1 dia para seu recorde histórico.", payload.Body);
    }

    [Fact]
    public async Task GroupedPayloadSummarizesRemainingHabits()
    {
        var dateService = new FixedDateService(new DateTime(2026, 6, 5, 8, 0, 0));
        var reading = CreateHabit(1, "Reading", "Daily", reminderTime: TimeSpan.FromHours(9));
        var workout = CreateHabit(2, "Workout", "Daily", reminderTime: TimeSpan.FromHours(10));
        var service = CreateService(dateService, [reading, workout]);

        var payloads = await service.GenerateNotificationPayloadsAsync(
            1,
            new ReminderGenerationOptions(Mode: "Grouped"));

        var payload = Assert.Single(payloads);
        Assert.Equal("GroupedReminder", payload.NotificationType);
        Assert.Equal("Você tem 2 hábitos restantes hoje.", payload.Body);
    }

    [Fact]
    public async Task PreferencesCanBeUpdatedPerUser()
    {
        var dateService = new FixedDateService(new DateTime(2026, 6, 5, 8, 0, 0));
        var service = CreateService(dateService, []);

        var preferences = await service.UpdatePreferencesAsync(new UserNotificationPreference
        {
            UserId = 1,
            NotificationsEnabled = false,
            QuietHoursStart = TimeSpan.FromHours(21),
            QuietHoursEnd = TimeSpan.FromHours(6),
            ReminderSoundEnabled = false,
            DefaultReminderType = "Motivation"
        });

        var saved = await service.GetPreferencesAsync(1);

        Assert.False(preferences.NotificationsEnabled);
        Assert.False(saved.ReminderSoundEnabled);
        Assert.Equal(TimeSpan.FromHours(21), saved.QuietHoursStart);
        Assert.Equal("Motivation", saved.DefaultReminderType);
    }

    [Fact]
    public async Task GetPreferencesRecoversWhenConcurrentRequestCreatesDefaultPreference()
    {
        var dateService = new FixedDateService(new DateTime(2026, 6, 5, 8, 0, 0));
        var habitRepository = new InMemoryHabitRepository([]);
        var preferenceRepository = new RacingNotificationPreferenceRepository();
        var recurrenceService = new RecurrenceService();
        var analyticsService = new AnalyticsService(habitRepository, recurrenceService, dateService);
        var service = new ReminderService(
            habitRepository,
            preferenceRepository,
            recurrenceService,
            analyticsService,
            dateService);

        var preferences = await service.GetPreferencesAsync(1);

        Assert.Equal(1, preferences.UserId);
        Assert.Equal(1, preferences.Id);
    }

    private static ReminderService CreateService(
        FixedDateService dateService,
        IReadOnlyCollection<Habit> habits,
        IReadOnlyCollection<UserNotificationPreference>? preferences = null)
    {
        var habitRepository = new InMemoryHabitRepository(habits);
        var preferenceRepository = new InMemoryNotificationPreferenceRepository(preferences ?? []);
        var recurrenceService = new RecurrenceService();
        var analyticsService = new AnalyticsService(habitRepository, recurrenceService, dateService);

        return new ReminderService(
            habitRepository,
            preferenceRepository,
            recurrenceService,
            analyticsService,
            dateService);
    }

    private static Habit CreateHabit(
        int id,
        string title,
        string recurrenceType,
        string? recurrenceConfig = null,
        TimeSpan? reminderTime = null)
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
            ReminderEnabled = reminderTime is not null,
            ReminderTime = reminderTime,
            ReminderTimezone = "America/Sao_Paulo",
            ReminderType = "Standard",
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

    private sealed class FixedDateService(DateTime now) : IDateService
    {
        public DateTime Now { get; } = now;
        public DateOnly Today { get; } = DateOnly.FromDateTime(now);
    }

    private sealed class InMemoryNotificationPreferenceRepository
        : INotificationPreferenceRepository
    {
        private readonly List<UserNotificationPreference> _preferences;

        public InMemoryNotificationPreferenceRepository(
            IReadOnlyCollection<UserNotificationPreference> preferences)
        {
            _preferences = preferences.ToList();
        }

        public Task<UserNotificationPreference> AddAsync(UserNotificationPreference entity)
        {
            entity.Id = _preferences.Count + 1;
            _preferences.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<bool> DeleteAsync(int id)
        {
            var preference = _preferences.FirstOrDefault(item => item.Id == id);
            if (preference is null)
            {
                return Task.FromResult(false);
            }

            preference.IsDeleted = true;
            return Task.FromResult(true);
        }

        public Task<IEnumerable<UserNotificationPreference>> GetAllAsync()
            => Task.FromResult<IEnumerable<UserNotificationPreference>>(
                _preferences.Where(preference => !preference.IsDeleted));

        public Task<UserNotificationPreference?> GetByIdAsync(int id)
            => Task.FromResult(_preferences.FirstOrDefault(preference =>
                preference.Id == id && !preference.IsDeleted));

        public Task<UserNotificationPreference?> GetByUserIdAsync(int userId)
            => Task.FromResult(_preferences.FirstOrDefault(preference =>
                preference.UserId == userId && !preference.IsDeleted));

        public Task<UserNotificationPreference> UpdateAsync(UserNotificationPreference entity)
        {
            var index = _preferences.FindIndex(preference => preference.Id == entity.Id);
            if (index >= 0)
            {
                _preferences[index] = entity;
            }

            return Task.FromResult(entity);
        }
    }

    private sealed class RacingNotificationPreferenceRepository : INotificationPreferenceRepository
    {
        private UserNotificationPreference? _preference;

        public Task<UserNotificationPreference> AddAsync(UserNotificationPreference entity)
        {
            _preference = new UserNotificationPreference
            {
                Id = 1,
                UserId = entity.UserId,
                NotificationsEnabled = true
            };

            throw new InvalidOperationException("Simulated unique constraint race.");
        }

        public Task<bool> DeleteAsync(int id)
            => Task.FromResult(false);

        public Task<IEnumerable<UserNotificationPreference>> GetAllAsync()
            => Task.FromResult<IEnumerable<UserNotificationPreference>>(
                _preference is null ? [] : [_preference]);

        public Task<UserNotificationPreference?> GetByIdAsync(int id)
            => Task.FromResult(_preference?.Id == id ? _preference : null);

        public Task<UserNotificationPreference?> GetByUserIdAsync(int userId)
            => Task.FromResult(_preference?.UserId == userId ? _preference : null);

        public Task<UserNotificationPreference> UpdateAsync(UserNotificationPreference entity)
            => Task.FromResult(entity);
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
