import { Component, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, combineLatest } from 'rxjs';
import { map, startWith, take } from 'rxjs/operators';
import { Habit, HabitDashboardItem, HabitDashboardResponse } from '../../../models/habit.model';
import { ReminderDashboard } from '../../../models/notification.model';
import { UserResponse } from '../../../models/user-responde.model';
import { HabitDialog } from '../dialogs/habit-dialog/habit-dialog';
import { HabitService } from '../../../services/habit-service';
import { LoadingService } from '../../../services/loading-service';
import { NotificationService } from '../../../services/notification-service';
import { UserService } from '../../../services/user-service';
import { ConfirmDialog } from '../dialogs/confirm-dialog/confirm-dialog';

interface HabitsViewModel {
  habits: HabitDashboardItem[];
  activeHabits: number;
  dueToday: number;
  completedToday: number;
  completionPercent: number;
  currentStreak: number;
  longestStreak: number;
  successRate: number;
  totalCompletions: number;
  dashboardDate: string;
  motivationalMessage: string;
  nextReminderLabel: string;
  attentionCount: number;
  attentionMessage: string;
  smartReminderMessage: string;
  hasReminderSummary: boolean;
}

@Component({
  selector: 'app-habits',
  templateUrl: './habits.html',
  styleUrl: './habits.scss',
  standalone: false
})
export class Habits implements OnInit {
  greeting = this.buildGreeting(new Date());
  view$!: Observable<HabitsViewModel>;
  user$!: Observable<UserResponse | null>;
  loading$!: Observable<boolean>;
  filterControl = new FormControl('', { nonNullable: true });

  constructor(
    private habitService: HabitService,
    private userService: UserService,
    private dialog: MatDialog,
    private loadingService: LoadingService,
    private notificationService: NotificationService,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    this.loading$ = this.loadingService.loading$;
    this.user$ = this.userService.getUser();

    this.triggerRefresh();

    this.view$ = combineLatest([
      this.habitService.dashboard$,
      this.notificationService.dashboard$,
      this.filterControl.valueChanges.pipe(startWith(''))
    ]).pipe(
      map(([dashboard, reminderDashboard, filterValue]) => {
        const search = filterValue.toLowerCase().trim();
        const habits = dashboard?.activeHabits ?? [];
        const filteredHabits = search
          ? habits.filter(habit => habit.title.toLowerCase().includes(search))
          : habits;

        return this.createViewModel(dashboard, filteredHabits, reminderDashboard);
      })
    );
  }

  getFirstName(name?: string): string {
    return name?.trim().split(/\s+/)[0] || 'Victor';
  }

  isHabitCompletedToday(habit: HabitDashboardItem): boolean {
    return habit.isCompletedToday;
  }

  openNewHabitDialog(habit?: HabitDashboardItem, event?: Event): void {
    event?.stopPropagation();

    const dialogRef = this.dialog.open(HabitDialog, {
      width: '450px',
      data: habit ? { ...habit } : null,
      panelClass: 'custom-dialog-container'
    });

    dialogRef.afterClosed().subscribe((result?: Partial<Habit>) => {
      if (result) this.handleSave(result);
    });
  }

  handleSave(habit: Partial<Habit>): void {
    if (habit.id) {
      this.habitService.update(habit as Habit).subscribe({
        next: () => this.triggerRefresh()
      });
    } else {
      this.userService.getUser().pipe(take(1)).subscribe({
        next: (user) => {
          if (user && user.id) {
            const payload = { ...habit, userId: user.id };
            this.habitService.add(payload).subscribe({
              next: () => this.triggerRefresh()
            });
          } else {
            console.error('Não foi possível criar o hábito: usuário não identificado.');
          }
        }
      });
    }
  }

  toggleToday(habit: HabitDashboardItem, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();

    this.habitService.toggleCompletion(habit.id).subscribe({
      next: () => this.triggerReminderRefresh(),
      error: () => {
        this.snackBar.open('Não foi possível atualizar este hábito. Seu progresso foi restaurado.', 'Fechar', {
          duration: 4000,
          horizontalPosition: 'center',
          verticalPosition: 'bottom',
          panelClass: ['snackbar-error']
        });
      }
    });
  }

