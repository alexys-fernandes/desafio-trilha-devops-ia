namespace HabitApp.Domain.Services.Models;

public record AchievementProgress(
    string Id,
    string Title,
    string Description,
    string Icon,
    string Category,
    int CurrentValue,
    int TargetValue,
    int ProgressPercent,
    bool IsUnlocked,
    string Message);

public record AchievementSet(
    int UserId,
    DateOnly Date,
    int ConsistencyScore,
    int UnlockedCount,
    int TotalCount,
    IReadOnlyCollection<AchievementProgress> Achievements);

public record MonthlyChallenge(
    string Id,
    string Title,
    string Description,
    string Icon,
    int CurrentValue,
    int TargetValue,
    int ProgressPercent,
    bool IsCompleted,
    string Message);

public record MonthlyChallengeSet(
    int UserId,
    string MonthLabel,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyCollection<MonthlyChallenge> Challenges);

public record MotivationHabitAtRisk(
    int HabitId,
    string Title,
    string Icon,
    string Color,
    string Category,
    int CurrentStreak,
    DateOnly? LastCompletedDate,
    DateOnly? NextScheduledDate,
    int MissedScheduledDatesCount,
    string RiskLevel,
    string Message);

public record HabitStreakStatus(
    int HabitId,
    string Title,
    string Icon,
    string Color,
    string Category,
    int CurrentStreak,
    int LongestStreak,
    int CompletionRate,
    int TotalCompletions,
    DateOnly? LastCompletedDate,
    string Status,
    string Message);

public record StreakCenter(
    int UserId,
    DateOnly Date,
    int ConsistencyScore,
    int CurrentOverallStreak,
    int LongestOverallStreak,
    IReadOnlyCollection<HabitStreakStatus> HabitStreaks,
    IReadOnlyCollection<MotivationHabitAtRisk> HabitsAtRisk,
    IReadOnlyCollection<string> MotivationalInsights);

public record MotivationSummary(
    int UserId,
    DateOnly Date,
    int ConsistencyScore,
    int CurrentOverallStreak,
    int LongestOverallStreak,
    int UnlockedAchievements,
    int TotalAchievements,
    int ActiveMonthlyChallenges,
    MotivationHabitAtRisk? MostAtRiskHabit,
    IReadOnlyCollection<string> MotivationalInsights);
