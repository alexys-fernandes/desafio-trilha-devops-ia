import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { Habit } from '../../../models/habit.model';
import { HabitService } from '../../../services/habit-service';
import { UserService } from '../../../services/user-service';
import { Recurrences } from './recurrences';

describe('Recurrences', () => {
  let fixture: ComponentFixture<Recurrences>;
  let habitServiceMock: {
    getHabitsByUser: ReturnType<typeof vi.fn>;
    getRecurrenceLabel: ReturnType<typeof vi.fn>;
    buildRecurrenceConfig: ReturnType<typeof vi.fn>;
    updateRecurrence: ReturnType<typeof vi.fn>;
  };

  const habits: Habit[] = [
    createHabit(1, 'Read', 'Daily', null),
    createHabit(2, 'Run', 'SpecificDaysOfWeek', JSON.stringify({ daysOfWeek: ['Monday', 'Friday'] })),
    createHabit(3, 'Journal', 'SpecificDaysOfWeek', JSON.stringify({ daysOfWeek: ['Monday', 'Friday'] })),
  ];

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    habitServiceMock = {
      getHabitsByUser: vi.fn(() => of(habits)),
      getRecurrenceLabel: vi.fn((type: string) => type === 'Daily' ? 'Todos os dias' : 'Seg, Sex'),
      buildRecurrenceConfig: vi.fn((type: string, days: string[], interval: number, monthlyDay: number) => {
        if (type === 'EveryXDays') return JSON.stringify({ interval });
        if (type === 'Monthly') return JSON.stringify({ dayOfMonth: monthlyDay });
        if (type === 'SpecificDaysOfWeek') return JSON.stringify({ daysOfWeek: days });
        return null;
      }),
      updateRecurrence: vi.fn((id: number) => of({ ...habits[0], id })),
    };

    await TestBed.configureTestingModule({
      declarations: [Recurrences],
      imports: [CommonModule, FormsModule],
      providers: [
        {
          provide: HabitService,
          useValue: habitServiceMock,
        },
        {
          provide: UserService,
          useValue: {
            getUser: vi.fn(() => of({ id: 1, name: 'Victor', email: 'victor@example.com' })),
          },
        },
        {
          provide: MatSnackBar,
          useValue: {
            open: vi.fn(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Recurrences);
    fixture.detectChanges();
  });

  it('groups habits by recurrence pattern', () => {
    const cards = view().querySelectorAll('[data-testid="recurrence-pattern-card"]');

    expect(cards.length).toBe(2);
    expect(view().textContent).toContain('Todos os dias');
    expect(view().textContent).toContain('Seg, Sex');
    expect(view().textContent).toContain('Run');
    expect(view().textContent).toContain('Journal');
  });

  it('bulk assigns recurrence to selected habits', () => {
    const component = fixture.componentInstance;

    component.toggleHabitSelection(1);
    component.bulkEditor.recurrenceType = 'EveryXDays';
    component.bulkEditor.intervalDays = 3;
    component.applyToSelected();

    expect(habitServiceMock.updateRecurrence).toHaveBeenCalledWith(1, {
      recurrenceType: 'EveryXDays',
      recurrenceConfig: JSON.stringify({ interval: 3 }),
      reminderTime: null,
    });
  });

  it('updates every habit in a recurrence pattern', () => {
    const component = fixture.componentInstance;
    const specificDaysPattern = component.patterns.find((pattern) => pattern.habits.length === 2);

    expect(specificDaysPattern).toBeTruthy();
    component.editPattern(specificDaysPattern!);
    component.patternEditor.recurrenceType = 'Monthly';
    component.patternEditor.monthlyDay = 12;
    component.applyToPattern(specificDaysPattern!);

    expect(habitServiceMock.updateRecurrence).toHaveBeenCalledWith(2, {
      recurrenceType: 'Monthly',
      recurrenceConfig: JSON.stringify({ dayOfMonth: 12 }),
      reminderTime: null,
    });
    expect(habitServiceMock.updateRecurrence).toHaveBeenCalledWith(3, {
      recurrenceType: 'Monthly',
      recurrenceConfig: JSON.stringify({ dayOfMonth: 12 }),
      reminderTime: null,
    });
  });
});

function createHabit(
  id: number,
  title: string,
  recurrenceType: string,
  recurrenceConfig: string | null,
): Habit {
  return {
    id,
    title,
    icon: '✓',
    color: '',
    category: '',
    recurrenceType,
    recurrenceConfig,
    reminderEnabled: false,
    reminderTime: null,
    reminderTimezone: 'America/Sao_Paulo',
    reminderMessage: null,
    reminderType: 'Standard',
    isArchived: false,
    userId: 1,
    createdAt: new Date('2026-06-01T08:00:00'),
    isDeleted: false,
  };
}
