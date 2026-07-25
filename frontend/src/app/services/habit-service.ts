import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  Habit,
  HabitArchiveUpdate,
  HabitCompletionHistory,
  HabitDashboardItem,
  HabitDashboardProgress,
  HabitDashboardResponse,
  HabitRecurrenceUpdate,
  HabitToggleCompletionResponse,
  RecurrenceInfo,
  WeeklyIndicator
} from '../models/habit.model';
import { BaseService } from './base-service';

type ApiHabitDashboardItem = Omit<HabitDashboardItem, 'isCompletedToday' | 'recurrence' | 'recurrenceLabel'> & {
  completedToday: boolean;
};

type ApiHabitDashboardResponse = Omit<HabitDashboardResponse, 'activeHabits'> & {
  activeHabits: ApiHabitDashboardItem[];
};

type ApiHabitToggleCompletionResponse = Omit<HabitToggleCompletionResponse, 'habit'> & {
  habit: ApiHabitDashboardItem;
};

@Injectable({ providedIn: 'root' })
export class HabitService extends BaseService<Habit> {

  private dashboardSubject = new BehaviorSubject<HabitDashboardResponse | null>(null);

  public dashboard$ = this.dashboardSubject.asObservable();
  public habits$ = this.dashboard$.pipe(
    map((dashboard) => dashboard?.activeHabits ?? [])
  );

  constructor(http: HttpClient) {
    super(http, `${environment.apiUrl}/habit`);
  }

  public override refresh(): void { }

  public refreshByUserId(userId: number): void {
    this.getDashboard(userId).subscribe({
      error: (err) => console.error(`Erro ao carregar dashboard de hábitos do usuário ${userId}: `, err)
    });
  }

  public getHabitsByUser(userId: number): Observable<Habit[]> {
    return this.http.get<Habit[]>(`${this.apiUrl}/user/${userId}`);
  }

  public getDashboard(userId: number): Observable<HabitDashboardResponse> {
    return this.http.get<ApiHabitDashboardResponse>(`${this.apiUrl}/user/${userId}/dashboard`).pipe(
      map((dashboard) => this.mapDashboardResponse(dashboard)),
      tap((dashboard) => this.dashboardSubject.next(dashboard))
    );
  }

  public toggleCompletion(habitId: number): Observable<HabitToggleCompletionResponse> {
    const previousDashboard = this.dashboardSubject.value;
    const optimisticDashboard = previousDashboard
      ? this.toggleDashboardHabit(previousDashboard, habitId)
      : null;

    if (optimisticDashboard) {
      this.dashboardSubject.next(optimisticDashboard);
    }

    const headers = new HttpHeaders({
      'X-Skip-Loading': 'true',
      'X-Skip-Notification': 'true'
    });

    return this.http
      .post<ApiHabitToggleCompletionResponse>(`${this.apiUrl}/${habitId}/toggle-completion`, null, { headers })
      .pipe(
        map((result) => this.mapToggleResponse(result)),
        tap((result) => this.applyToggleResult(result)),
        catchError((error) => {
          if (previousDashboard) {
            this.dashboardSubject.next(previousDashboard);
          }

          return throwError(() => error);
        })
      );
  }

  public getHistory(habitId: number): Observable<HabitCompletionHistory> {
    return this.http.get<HabitCompletionHistory>(`${this.apiUrl}/${habitId}/history`);
  }

  public updateRecurrence(habitId: number, recurrence: HabitRecurrenceUpdate): Observable<Habit> {
    return this.http.put<Habit>(`${this.apiUrl}/${habitId}/recurrence`, recurrence);
  }

  public buildRecurrenceConfig(
    recurrenceType: string,
    selectedDays: string[] = [],
    intervalDays = 2,
    monthlyDay = 1
  ): string | null {
    switch (recurrenceType) {
      case 'SpecificDaysOfWeek':
        return JSON.stringify({ daysOfWeek: selectedDays });
      case 'EveryXDays':
        return JSON.stringify({ interval: Math.max(1, intervalDays) });
      case 'Monthly':
        return JSON.stringify({ dayOfMonth: Math.min(31, Math.max(1, monthlyDay)) });
      default:
        return null;
    }
  }