  deleteHabit(habit: HabitDashboardItem, event?: Event): void {
    event?.stopPropagation();

    const dialogRef = this.dialog.open(ConfirmDialog, {
      width: '400px',
      panelClass: 'custom-dialog-container',
      data: {
        title: 'Excluir hábito',
        message: `Excluir <strong>"${habit.title}"</strong>?<br />Esta ação não pode ser desfeita.`,
        confirmBtnText: 'Excluir'
      }
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.habitService.delete(habit.id).subscribe({
          next: () => this.triggerRefresh()
        });
      }
    });
  }

  archiveHabit(habit: HabitDashboardItem, event?: Event): void {
    event?.stopPropagation();

    this.habitService.archiveHabit(habit.id, true).subscribe();
  }

  getWeekdayLabel(dayOfWeek: string): string {
    return this.translateWeekday(dayOfWeek).slice(0, 3);
  }

  isDashboardDate(indicatorDate: string, dashboardDate: string): boolean {
    return indicatorDate === dashboardDate;
  }

  private createViewModel(
    dashboard: HabitDashboardResponse | null,
    habits: HabitDashboardItem[],
    reminderDashboard: ReminderDashboard | null
  ): HabitsViewModel {
    const activeHabits = dashboard?.dailyProgress.totalHabits ?? 0;
    const dueToday = dashboard?.dailyProgress.dueToday ?? 0;
    const completedToday = dashboard?.dailyProgress.completedToday ?? 0;
    const completionPercent = dashboard?.dailyProgress.completionRate ?? 0;
    const successRate = dashboard?.weeklySuccessRate ?? 0;
    const totalCompletions = dashboard?.totalCompletions ?? 0;
    const currentStreak = Math.max(0, ...(dashboard?.activeHabits.map(habit => habit.currentStreak) ?? []));
    const longestStreak = Math.max(0, ...(dashboard?.activeHabits.map(habit => habit.longestStreak) ?? []));

    const nextReminderLabel = this.getNextReminderLabel(reminderDashboard);
    const attentionCount = reminderDashboard?.habitsAtRisk.length ?? 0;
    const attentionMessage = reminderDashboard?.habitsAtRisk[0]?.message ?? '';
    const smartReminderMessage = reminderDashboard?.smartMotivations[0] ?? '';

    return {
      habits,
      activeHabits,
      dueToday,
      completedToday,
      completionPercent,
      currentStreak,
      longestStreak,
      successRate,
      totalCompletions,
      dashboardDate: dashboard?.date ?? '',
      motivationalMessage: this.getMotivationalMessage(activeHabits, completedToday),
      nextReminderLabel,
      attentionCount,
      attentionMessage,
      smartReminderMessage,
      hasReminderSummary: Boolean(nextReminderLabel || attentionCount || smartReminderMessage)
    };
  }

  private getNextReminderLabel(reminderDashboard: ReminderDashboard | null): string {
    if (!reminderDashboard?.nextReminder) {
      return '';
    }

    const time = reminderDashboard.nextReminder.reminderTime.slice(0, 5);
    return `${time} · ${reminderDashboard.nextReminder.title}`;
  }

  private getMotivationalMessage(activeHabits: number, completedToday: number): string {
    if (!activeHabits) {
      return 'Crie seu primeiro hábito e comece uma nova sequência hoje.';
    }

    if (completedToday === activeHabits) {
      return 'Dia perfeito. Sua sequência está protegida.';
    }

    if (!completedToday) {
      return 'Comece com uma pequena vitória. Toque em qualquer hábito para concluir.';
    }

    const remaining = activeHabits - completedToday;
    const label = remaining === 1 ? 'hábito' : 'hábitos';

    return `${remaining} ${label} restantes para um dia perfeito.`;
  }

  private buildGreeting(date: Date): string {
    const hour = date.getHours();

    if (hour < 12) {
      return 'Bom dia';
    }

    if (hour < 18) {
      return 'Boa tarde';
    }

    return 'Boa noite';
  }

  private translateWeekday(dayOfWeek: string): string {
    const labels: Record<string, string> = {
      Sunday: 'Domingo',
      Monday: 'Segunda',
      Tuesday: 'Terça',
      Wednesday: 'Quarta',
      Thursday: 'Quinta',
      Friday: 'Sexta',
      Saturday: 'Sábado',
    };

    return labels[dayOfWeek] ?? dayOfWeek;
  }

  private triggerRefresh(): void {
    this.userService.getUser().pipe(take(1)).subscribe((user) => {
      if (user && user.id) {
        this.habitService.refreshByUserId(user.id);
        this.notificationService.refreshByUserId(user.id);
      }
    });
  }

  private triggerReminderRefresh(): void {
    this.userService.getUser().pipe(take(1)).subscribe((user) => {
      if (user?.id) {
        this.notificationService.refreshByUserId(user.id);
      }
    });
  }
}
