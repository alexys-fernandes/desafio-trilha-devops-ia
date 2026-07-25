import { Base } from './base-model';

export type ReminderType = 'Standard' | 'StreakProtection' | 'Motivation' | 'Custom';

export interface NotificationPreference extends Base {
  userId: number;
  notificationsEnabled: boolean;
  quietHoursStart?: string | null;
  quietHoursEnd?: string | null;
  reminderSoundEnabled: boolean;
  motivationalNotificationsEnabled: boolean;
  achievementNotificationsEnabled: boolean;
  streakRiskNotificationsEnabled: boolean;
  defaultReminderTime?: string | null;
  defaultReminderType: ReminderType;
}

export interface HabitReminderCandidate {
  habitId: number;
  title: string;
  icon: string;
  color: string;
  category: string;
  scheduledDate: string;
  reminderTime: string;
  scheduledAt: string;
  timezone: string;
  reminderType: ReminderType;
  message: string;
  isCompleted: boolean;
  isSuppressedByQuietHours: boolean;
}

export interface MissedHabitReminder {
  habitId: number;
  title: string;
  icon: string;
  color: string;
  category: string;
  reminderTime?: string | null;
  currentStreak: number;
  message: string;
}

export interface HabitStreakRiskReminder {
  habitId: number;
  title: string;
  icon: string;
  color: string;
  category: string;
  currentStreak: number;
  longestStreak: number;
  daysUntilPersonalRecord?: number | null;
  riskLevel: 'high' | 'medium';
  message: string;
}

export interface NotificationPayload {
  id: string;
  notificationType: string;
  title: string;
  body: string;
  priority: 'high' | 'normal' | 'low';
  scheduledFor: string;
  timezone: string;
  habitId?: number | null;
  groupKey?: string | null;
  metadata: Record<string, string>;
}

export interface ReminderDashboard {
  userId: number;
  date: string;
  generatedAt: string;
  preferences: NotificationPreference;
  nextReminder?: HabitReminderCandidate | null;
  upcomingReminders: HabitReminderCandidate[];
  missedHabits: MissedHabitReminder[];
  habitsAtRisk: HabitStreakRiskReminder[];
  smartMotivations: string[];
  payloads: NotificationPayload[];
}