  public getRecurrenceLabel(recurrenceType: string, recurrenceConfig?: string | null): string {
    switch (recurrenceType) {
      case 'SpecificDaysOfWeek':
        return this.getSpecificDaysLabel(recurrenceConfig);
      case 'Weekdays':
        return 'Dias úteis';
      case 'Weekends':
        return 'Fins de semana';
      case 'EveryXDays':
        return this.getEveryXDaysLabel(recurrenceConfig);
      case 'Weekly':
        return 'Semanal';
      case 'Monthly':
        return 'Mensal';
      case 'Custom':
        return 'Personalizada';
      case 'Daily':
      case 'EveryDay':
      default:
        return 'Todos os dias';
    }
  }

  public archiveHabit(habitId: number, isArchived = true): Observable<Habit> {
    const payload: HabitArchiveUpdate = { isArchived };

    return this.http.put<Habit>(`${this.apiUrl}/${habitId}/archive`, payload).pipe(
      tap(() => this.removeHabitFromDashboard(habitId))
    );
  }

  public override add(item: Partial<Habit>): Observable<Habit> {
    return this.http.post<Habit>(this.apiUrl, item);
  }

  public override update(item: Habit): Observable<Habit> {
    return this.http.put<Habit>(`${this.apiUrl}/${item.id}`, item);
  }

