export interface AnalyticsHabitSummary {
  habitId: number;
  title: string;
  icon: string;
  color: string;
  category: string;
  completionRate: number;
  currentStreak: number;
  longestStreak: number;
  totalCompletions: number;
}

export interface AnalyticsWeekdayStat {
  dayOfWeek: string;
  scheduledHabits: number;
  completedHabits: number;
  completionRate: number;
}

export interface AnalyticsTrendDay {
  date: string;
  scheduledHabits: number;
  completedHabits: number;
  completionRate: number;
}

export interface AnalyticsTrendWindow {
  label: string;
  startDate: string;
  endDate: string;
  scheduledHabits: number;
  completedHabits: number;
  completionRate: number;
  dailyBreakdown: AnalyticsTrendDay[];
}

export interface AnalyticsOverview {
  userId: number;
  date: string;
  totalActiveHabits: number;
  totalCompletions: number;
  averageCompletionRate: number;
  bestHabit?: AnalyticsHabitSummary | null;
  weakestHabit?: AnalyticsHabitSummary | null;
  bestDayOfWeek?: AnalyticsWeekdayStat | null;
  weakestDayOfWeek?: AnalyticsWeekdayStat | null;
  currentOverallStreak: number;
  longestOverallStreak: number;
  weeklyCompletionTrend: AnalyticsTrendDay[];
  monthlyCompletionTrend: AnalyticsTrendDay[];
}

export interface HabitAnalytics {
  userId: number;
  habitId: number;
  title: string;
  icon: string;
  color: string;
  category: string;
  currentStreak: number;
  longestStreak: number;
  totalCompletions: number;
  completionRate: number;
  bestWeekday?: AnalyticsWeekdayStat | null;
  weakestWeekday?: AnalyticsWeekdayStat | null;
  lastCompletedDate?: string | null;
  missedScheduledDates: string[];
  weeklyTrend: AnalyticsTrendDay[];
  monthlyTrend: AnalyticsTrendDay[];
}

export interface CalendarAnalyticsDay {
  date: string;
  scheduledCount: number;
  completedCount: number;
  completionRate: number;
  status: 'perfect' | 'partial' | 'missed' | 'none';
}

export interface CalendarAnalytics {
  userId: number;
  startDate: string;
  endDate: string;
  days: CalendarAnalyticsDay[];
}

export interface TrendAnalytics {
  userId: number;
  date: string;
  last7Days: AnalyticsTrendWindow;
  last30Days: AnalyticsTrendWindow;
  last90Days: AnalyticsTrendWindow;
}
