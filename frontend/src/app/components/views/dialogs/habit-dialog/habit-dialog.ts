import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Habit, HabitDashboardItem } from '../../../../models/habit.model';
import { HabitService } from '../../../../services/habit-service';

@Component({
  selector: 'app-habit-dialog',
  templateUrl: './habit-dialog.html',
  styleUrl: './habit-dialog.scss',
  standalone: false
})
export class HabitDialog implements OnInit {
  formData: Partial<Habit> = {
    title: '',
    icon: '',
    recurrenceType: 'Daily',
    reminderEnabled: false,
    reminderTime: '09:00',
    reminderTimezone: 'America/Sao_Paulo',
    reminderType: 'Standard'
  };

  recurrenceOptions = [
    { value: 'Daily', label: 'Todos os dias' },
    { value: 'Weekdays', label: 'Dias úteis' },
    { value: 'Weekends', label: 'Fins de semana' },
    { value: 'SpecificDaysOfWeek', label: 'Dias específicos da semana' },
    { value: 'EveryXDays', label: 'A cada X dias' },
    { value: 'Monthly', label: 'Mensal' },
  ];
  weekDays = [
    { value: 'Monday', label: 'Seg' },
    { value: 'Tuesday', label: 'Ter' },
    { value: 'Wednesday', label: 'Qua' },
    { value: 'Thursday', label: 'Qui' },
    { value: 'Friday', label: 'Sex' },
    { value: 'Saturday', label: 'Sáb' },
    { value: 'Sunday', label: 'Dom' },
  ];
  selectedWeekDays: string[] = [];
  intervalDays = 2;
  monthlyDay = 1;
  reminderTypes = [
    { value: 'Standard', label: 'Padrão' },
    { value: 'StreakProtection', label: 'Proteção de sequência' },
    { value: 'Motivation', label: 'Motivação' },
    { value: 'Custom', label: 'Personalizado' },
  ];

  iconSuggestions: string[] = [
    '💧', '🏃', '📚', '🧘', '🍎', '💪', '💊', '🛌', '🚭', '🌱', '🎨', '💻'
  ];

  constructor(
    public dialogRef: MatDialogRef<HabitDialog>,
    @Inject(MAT_DIALOG_DATA) public data: Partial<Habit> | HabitDashboardItem | null,
    private habitService: HabitService
  ) { }

  ngOnInit(): void {
    if (this.data) {
      this.formData = { ...this.data };
    }

    this.formData.recurrenceType = this.normalizeRecurrenceType(this.formData.recurrenceType);
    this.formData.reminderEnabled = Boolean(this.formData.reminderEnabled);
    this.formData.reminderTime = this.toInputTime(this.formData.reminderTime) ?? '09:00';
    this.formData.reminderTimezone = this.formData.reminderTimezone || 'America/Sao_Paulo';
    this.formData.reminderType = this.normalizeReminderType(this.formData.reminderType);
    this.hydrateRecurrenceControls();
  }

  selectIcon(icon: string): void {
    this.formData.icon = icon;
  }

  onRecurrenceTypeChange(): void {
    if (this.formData.recurrenceType === 'SpecificDaysOfWeek' && !this.selectedWeekDays.length) {
      this.selectedWeekDays = ['Monday', 'Wednesday', 'Friday'];
    }
  }

  onReminderEnabledChange(): void {
    if (this.formData.reminderEnabled && !this.formData.reminderTime) {
      this.formData.reminderTime = '09:00';
    }
  }

  toggleWeekDay(day: string): void {
    this.selectedWeekDays = this.isWeekDaySelected(day)
      ? this.selectedWeekDays.filter((selectedDay) => selectedDay !== day)
      : [...this.selectedWeekDays, day];
  }

  isWeekDaySelected(day: string): boolean {
    return this.selectedWeekDays.includes(day);
  }

  onSave(): void {
    if (this.formData.title && this.formData.icon) {
      this.formData.recurrenceType = this.normalizeRecurrenceType(this.formData.recurrenceType);
      this.formData.recurrenceConfig = this.habitService.buildRecurrenceConfig(
        this.formData.recurrenceType,
        this.selectedWeekDays,
        this.intervalDays,
        this.monthlyDay
      );
      this.formData.reminderType = this.normalizeReminderType(this.formData.reminderType);
      this.formData.reminderTimezone = this.formData.reminderTimezone || 'America/Sao_Paulo';

      if (this.formData.reminderEnabled) {
        this.formData.reminderTime = this.toApiTime(this.formData.reminderTime) ?? '09:00:00';
        this.formData.reminderMessage = this.formData.reminderMessage?.trim() || null;
      } else {
        this.formData.reminderTime = null;
        this.formData.reminderMessage = null;
      }

      this.dialogRef.close(this.formData);
    }
  }

  onClose(): void {
    this.dialogRef.close();
  }

  private hydrateRecurrenceControls(): void {
    const config = this.parseRecurrenceConfig();

    if (Array.isArray(config['daysOfWeek'])) {
      this.selectedWeekDays = config['daysOfWeek']
        .filter((day): day is string => typeof day === 'string');
    }

    const interval = Number(config['interval'] ?? config['everyXDays']);
    if (Number.isFinite(interval) && interval > 0) {
      this.intervalDays = interval;
    }

    const dayOfMonth = Number(config['dayOfMonth'] ?? config['day']);
    if (Number.isFinite(dayOfMonth) && dayOfMonth > 0) {
      this.monthlyDay = Math.min(31, dayOfMonth);
    }
  }

  private parseRecurrenceConfig(): Record<string, unknown> {
    if (!this.formData.recurrenceConfig) {
      return {};
    }

    try {
      return JSON.parse(this.formData.recurrenceConfig) as Record<string, unknown>;
    } catch {
      return {};
    }
  }

  private normalizeRecurrenceType(recurrenceType?: string | null): string {
    return recurrenceType === 'EveryDay' || !recurrenceType ? 'Daily' : recurrenceType;
  }

  private normalizeReminderType(reminderType?: string | null): string {
    const supportedTypes = this.reminderTypes.map((option) => option.value);
    return reminderType && supportedTypes.includes(reminderType) ? reminderType : 'Standard';
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