  public override delete(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/${id}`).pipe(
      tap((success: boolean) => {
        if (success) {
          this.removeHabitFromDashboard(id);
        }
      })
    );
  }

  private applyToggleResult(result: HabitToggleCompletionResponse): void {
    const dashboard = this.dashboardSubject.value;

    if (!dashboard) {
      return;
    }

    const updatedDashboard = {
      ...dashboard,
      activeHabits: dashboard.activeHabits.map((habit) =>
        habit.id === result.habitId ? result.habit : habit
      )
    };

    this.dashboardSubject.next(this.recalculateDashboard(updatedDashboard));
  }

  private removeHabitFromDashboard(habitId: number): void {
    const dashboard = this.dashboardSubject.value;

    if (!dashboard) {
      return;
    }

    this.dashboardSubject.next(
      this.recalculateDashboard({
        ...dashboard,
        activeHabits: dashboard.activeHabits.filter((habit) => habit.id !== habitId)
      })
    );
  }

  private toggleDashboardHabit(
    dashboard: HabitDashboardResponse,
    habitId: number
  ): HabitDashboardResponse {
    const activeHabits = dashboard.activeHabits.map((habit) => {
      if (habit.id !== habitId) {
        return habit;
      }

      const isCompletedToday = !habit.isCompletedToday;
      const totalCompletions = Math.max(
        0,
        habit.totalCompletions + (isCompletedToday ? 1 : -1)
      );

      return {
        ...habit,
        isCompletedToday,
        currentStreak: isCompletedToday ? Math.max(1, habit.currentStreak) : 0,
        totalCompletions,
        weeklyIndicators: habit.weeklyIndicators.map((indicator) =>
          indicator.date === dashboard.date
            ? { ...indicator, isCompleted: isCompletedToday }
            : indicator
        )
      };
    });

    return this.recalculateDashboard({ ...dashboard, activeHabits });
  }

  private recalculateDashboard(dashboard: HabitDashboardResponse): HabitDashboardResponse {
    const dueToday = dashboard.activeHabits.filter((habit) => habit.isDueToday).length;
    const completedToday = dashboard.activeHabits.filter(
      (habit) => habit.isDueToday && habit.isCompletedToday
    ).length;
    const dailyProgress: HabitDashboardProgress = {
      totalHabits: dashboard.activeHabits.length,
      dueToday,
      completedToday,
      completionRate: dueToday ? Math.round((completedToday / dueToday) * 100) : 0
    };

    return {
      ...dashboard,
      dailyProgress,
      weeklySuccessRate: this.calculateWeeklySuccessRate(dashboard),
      totalCompletions: dashboard.activeHabits.reduce(
        (total, habit) => total + habit.totalCompletions,
        0
      )
    };
  }

  private calculateWeeklySuccessRate(dashboard: HabitDashboardResponse): number {
    const dueIndicators = dashboard.activeHabits.flatMap((habit) =>
      habit.weeklyIndicators.filter((indicator) => indicator.date <= dashboard.date && indicator.isDue)
    );

    if (!dueIndicators.length) {
      return 0;
    }

    const completedIndicators = dueIndicators.filter((indicator) => indicator.isCompleted).length;
    return Math.round((completedIndicators / dueIndicators.length) * 100);
  }

  private mapDashboardResponse(dashboard: ApiHabitDashboardResponse): HabitDashboardResponse {
    return {
      ...dashboard,
      activeHabits: dashboard.activeHabits.map((habit) => this.mapDashboardItem(habit))
    };
  }

  private mapToggleResponse(result: ApiHabitToggleCompletionResponse): HabitToggleCompletionResponse {
    return {
      ...result,
      habit: this.mapDashboardItem(result.habit)
    };
  }

  private mapDashboardItem(habit: ApiHabitDashboardItem): HabitDashboardItem {
    const recurrence = this.getRecurrenceInfo(
      habit.recurrenceType,
      habit.recurrenceConfig,
      habit.reminderTime
    );

    return {
      ...habit,
      reminderEnabled: Boolean(habit.reminderEnabled),
      reminderTimezone: habit.reminderTimezone || 'America/Sao_Paulo',
      reminderMessage: habit.reminderMessage ?? null,
      reminderType: habit.reminderType || 'Standard',
      isCompletedToday: habit.completedToday,
      recurrence,
      recurrenceLabel: recurrence.label
    };
  }

  private getRecurrenceInfo(
    recurrenceType: string,
    recurrenceConfig?: string | null,
    reminderTime?: string | null
  ): RecurrenceInfo {
    const normalizedType = recurrenceType || 'Daily';
    const label = this.getRecurrenceLabel(normalizedType, recurrenceConfig);

    return {
      type: normalizedType,
      config: recurrenceConfig,
      reminderTime,
      label
    };
  }

  private getSpecificDaysLabel(recurrenceConfig?: string | null): string {
    const days = this.extractDaysFromConfig(recurrenceConfig);

    if (!days.length) {
      return 'Dias específicos';
    }

    return days.map((day) => this.translateDayLabel(day)).join(', ');
  }

  private getEveryXDaysLabel(recurrenceConfig?: string | null): string {
    const interval = this.extractIntervalFromConfig(recurrenceConfig);

    if (!interval || interval <= 1) {
      return 'Todos os dias';
    }

    return `A cada ${interval} dias`;
  }

  private extractDaysFromConfig(recurrenceConfig?: string | null): string[] {
    if (!recurrenceConfig) {
      return [];
    }

    try {
      const config = JSON.parse(recurrenceConfig) as Record<string, unknown>;
      const days = config['daysOfWeek'] ?? config['weekDays'] ?? config['days'];

      if (Array.isArray(days)) {
        return days.map((day) => this.normalizeDayLabel(day)).filter(Boolean);
      }
    } catch {
      return recurrenceConfig
        .split(/[,;|\s]+/)
        .map((day) => this.normalizeDayLabel(day))
        .filter(Boolean);
    }

    return [];
  }

  private extractIntervalFromConfig(recurrenceConfig?: string | null): number | null {
    if (!recurrenceConfig) {
      return null;
    }

    try {
      const config = JSON.parse(recurrenceConfig) as Record<string, unknown>;
      const interval = config['interval'] ?? config['everyXDays'] ?? config['days'];
      return typeof interval === 'number' ? interval : Number(interval) || null;
    } catch {
      return Number(recurrenceConfig) || null;
    }
  }

  private normalizeDayLabel(value: unknown): string {
    const dayMap = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

    if (typeof value === 'number' && value >= 0 && value <= 6) {
      return dayMap[value];
    }

    if (typeof value !== 'string') {
      return '';
    }

    const normalized = value.trim().toLowerCase();

    if (!normalized) {
      return '';
    }

    return dayMap.find((day) => day.toLowerCase().startsWith(normalized.slice(0, 3))) ?? '';
  }

  private translateDayLabel(dayOfWeek: string): string {
    const labels: Record<string, string> = {
      Sunday: 'Dom',
      Monday: 'Seg',
      Tuesday: 'Ter',
      Wednesday: 'Qua',
      Thursday: 'Qui',
      Friday: 'Sex',
      Saturday: 'Sáb',
    };

    return labels[dayOfWeek] ?? dayOfWeek;
  }
}
