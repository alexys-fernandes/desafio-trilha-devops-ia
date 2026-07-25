using HabitApp.Application.Dtos;

namespace HabitApp.Application.Interfaces;

public interface IMotivationApplicationService
{
    Task<MotivationSummaryDto> GetSummaryAsync(int userId);
    Task<StreakCenterDto> GetStreakCenterAsync(int userId);
    Task<AchievementSetDto> GetAchievementsAsync(int userId);
    Task<MonthlyChallengeSetDto> GetMonthlyChallengesAsync(int userId);
}
