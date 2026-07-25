import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { catchError, finalize, switchMap, take } from 'rxjs/operators';
import {
  NotificationPreference,
  ReminderDashboard,
  ReminderType
} from '../../../models/notification.model';
import { NotificationService } from '../../../services/notification-service';
import { UserService } from '../../../services/user-service';

interface NotificationSettingsView {
  preferences: NotificationPreference;
  dashboard: ReminderDashboard;
}

@Component({
  selector: 'app-notifications',
  templateUrl: './notifications.html',
  styleUrl: './notifications.scss',
  standalone: false,
})
export class Notifications implements OnInit {
  view: NotificationSettingsView | null = null;
  formData: Partial<NotificationPreference> = {};
  loading = false;
  saving = false;
  errorMessage = '';
  private userId: number | null = null;

  reminderTypes: { value: ReminderType; label: string }[] = [
    { value: 'Standard', label: 'Padrão' },
    { value: 'StreakProtection', label: 'Proteção de sequência' },
    { value: 'Motivation', label: 'Motivação' },
    { value: 'Custom', label: 'Personalizado' },
  ];

  constructor(
    private notificationService: NotificationService,
    private userService: UserService,
    private snackBar: MatSnackBar,
    private changeDetectorRef: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading = true;
    this.errorMessage = '';

    this.userService.getUser().pipe(
      take(1),
      switchMap((user) => {
        if (!user?.id) {
          return of(null);
        }

        this.userId = user.id;

        return this.notificationService.getDashboard(user.id).pipe(take(1));
      }),
      catchError(() => {
        this.errorMessage = 'Não foi possível carregar as configurações de notificação.';
        return of(null);
      }),
      finalize(() => {
        this.loading = false;
        this.changeDetectorRef.markForCheck();
      }),
    ).subscribe((result) => {
      this.loading = false;

      if (!result) {
        return;
      }

      this.view = {
        preferences: result.preferences,
        dashboard: result,
      };
      this.formData = this.toFormData(result.preferences);
      this.changeDetectorRef.markForCheck();
    });
  }

  saveSettings(): void {
    if (!this.userId) {
      return;
    }

    this.saving = true;

    this.notificationService.updatePreferences(this.userId, this.toApiPayload()).pipe(
      take(1),
      switchMap((preferences) =>
        this.notificationService.getDashboard(this.userId as number).pipe(
          take(1),
          switchMap((dashboard) => of({ preferences, dashboard }))
        )
      ),
      catchError(() => {
        this.snackBar.open('Não foi possível salvar as configurações de notificação.', 'Fechar', {
          duration: 4000,
          panelClass: ['snackbar-error'],
        });
        return of(null);
      }),
      finalize(() => {
        this.saving = false;
        this.changeDetectorRef.markForCheck();
      }),
    ).subscribe((result) => {
      this.saving = false;

      if (!result) {
        return;
      }

      this.view = result;
      this.formData = this.toFormData(result.preferences);
      this.snackBar.open('Configurações de notificação salvas.', 'Fechar', {
        duration: 3000,
        panelClass: ['snackbar-success'],
      });
      this.changeDetectorRef.markForCheck();
    });
  }

  formatTime(value?: string | null): string {
    if (!value) {
      return 'Não definido';
    }

    return value.slice(0, 5);
  }

  getNextReminderLabel(dashboard: ReminderDashboard): string {
    if (!dashboard.nextReminder) {
      return 'Nenhum lembrete programado';
    }

    return `${this.formatTime(dashboard.nextReminder.reminderTime)} · ${dashboard.nextReminder.title}`;
  }

  private toFormData(preferences: NotificationPreference): Partial<NotificationPreference> {
    return {
      ...preferences,
      quietHoursStart: this.toInputTime(preferences.quietHoursStart),
      quietHoursEnd: this.toInputTime(preferences.quietHoursEnd),
      defaultReminderTime: this.toInputTime(preferences.defaultReminderTime),
    };
  }

  private toApiPayload(): Partial<NotificationPreference> {
    return {
      ...this.formData,
      quietHoursStart: this.toApiTime(this.formData.quietHoursStart),
      quietHoursEnd: this.toApiTime(this.formData.quietHoursEnd),
      defaultReminderTime: this.toApiTime(this.formData.defaultReminderTime),
    };
  }

  private toInputTime(value?: string | null): string | null {
    return value ? value.slice(0, 5) : null;
  }

  private toApiTime(value?: string | null): string | null {
    if (!value) {
      return null;
    }

    return value.length === 5 ? `${value}:00` : value;
  }
}
