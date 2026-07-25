namespace HabitApp.Application.Dtos;

public class AchievementProgressDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
    public int TargetValue { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsUnlocked { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AchievementSetDto
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public int ConsistencyScore { get; set; }
    public int UnlockedCount { get; set; }
    public int TotalCount { get; set; }
    public IEnumerable<AchievementProgressDto> Achievements { get; set; } = [];
}

public class MonthlyChallengeDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
    public int TargetValue { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsCompleted { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class MonthlyChallengeSetDto
{
    public int UserId { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public IEnumerable<MonthlyChallengeDto> Challenges { get; set; } = [];
}

public class MotivationHabitAtRiskDto
{
    public int HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public DateOnly? LastCompletedDate { get; set; }
    public DateOnly? NextScheduledDate { get; set; }
    public int MissedScheduledDatesCount { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class HabitStreakStatusDto
{
    public int HabitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int CompletionRate { get; set; }
    public int TotalCompletions { get; set; }
    public DateOnly? LastCompletedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class StreakCenterDto
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public int ConsistencyScore { get; set; }
    public int CurrentOverallStreak { get; set; }
    public int LongestOverallStreak { get; set; }
    public IEnumerable<HabitStreakStatusDto> HabitStreaks { get; set; } = [];
    public IEnumerable<MotivationHabitAtRiskDto> HabitsAtRisk { get; set; } = [];
    public IEnumerable<string> MotivationalInsights { get; set; } = [];
}

public class MotivationSummaryDto
{
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
    public int ConsistencyScore { get; set; }
    public int CurrentOverallStreak { get; set; }
    public int LongestOverallStreak { get; set; }
    public int UnlockedAchievements { get; set; }
    public int TotalAchievements { get; set; }
    public int ActiveMonthlyChallenges { get; set; }
    public MotivationHabitAtRiskDto? MostAtRiskHabit { get; set; }
    public IEnumerable<string> MotivationalInsights { get; set; } = [];
}
