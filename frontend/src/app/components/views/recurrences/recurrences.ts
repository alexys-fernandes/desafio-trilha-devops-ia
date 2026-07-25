import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin, Observable, of } from 'rxjs';
import { catchError, switchMap, take } from 'rxjs/operators';
import { Habit, RecurrencePattern } from '../../../models/habit.model';
import { HabitService } from '../../../services/habit-service';
import { UserService } from '../../../services/user-service';

interface RecurrenceEditorState {
  recurrenceType: string;
  selectedWeekDays: string[];
  intervalDays: number;
  monthlyDay: number;
}

@Component({
  selector: 'app-recurrences',
  templateUrl: './recurrences.html',
  styleUrl: './recurrences.scss',
  standalone: false,
})
export class Recurrences implements OnInit {
  habits: Habit[] = [];
  patterns: RecurrencePattern[] = [];
  selectedHabitIds = new Set<number>();
  editingPatternKey: string | null = null;
  bulkEditor: RecurrenceEditorState = this.createDefaultEditor();
  patternEditor: RecurrenceEditorState = this.createDefaultEditor();
  loading = false;

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

  constructor(
    private habitService: HabitService,
    private userService: UserService,
    private snackBar: MatSnackBar,
  ) { }

  ngOnInit(): void {
    this.loadRecurrences();
  }

  loadRecurrences(): void {
    this.loading = true;
    this.userService.getUser().pipe(
      take(1),
      switchMap((user) => user?.id ? this.habitService.getHabitsByUser(user.id) : of([])),
      catchError(() => {
        this.snackBar.open('Não foi possível carregar recorrências.', 'Fechar', {
          duration: 4000,
          panelClass: ['snackbar-error'],
        });
        return of([]);
      }),
    ).subscribe((habits) => {
      this.habits = habits.filter((habit) => !habit.isArchived);
      this.patterns = this.groupHabitsByRecurrence(this.habits);
      this.loading = false;
    });
  }

  toggleHabitSelection(habitId: number): void {
    if (this.selectedHabitIds.has(habitId)) {
      this.selectedHabitIds.delete(habitId);
    } else {
      this.selectedHabitIds.add(habitId);
    }
  }

  isHabitSelected(habitId: number): boolean {
    return this.selectedHabitIds.has(habitId);
  }

  editPattern(pattern: RecurrencePattern): void {
    this.editingPatternKey = pattern.key;
    this.patternEditor = this.createEditorFromPattern(pattern);
  }

  cancelPatternEdit(): void {
    this.editingPatternKey = null;
  }

  applyToSelected(): void {
    this.applyRecurrenceToHabits(Array.from(this.selectedHabitIds), this.bulkEditor);
  }

  applyToPattern(pattern: RecurrencePattern): void {
    this.applyRecurrenceToHabits(pattern.habits.map((habit) => habit.id), this.patternEditor);
  }

  toggleEditorDay(editor: RecurrenceEditorState, day: string): void {
    editor.selectedWeekDays = editor.selectedWeekDays.includes(day)
      ? editor.selectedWeekDays.filter((selectedDay) => selectedDay !== day)
      : [...editor.selectedWeekDays, day];
  }

  isEditorDaySelected(editor: RecurrenceEditorState, day: string): boolean {
    return editor.selectedWeekDays.includes(day);
  }

  ensureSpecificDays(editor: RecurrenceEditorState): void {
    if (editor.recurrenceType === 'SpecificDaysOfWeek' && !editor.selectedWeekDays.length) {
      editor.selectedWeekDays = ['Monday', 'Wednesday', 'Friday'];
    }
  }

  private applyRecurrenceToHabits(habitIds: number[], editor: RecurrenceEditorState): void {
    if (!habitIds.length) {
      return;
    }

    const recurrence = {
      recurrenceType: editor.recurrenceType,
      recurrenceConfig: this.habitService.buildRecurrenceConfig(
        editor.recurrenceType,
        editor.selectedWeekDays,
        editor.intervalDays,
        editor.monthlyDay,
      ),
      reminderTime: null,
    };
    const requests: Observable<Habit>[] = habitIds.map((habitId) =>
      this.habitService.updateRecurrence(habitId, recurrence)
    );

    forkJoin(requests).subscribe({
      next: () => {
        this.selectedHabitIds.clear();
        this.editingPatternKey = null;
        this.loadRecurrences();
        this.snackBar.open('Recorrência atualizada.', 'Fechar', {
          duration: 3000,
          panelClass: ['snackbar-success'],
        });
      },
      error: () => {
        this.snackBar.open('Não foi possível atualizar a recorrência.', 'Fechar', {
          duration: 4000,
          panelClass: ['snackbar-error'],
        });
      },
    });
  }

  private groupHabitsByRecurrence(habits: Habit[]): RecurrencePattern[] {
    const groups = new Map<string, RecurrencePattern>();

    habits.forEach((habit) => {
      const recurrenceType = this.normalizeRecurrenceType(habit.recurrenceType);
      const recurrenceConfig = habit.recurrenceConfig ?? null;
      const key = `${recurrenceType}:${recurrenceConfig ?? ''}`;
      const label = this.habitService.getRecurrenceLabel(recurrenceType, recurrenceConfig);

      if (!groups.has(key)) {
        groups.set(key, {
          key,
          label,
          recurrenceType,
          recurrenceConfig,
          habits: [],
        });
      }

      groups.get(key)?.habits.push(habit);
    });

    return Array.from(groups.values()).sort((left, right) => left.label.localeCompare(right.label));
  }

  private createEditorFromPattern(pattern: RecurrencePattern): RecurrenceEditorState {
    const editor = this.createDefaultEditor();
    editor.recurrenceType = pattern.recurrenceType;

    try {
      const config = pattern.recurrenceConfig
        ? JSON.parse(pattern.recurrenceConfig) as Record<string, unknown>
        : {};

      if (Array.isArray(config['daysOfWeek'])) {
        editor.selectedWeekDays = config['daysOfWeek']
          .filter((day): day is string => typeof day === 'string');
      }

      const interval = Number(config['interval']);
      if (Number.isFinite(interval) && interval > 0) {
        editor.intervalDays = interval;
      }

      const dayOfMonth = Number(config['dayOfMonth']);
      if (Number.isFinite(dayOfMonth) && dayOfMonth > 0) {
        editor.monthlyDay = Math.min(31, dayOfMonth);
      }
    } catch {
      return editor;
    }

    return editor;
  }

  private createDefaultEditor(): RecurrenceEditorState {
    return {
      recurrenceType: 'Daily',
      selectedWeekDays: [],
      intervalDays: 2,
      monthlyDay: 1,
    };
  }

  private normalizeRecurrenceType(recurrenceType: string): RecurrencePattern['recurrenceType'] {
    return recurrenceType === 'EveryDay' ? 'Daily' : recurrenceType as RecurrencePattern['recurrenceType'];
  }
}
