using System.Globalization;
using HabitApp.Domain.Entities;
using HabitApp.Domain.Services.Interfaces;
using HabitApp.Domain.Services.Models;
using HabitApp.Infrastructure.Data.Interfaces;

namespace HabitApp.Domain.Services;

public class MotivationService(
    IHabitRepository habitRepository,
    IAnalyticsService analyticsService,
    IRecurrenceService recurrenceService,
    IDateService dateService) : IMotivationService
{
    private readonly IHabitRepository _habitRepository = habitRepository;
    private readonly IAnalyticsService _analyticsService = analyticsService;
    private readonly IRecurrenceService _recurrenceService = recurrenceService;
    private readonly IDateService _dateService = dateService;

    public async Task<MotivationSummary> GetSummaryAsync(int userId)
    {
        var overview = await _analyticsService.GetOverviewAsync(userId);
        var trends = await _analyticsService.GetTrendsAsync(userId);
        var streakCenter = await GetStreakCenterAsync(userId);
        var achievements = await GetAchievementsAsync(userId);
        var challenges = await GetMonthlyChallengesAsync(userId);
        var insights = BuildMotivationalInsights(
            overview,
            trends,
            streakCenter.HabitsAtRisk,
            achievements,
            challenges);

        return new MotivationSummary(
            userId,
            _dateService.Today,
            CalculateConsistencyScore(overview, trends),
            overview.CurrentOverallStreak,
            overview.LongestOverallStreak,
            achievements.UnlockedCount,
            achievements.TotalCount,
            challenges.Challenges.Count(challenge => !challenge.IsCompleted),
            streakCenter.HabitsAtRisk.FirstOrDefault(),
            insights);
    }

    public async Task<StreakCenter> GetStreakCenterAsync(int userId)
    {
        var today = _dateService.Today;
        var overview = await _analyticsService.GetOverviewAsync(userId);
        var trends = await _analyticsService.GetTrendsAsync(userId);
        var activeHabits = await GetActiveHabitsAsync(userId);
        var habitAnalytics = await GetHabitAnalyticsAsync(userId, activeHabits);
        var risks = BuildHabitsAtRisk(activeHabits, habitAnalytics, today);
        var streaks = habitAnalytics
            .OrderByDescending(habit => habit.CurrentStreak)
            .ThenByDescending(habit => habit.LongestStreak)
            .ThenBy(habit => habit.Title)
            .Select(habit => ToHabitStreakStatus(habit, risks))
            .ToList();

        return new StreakCenter(
            userId,
            today,
            CalculateConsistencyScore(overview, trends),
            overview.CurrentOverallStreak,
            overview.LongestOverallStreak,
            streaks,
            risks,
            BuildStreakInsights(overview, trends, risks, streaks));
    }

    public async Task<AchievementSet> GetAchievementsAsync(int userId)
    {
        var overview = await _analyticsService.GetOverviewAsync(userId);
        var trends = await _analyticsService.GetTrendsAsync(userId);
        var calendar = await _analyticsService.GetCalendarAsync(userId);
        var activeHabits = await GetActiveHabitsAsync(userId);
        var habitAnalytics = await GetHabitAnalyticsAsync(userId, activeHabits);
        var consistencyScore = CalculateConsistencyScore(overview, trends);
        var achievements = BuildAchievements(
            overview,
            trends,
            calendar,
            habitAnalytics,
            consistencyScore);

        return new AchievementSet(
            userId,
            _dateService.Today,
            consistencyScore,
            achievements.Count(achievement => achievement.IsUnlocked),
            achievements.Count,
            achievements);
    }

    public async Task<MonthlyChallengeSet> GetMonthlyChallengesAsync(int userId)
    {
        var today = _dateService.Today;
        var startDate = new DateOnly(today.Year, today.Month, 1);
        var overview = await _analyticsService.GetOverviewAsync(userId);
        var calendar = await _analyticsService.GetCalendarAsync(userId);
        var activeHabits = await GetActiveHabitsAsync(userId);
        var habitAnalytics = await GetHabitAnalyticsAsync(userId, activeHabits);
        var monthDays = calendar.Days
            .Where(day => day.Date >= startDate && day.Date <= today)
            .ToList();
        var scheduledThisMonth = monthDays.Sum(day => day.ScheduledCount);
        var completedThisMonth = monthDays.Sum(day => day.CompletedCount);
        var monthCompletionRate = scheduledThisMonth == 0
            ? 0
            : Percentage(completedThisMonth, scheduledThisMonth);
        var perfectDays = monthDays.Count(day => day.Status == "perfect");
        var protectedHabits = habitAnalytics.Count(habit => habit.CurrentStreak > 0);
        var completionTarget = Math.Max(12, Math.Min(40, scheduledThisMonth == 0 ? 20 : scheduledThisMonth));
        var challenges = new List<MonthlyChallenge>
        {
            CreateChallenge(
                "monthly-consistency",
                "Consistência mensal",
                "Alcance uma taxa de conclusão de 85% nos hábitos programados deste mês.",
                "calendar_month",
                monthCompletionRate,
                85,
                "Mantenha o mês estável."),
            CreateChallenge(
                "perfect-days",
                "Dias perfeitos",
                "Finalize todos os hábitos programados em 10 dias deste mês.",
                "verified",
                perfectDays,
                10,
                "Acumule mais dias perfeitos."),
            CreateChallenge(
                "completion-volume",
                "Volume de conclusões",
                $"Conclua {completionTarget} marcações de hábitos programados neste mês.",
                "done_all",
                completedThisMonth,
                completionTarget,
                "Cada marcação programada avança este desafio."),
            CreateChallenge(
                "protect-streaks",
                "Proteja suas sequências",
                "Mantenha todos os hábitos ativos com sequência em andamento.",
                "local_fire_department",
                protectedHabits,
                Math.Max(1, overview.TotalActiveHabits),
                "Traga todos os hábitos de volta para uma sequência.")
        };

        return new MonthlyChallengeSet(
            userId,
            today.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("pt-BR")),
            startDate,
            today,
            challenges);
    }

    private async Task<List<Habit>> GetActiveHabitsAsync(int userId)
        => (await _habitRepository.GetByUserIdWithCompletionsAsync(userId, activeOnly: true))
            .OrderBy(habit => habit.Title)
            .ToList();

    private async Task<IReadOnlyCollection<HabitAnalytics>> GetHabitAnalyticsAsync(
        int userId,
        IReadOnlyCollection<Habit> habits)
    {
        var analytics = new List<HabitAnalytics>();

        foreach (var habit in habits)
        {
            analytics.Add(await _analyticsService.GetHabitAnalyticsAsync(userId, habit.Id));
        }

        return analytics;
    }

    private IReadOnlyCollection<MotivationHabitAtRisk> BuildHabitsAtRisk(
        IReadOnlyCollection<Habit> activeHabits,
        IReadOnlyCollection<HabitAnalytics> habitAnalytics,
        DateOnly today)
    {
        var habitsById = activeHabits.ToDictionary(habit => habit.Id);

        return habitAnalytics
            .Select(habit => BuildHabitRisk(habitsById[habit.HabitId], habit, today))
            .Where(risk => risk is not null)
            .Select(risk => risk!)
            .OrderByDescending(risk => RiskWeight(risk.RiskLevel))
            .ThenByDescending(risk => risk.MissedScheduledDatesCount)
            .ThenBy(risk => risk.Title)
            .ToList();
    }

    private MotivationHabitAtRisk? BuildHabitRisk(Habit habit, HabitAnalytics analytics, DateOnly today)
    {
        var missedThisWeek = analytics.WeeklyTrend.Sum(day =>
            Math.Max(0, day.ScheduledHabits - day.CompletedHabits));
        var isScheduledToday = _recurrenceService.IsHabitScheduledForDate(habit, today);
        var isMissedToday = analytics.MissedScheduledDates.Contains(today);
        var completionRate = analytics.CompletionRate;

        if (!isMissedToday && missedThisWeek == 0 && completionRate >= 60)
        {
            return null;
        }

        var riskLevel = isMissedToday || missedThisWeek >= 2 || completionRate < 40
            ? "high"
            : "medium";
        var message = isScheduledToday && isMissedToday
            ? $"{analytics.Title} está programado para hoje e ainda está aberto."
            : completionRate < 40
                ? $"{analytics.Title} está abaixo de 40% de consistência."
                : $"{analytics.Title} tem dias programados não concluídos nesta semana.";

        return new MotivationHabitAtRisk(
            analytics.HabitId,
            analytics.Title,
            analytics.Icon,
            analytics.Color,
            analytics.Category,
            analytics.CurrentStreak,
            analytics.LastCompletedDate,
            FindNextScheduledDate(habit, today),
            missedThisWeek,
            riskLevel,
            message);
    }

    private DateOnly? FindNextScheduledDate(Habit habit, DateOnly today)
    {
        for (var date = today; date <= today.AddDays(14); date = date.AddDays(1))
        {
            if (_recurrenceService.IsHabitScheduledForDate(habit, date))
            {
                return date;
            }
        }

        return null;
    }

    private HabitStreakStatus ToHabitStreakStatus(
        HabitAnalytics habit,
        IReadOnlyCollection<MotivationHabitAtRisk> risks)
    {
        var risk = risks.FirstOrDefault(item => item.HabitId == habit.HabitId);

        if (habit.TotalCompletions == 0)
        {
            return new HabitStreakStatus(
                habit.HabitId,
                habit.Title,
                habit.Icon,
                habit.Color,
                habit.Category,
                habit.CurrentStreak,
                habit.LongestStreak,
                habit.CompletionRate,
                habit.TotalCompletions,
                habit.LastCompletedDate,
                "new",
                "Conclua este hábito para iniciar uma sequência.");
        }

        if (risk is not null)
        {
            return new HabitStreakStatus(
                habit.HabitId,
                habit.Title,
                habit.Icon,
                habit.Color,
                habit.Category,
                habit.CurrentStreak,
                habit.LongestStreak,
                habit.CompletionRate,
                habit.TotalCompletions,
                habit.LastCompletedDate,
                "at-risk",
                risk.Message);
        }

        if (habit.CurrentStreak > 0)
        {
            return new HabitStreakStatus(
                habit.HabitId,
                habit.Title,
                habit.Icon,
                habit.Color,
                habit.Category,
                habit.CurrentStreak,
                habit.LongestStreak,
                habit.CompletionRate,
                habit.TotalCompletions,
                habit.LastCompletedDate,
                "protected",
                $"{habit.Title} está protegido hoje.");
        }

        return new HabitStreakStatus(
            habit.HabitId,
            habit.Title,
            habit.Icon,
            habit.Color,
            habit.Category,
            habit.CurrentStreak,
            habit.LongestStreak,
            habit.CompletionRate,
            habit.TotalCompletions,
            habit.LastCompletedDate,
            "rebuilding",
            $"Reinicie {habit.Title} com a próxima conclusão programada.");
    }

    private static IReadOnlyCollection<AchievementProgress> BuildAchievements(
        AnalyticsOverview overview,
        TrendAnalytics trends,
        CalendarAnalytics calendar,
        IReadOnlyCollection<HabitAnalytics> habitAnalytics,
        int consistencyScore)
    {
        var perfectStreak = CalculateLongestPerfectDayStreak(calendar.Days);
        var protectedHabits = habitAnalytics.Count(habit => habit.CurrentStreak >= 3);

        return
        [
            CreateAchievement(
                "first-check",
                "Primeira marcação",
                "Conclua seu primeiro hábito programado.",
                "check_circle",
                "Fundação",
                overview.TotalCompletions,
                1,
                "Uma marcação inicia o sistema."),
            CreateAchievement(
                "ten-checks",
                "Dez marcações",
                "Alcance 10 conclusões de hábitos no total.",
                "done_all",
                "Fundação",
                overview.TotalCompletions,
                10,
                "Construa uma base visível."),
            CreateAchievement(
                "hundred-checks",
                "Cem marcações",
                "Alcance 100 conclusões de hábitos no total.",
                "workspace_premium",
                "Marco",
                overview.TotalCompletions,
                100,
                "O progresso de longo prazo está se acumulando."),
            CreateAchievement(
                "week-streak",
                "Sequência de sete dias",
                "Mantenha uma sequência geral perfeita por 7 dias programados.",
                "local_fire_department",
                "Sequência",
                overview.LongestOverallStreak,
                7,
                "Proteja todos os dias programados por uma semana."),
            CreateAchievement(
                "month-streak",
                "Sequência de trinta dias",
                "Mantenha uma sequência geral perfeita por 30 dias programados.",
                "bolt",
                "Sequência",
                overview.LongestOverallStreak,
                30,
                "Um mês inteiro de hábitos protegidos."),
            CreateAchievement(
                "consistent",
                "Ritmo confiável",
                "Alcance uma taxa de conclusão de 70% considerando a recorrência.",
                "trending_up",
                "Consistência",
                overview.AverageCompletionRate,
                70,
                "Mantenha hábitos programados acima de 70%."),
            CreateAchievement(
                "elite-consistency",
                "Consistência de elite",
                "Alcance uma taxa de conclusão de 90% considerando a recorrência.",
                "stars",
                "Consistência",
                overview.AverageCompletionRate,
                90,
                "Um ritmo excelente nos dias programados."),
            CreateAchievement(
                "perfect-week",
                "Semana perfeita",
                "Conclua todos os hábitos programados por 7 dias programados seguidos.",
                "verified",
                "Consistência",
                perfectStreak,
                7,
                "Acumule sete dias programados perfeitos."),
            CreateAchievement(
                "flow-builder",
                "Construtor de fluxo",
                "Mantenha 3 hábitos ativos em sequências de pelo menos 3 dias.",
                "account_tree",
                "Combinação de hábitos",
                protectedHabits,
                3,
                "Crie consistência em vários hábitos."),
            CreateAchievement(
                "strong-week",
                "Semana forte",
                "Alcance uma taxa de conclusão de 80% nos últimos 7 dias.",
                "insights",
                "Impulso",
                trends.Last7Days.CompletionRate,
                80,
                "Finalize a semana acima de 80%."),
            CreateAchievement(
                "consistency-score",
                "Pontuação de consistência",
                "Alcance 85 pontos de consistência motivacional.",
                "speed",
                "Motivação",
                consistencyScore,
                85,
                "Equilibre taxa de conclusão, ritmo semanal e sequência.")
        ];
    }

    private static AchievementProgress CreateAchievement(
        string id,
        string title,
        string description,
        string icon,
        string category,
        int currentValue,
        int targetValue,
        string lockedMessage)
    {
        var progressPercent = ProgressPercent(currentValue, targetValue);
        var isUnlocked = currentValue >= targetValue;

        return new AchievementProgress(
            id,
            title,
            description,
            icon,
            category,
            Math.Min(currentValue, targetValue),
            targetValue,
            progressPercent,
            isUnlocked,
            isUnlocked ? "Liberada" : lockedMessage);
    }

    private static MonthlyChallenge CreateChallenge(
        string id,
        string title,
        string description,
        string icon,
        int currentValue,
        int targetValue,
        string activeMessage)
    {
        var progressPercent = ProgressPercent(currentValue, targetValue);
        var isCompleted = currentValue >= targetValue;

        return new MonthlyChallenge(
            id,
            title,
            description,
            icon,
            Math.Min(currentValue, targetValue),
            targetValue,
            progressPercent,
            isCompleted,
            isCompleted ? "Concluído" : activeMessage);
    }

    private static IReadOnlyCollection<string> BuildMotivationalInsights(
        AnalyticsOverview overview,
        TrendAnalytics trends,
        IReadOnlyCollection<MotivationHabitAtRisk> risks,
        AchievementSet achievements,
        MonthlyChallengeSet challenges)
    {
        var insights = new List<string>();

        if (overview.CurrentOverallStreak > 0)
        {
            insights.Add($"Sua sequência geral tem {overview.CurrentOverallStreak} dias programados.");
        }

        if (trends.Last7Days.CompletionRate > trends.Last30Days.CompletionRate)
        {
            insights.Add("Seu ritmo está melhorando nesta semana.");
        }

        if (risks.Count > 0)
        {
            insights.Add($"{risks.First().Title} precisa de atenção hoje.");
        }

        if (achievements.UnlockedCount > 0)
        {
            var achievementLabel = achievements.UnlockedCount == 1 ? "conquista liberada" : "conquistas liberadas";
            insights.Add($"{achievements.UnlockedCount} {achievementLabel}.");
        }

        var closestChallenge = challenges.Challenges
            .Where(challenge => !challenge.IsCompleted)
            .OrderByDescending(challenge => challenge.ProgressPercent)
            .FirstOrDefault();

        if (closestChallenge is not null)
        {
            insights.Add($"{closestChallenge.Title} está {closestChallenge.ProgressPercent}% completo.");
        }

        if (insights.Count == 0)
        {
            insights.Add("Conclua alguns hábitos programados para ativar seu sistema de motivação.");
        }

        return insights.Take(4).ToList();
    }

    private static IReadOnlyCollection<string> BuildStreakInsights(
        AnalyticsOverview overview,
        TrendAnalytics trends,
        IReadOnlyCollection<MotivationHabitAtRisk> risks,
        IReadOnlyCollection<HabitStreakStatus> streaks)
    {
        var insights = new List<string>();

        if (overview.CurrentOverallStreak > 0)
        {
            insights.Add($"Você está em uma sequência geral de {overview.CurrentOverallStreak} dias.");
        }

        var strongest = streaks
            .Where(streak => streak.CurrentStreak > 0)
            .OrderByDescending(streak => streak.CurrentStreak)
            .FirstOrDefault();

        if (strongest is not null)
        {
            insights.Add($"{strongest.Title} tem sua sequência ativa mais forte.");
        }

        if (risks.Count > 0)
        {
            insights.Add(risks.Count == 1 ? "1 hábito em risco." : $"{risks.Count} hábitos em risco.");
        }

        if (trends.Last7Days.CompletionRate >= 80)
        {
            insights.Add("Esta semana mantém forte a base da sua sequência.");
        }

        if (insights.Count == 0)
        {
            insights.Add("Conclua um hábito programado para iniciar uma nova sequência.");
        }

        return insights.Take(4).ToList();
    }

    private static int CalculateConsistencyScore(AnalyticsOverview overview, TrendAnalytics trends)
    {
        var streakBonus = Math.Min(10, overview.CurrentOverallStreak);
        var weightedScore = (overview.AverageCompletionRate * 0.6m)
            + (trends.Last7Days.CompletionRate * 0.3m)
            + streakBonus;

        return Math.Clamp((int)Math.Round(weightedScore, MidpointRounding.AwayFromZero), 0, 100);
    }

    private static int CalculateLongestPerfectDayStreak(IReadOnlyCollection<CalendarAnalyticsDay> days)
    {
        var longest = 0;
        var current = 0;

        foreach (var day in days.OrderBy(day => day.Date))
        {
            if (day.Status == "none")
            {
                continue;
            }

            if (day.Status == "perfect")
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 0;
            }
        }

        return longest;
    }

    private static int ProgressPercent(int currentValue, int targetValue)
        => targetValue <= 0 ? 0 : Math.Clamp(Percentage(currentValue, targetValue), 0, 100);

    private static int Percentage(int value, int total)
        => total == 0 ? 0 : (int)Math.Round(value * 100m / total, MidpointRounding.AwayFromZero);

    private static int RiskWeight(string riskLevel)
        => riskLevel == "high" ? 2 : 1;
}
