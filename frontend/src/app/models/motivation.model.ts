export interface AchievementProgress {
  id: string;
  title: string;
  description: string;
  icon: string;
  category: string;
  currentValue: number;
  targetValue: number;
  progressPercent: number;
  isUnlocked: boolean;
  message: string;
}

export interface AchievementSet {
  userId: number;
  date: string;
  consistencyScore: number;
  unlockedCount: number;
  totalCount: number;
  achievements: AchievementProgress[];
}

export interface MonthlyChallenge {
  id: string;
  title: string;
  description: string;
  icon: string;
  currentValue: number;
  targetValue: number;
  progressPercent: number;
  isCompleted: boolean;
  message: string;
}

export interface MonthlyChallengeSet {
  userId: number;
  monthLabel: string;
  startDate: string;
  endDate: string;
  challenges: MonthlyChallenge[];
}

export interface MotivationHabitAtRisk {
  habitId: number;
  title: string;
  icon: string;
  color: string;
  category: string;
  currentStreak: number;
  lastCompletedDate?: string | null;
  nextScheduledDate?: string | null;
  missedScheduledDatesCount: number;
  riskLevel: 'high' | 'medium';
  message: string;
}

export interface HabitStreakStatus {
  habitId: number;
  title: string;
  icon: string;
  color: string;
  category: string;
  currentStreak: number;
  longestStreak: number;
  completionRate: number;
  totalCompletions: number;
  lastCompletedDate?: string | null;
  status: 'protected' | 'at-risk' | 'new' | 'rebuilding';
  message: string;
}

export interface StreakCenter {
  userId: number;
  date: string;
  consistencyScore: number;
  currentOverallStreak: number;
  longestOverallStreak: number;
  habitStreaks: HabitStreakStatus[];
  habitsAtRisk: MotivationHabitAtRisk[];
  motivationalInsights: string[];
}

export interface MotivationSummary {
  userId: number;
  date: string;
  consistencyScore: number;
  currentOverallStreak: number;
  longestOverallStreak: number;
  unlockedAchievements: number;
  totalAchievements: number;
  activeMonthlyChallenges: number;
  mostAtRiskHabit?: MotivationHabitAtRisk | null;
  motivationalInsights: string[];
}
