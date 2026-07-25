import { Base } from "./base-model";

export interface Habit extends Base {
  title: string;
  icon: string;
  color: string;
  category: string;
  recurrenceType: string;
  recurrenceConfig?: string | null;
  reminderEnabled: boolean;
  reminderTime?: string | null;
  reminderTimezone: string;
  reminderMessage?: string | null;
  reminderType: string;
  isArchived: boolean;
  userId: number;
}

export interface RecurrenceInfo {
  type: string;
  config?: string | null;
  reminderTime?: string | null;
  label: string;
}

export interface WeeklyIndicator {
  date: string;
  dayOfWeek: string;
  isDue: boolean;
  isCompleted: boolean;
}

export interface HabitStats {
  currentStreak: number;
  longestStreak: number;
  weeklySuccessRate: number;
  totalCompletions: number;
}

export interface HabitDashboardProgress {
  totalHabits: number;
  dueToday: number;
  completedToday: number;
  completionRate: number;
}

export interface HabitDashboardItem {
  id: number;
  userId: number;
  title: string;
  icon: string;
  color: string;
  category: string;
  recurrenceType: string;
  recurrenceConfig?: string | null;
  reminderEnabled: boolean;
  reminderTime?: string | null;
  reminderTimezone: string;
  reminderMessage?: string | null;
  reminderType: string;
  recurrenceLabel: string;
  recurrence: RecurrenceInfo;
  isArchived: boolean;
  isDueToday: boolean;
  isCompletedToday: boolean;
  currentStreak: number;
  longestStreak: number;
  weeklySuccessRate: number;
  totalCompletions: number;
  weeklyIndicators: WeeklyIndicator[];
}

export interface HabitDashboardResponse {
  userId: number;
  date: string;
  activeHabits: HabitDashboardItem[];
  dailyProgress: HabitDashboardProgress;
  weeklySuccessRate: number;
  totalCompletions: number;
}

export interface HabitCompletion {
  id: number;
  habitId: number;
  userId: number;
  completedDate: string;
  completedAt: string;
  createdAt: string;
  modifiedAt?: string | null;
  isDeleted: boolean;
}

export interface HabitCompletionHistory {
  habitId: number;
  userId: number;
  title: string;
  recurrenceType: string;
  recurrenceConfig?: string | null;
  stats: HabitStats;
  weeklyIndicators: WeeklyIndicator[];
  completions: HabitCompletion[];
}

export interface HabitToggleCompletionResponse {
  habitId: number;
  completedDate: string;
  completedToday: boolean;
  completedAt?: string | null;
  habit: HabitDashboardItem;
}

export interface HabitRecurrenceUpdate {
  recurrenceType: string;
  recurrenceConfig?: string | null;
  reminderEnabled?: boolean | null;
  reminderTime?: string | null;
  reminderTimezone?: string | null;
  reminderMessage?: string | null;
  reminderType?: string | null;
}

export interface HabitArchiveUpdate {
  isArchived: boolean;
}

export type HabitRecurrenceType =
  | 'Daily'
  | 'EveryDay'
  | 'Weekdays'
  | 'Weekends'
  | 'SpecificDaysOfWeek'
  | 'EveryXDays'
  | 'Weekly'
  | 'Monthly'
  | 'Custom';

export interface RecurrencePattern {
  key: string;
  label: string;
  recurrenceType: HabitRecurrenceType;
  recurrenceConfig?: string | null;
  habits: Habit[];
}
