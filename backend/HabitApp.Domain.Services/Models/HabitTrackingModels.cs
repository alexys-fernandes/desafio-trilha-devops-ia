using HabitApp.Domain.Entities;

namespace HabitApp.Domain.Services.Models;

public record HabitStats(
    int CurrentStreak,
    int LongestStreak,
    int WeeklySuccessRate,
    int TotalCompletions);

public record WeeklyIndicator(
    DateOnly Date,
    string DayOfWeek,
    bool IsDue,
    bool IsCompleted);

public record DashboardProgress(
    int TotalHabits,
    int DueToday,
    int CompletedToday,
    int CompletionRate);

public record HabitDashboardItem(
    Habit Habit,
    bool IsDueToday,
    bool CompletedToday,
    HabitStats Stats,
    IReadOnlyCollection<WeeklyIndicator> WeeklyIndicators);

public record HabitDashboard(
    int UserId,
    DateOnly Date,
    IReadOnlyCollection<HabitDashboardItem> ActiveHabits,
    DashboardProgress DailyProgress,
    int WeeklySuccessRate,
    int TotalCompletions);

public record HabitToggleResult(
    Habit Habit,
    DateOnly CompletedDate,
    bool CompletedToday,
    DateTime? CompletedAt,
    HabitStats Stats,
    IReadOnlyCollection<WeeklyIndicator> WeeklyIndicators);

public record HabitHistory(
    Habit Habit,
    IReadOnlyCollection<HabitCompletion> Completions,
    HabitStats Stats,
    IReadOnlyCollection<WeeklyIndicator> WeeklyIndicators);
