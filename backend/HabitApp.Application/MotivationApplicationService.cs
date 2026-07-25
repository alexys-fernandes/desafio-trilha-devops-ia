using HabitApp.Application.Dtos;
using HabitApp.Application.Interfaces;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Domain.Services.Models;

namespace HabitApp.Application;

public class MotivationApplicationService(IMotivationService motivationService) : IMotivationApplicationService
{
    private readonly IMotivationService _motivationService = motivationService;

    public async Task<MotivationSummaryDto> GetSummaryAsync(int userId)
        => ToSummaryDto(await _motivationService.GetSummaryAsync(userId));

    public async Task<StreakCenterDto> GetStreakCenterAsync(int userId)
        => ToStreakCenterDto(await _motivationService.GetStreakCenterAsync(userId));

    public async Task<AchievementSetDto> GetAchievementsAsync(int userId)
        => ToAchievementSetDto(await _motivationService.GetAchievementsAsync(userId));

    public async Task<MonthlyChallengeSetDto> GetMonthlyChallengesAsync(int userId)
        => ToMonthlyChallengeSetDto(await _motivationService.GetMonthlyChallengesAsync(userId));

    private static MotivationSummaryDto ToSummaryDto(MotivationSummary summary)
        => new()
        {
            UserId = summary.UserId,
            Date = summary.Date,
            ConsistencyScore = summary.ConsistencyScore,
            CurrentOverallStreak = summary.CurrentOverallStreak,
            LongestOverallStreak = summary.LongestOverallStreak,
            UnlockedAchievements = summary.UnlockedAchievements,
            TotalAchievements = summary.TotalAchievements,
            ActiveMonthlyChallenges = summary.ActiveMonthlyChallenges,
            MostAtRiskHabit = summary.MostAtRiskHabit is null
                ? null
                : ToHabitAtRiskDto(summary.MostAtRiskHabit),
            MotivationalInsights = summary.MotivationalInsights
        };

    private static StreakCenterDto ToStreakCenterDto(StreakCenter streakCenter)
        => new()
        {
            UserId = streakCenter.UserId,
            Date = streakCenter.Date,
            ConsistencyScore = streakCenter.ConsistencyScore,
            CurrentOverallStreak = streakCenter.CurrentOverallStreak,
            LongestOverallStreak = streakCenter.LongestOverallStreak,
            HabitStreaks = streakCenter.HabitStreaks.Select(ToHabitStreakStatusDto),
            HabitsAtRisk = streakCenter.HabitsAtRisk.Select(ToHabitAtRiskDto),
            MotivationalInsights = streakCenter.MotivationalInsights
        };

    private static AchievementSetDto ToAchievementSetDto(AchievementSet achievementSet)
        => new()
        {
            UserId = achievementSet.UserId,
            Date = achievementSet.Date,
            ConsistencyScore = achievementSet.ConsistencyScore,
            UnlockedCount = achievementSet.UnlockedCount,
            TotalCount = achievementSet.TotalCount,
            Achievements = achievementSet.Achievements.Select(ToAchievementProgressDto)
        };

    private static MonthlyChallengeSetDto ToMonthlyChallengeSetDto(MonthlyChallengeSet challengeSet)
        => new()
        {
            UserId = challengeSet.UserId,
            MonthLabel = challengeSet.MonthLabel,
            StartDate = challengeSet.StartDate,
            EndDate = challengeSet.EndDate,
            Challenges = challengeSet.Challenges.Select(ToMonthlyChallengeDto)
        };

    private static AchievementProgressDto ToAchievementProgressDto(AchievementProgress achievement)
        => new()
        {
            Id = achievement.Id,
            Title = achievement.Title,
            Description = achievement.Description,
            Icon = achievement.Icon,
            Category = achievement.Category,
            CurrentValue = achievement.CurrentValue,
            TargetValue = achievement.TargetValue,
            ProgressPercent = achievement.ProgressPercent,
            IsUnlocked = achievement.IsUnlocked,
            Message = achievement.Message
        };

    private static MonthlyChallengeDto ToMonthlyChallengeDto(MonthlyChallenge challenge)
        => new()
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Description = challenge.Description,
            Icon = challenge.Icon,
            CurrentValue = challenge.CurrentValue,
            TargetValue = challenge.TargetValue,
            ProgressPercent = challenge.ProgressPercent,
            IsCompleted = challenge.IsCompleted,
            Message = challenge.Message
        };

    private static MotivationHabitAtRiskDto ToHabitAtRiskDto(MotivationHabitAtRisk habit)
        => new()
        {
            HabitId = habit.HabitId,
            Title = habit.Title,
            Icon = habit.Icon,
            Color = habit.Color,
            Category = habit.Category,
            CurrentStreak = habit.CurrentStreak,
            LastCompletedDate = habit.LastCompletedDate,
            NextScheduledDate = habit.NextScheduledDate,
            MissedScheduledDatesCount = habit.MissedScheduledDatesCount,
            RiskLevel = habit.RiskLevel,
            Message = habit.Message
        };

    private static HabitStreakStatusDto ToHabitStreakStatusDto(HabitStreakStatus habit)
        => new()
        {
            HabitId = habit.HabitId,
            Title = habit.Title,
            Icon = habit.Icon,
            Color = habit.Color,
            Category = habit.Category,
            CurrentStreak = habit.CurrentStreak,
            LongestStreak = habit.LongestStreak,
            CompletionRate = habit.CompletionRate,
            TotalCompletions = habit.TotalCompletions,
            LastCompletedDate = habit.LastCompletedDate,
            Status = habit.Status,
            Message = habit.Message
        };
}
