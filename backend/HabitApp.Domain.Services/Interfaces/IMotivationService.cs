using HabitApp.Domain.Services.Models;

namespace HabitApp.Domain.Services.Interfaces;

public interface IMotivationService
{
    Task<MotivationSummary> GetSummaryAsync(int userId);
    Task<StreakCenter> GetStreakCenterAsync(int userId);
    Task<AchievementSet> GetAchievementsAsync(int userId);
    Task<MonthlyChallengeSet> GetMonthlyChallengesAsync(int userId);
}
