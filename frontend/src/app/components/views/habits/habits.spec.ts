import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import {
  Habit,
  HabitDashboardItem,
  HabitDashboardResponse,
  HabitToggleCompletionResponse,
} from '../../../models/habit.model';
import { HabitService } from '../../../services/habit-service';
import { LoadingService } from '../../../services/loading-service';
import { NotificationService } from '../../../services/notification-service';
import { UserService } from '../../../services/user-service';
import { Habits } from './habits';

describe('Habits', () => {
  let fixture: ComponentFixture<Habits>;
  let dashboardSubject: BehaviorSubject<HabitDashboardResponse | null>;
  let notificationDashboardSubject: BehaviorSubject<any>;
  let loadingSubject: BehaviorSubject<boolean>;
  let habitServiceMock: {
    dashboard$: Observable<HabitDashboardResponse | null>;
    refreshByUserId: ReturnType<typeof vi.fn>;
    toggleCompletion: ReturnType<typeof vi.fn>;
    add: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
    archiveHabit: ReturnType<typeof vi.fn>;
  };
  let snackBarMock: { open: ReturnType<typeof vi.fn> };
  let notificationServiceMock: {
    dashboard$: Observable<any>;
    refreshByUserId: ReturnType<typeof vi.fn>;
  };

  const baseHabit: HabitDashboardItem = {
    id: 1,
    title: 'Beber água pela manhã',
    icon: '💧',
    color: '',
    category: 'Health',
    recurrenceType: 'EveryDay',
    recurrenceConfig: null,
    reminderEnabled: true,
    reminderTime: null,
    reminderTimezone: 'America/Sao_Paulo',
    reminderMessage: null,
    reminderType: 'Standard',
    recurrenceLabel: 'Todos os dias',
    recurrence: {
      type: 'EveryDay',
      config: null,
      reminderTime: null,
      label: 'Todos os dias',
    },
    isArchived: false,
    isDueToday: true,
    isCompletedToday: false,
    currentStreak: 5,
    longestStreak: 11,
    weeklySuccessRate: 75,
    totalCompletions: 24,
    weeklyIndicators: [
      { date: '2026-06-01', dayOfWeek: 'Monday', isDue: true, isCompleted: true },
      { date: '2026-06-02', dayOfWeek: 'Tuesday', isDue: true, isCompleted: true },
      { date: '2026-06-03', dayOfWeek: 'Wednesday', isDue: true, isCompleted: false },
      { date: '2026-06-04', dayOfWeek: 'Thursday', isDue: true, isCompleted: true },
      { date: '2026-06-05', dayOfWeek: 'Friday', isDue: true, isCompleted: false },
      { date: '2026-06-06', dayOfWeek: 'Saturday', isDue: true, isCompleted: false },
      { date: '2026-06-07', dayOfWeek: 'Sunday', isDue: true, isCompleted: false },
    ],
    userId: 1,
  };

  const baseDashboard: HabitDashboardResponse = {
    userId: 1,
    date: '2026-06-05',
    activeHabits: [baseHabit],
    dailyProgress: {
      totalHabits: 3,
      dueToday: 2,
      completedToday: 1,
      completionRate: 50,
    },
    weeklySuccessRate: 72,
    totalCompletions: 41,
  };

  const toggleResponse: HabitToggleCompletionResponse = {
    habitId: baseHabit.id,
    completedDate: '2026-06-05',
    completedToday: true,
    completedAt: '2026-06-05T13:00:00',
    habit: {
      ...baseHabit,
      isCompletedToday: true,
      currentStreak: 6,
      totalCompletions: 25,
    },
  };

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    dashboardSubject = new BehaviorSubject<HabitDashboardResponse | null>(null);
    notificationDashboardSubject = new BehaviorSubject<any>(null);
    loadingSubject = new BehaviorSubject<boolean>(false);
    habitServiceMock = {
      dashboard$: dashboardSubject.asObservable(),
      refreshByUserId: vi.fn(),
      toggleCompletion: vi.fn(() => of(toggleResponse)),
      add: vi.fn(() => of({} as Habit)),
      update: vi.fn(() => of({} as Habit)),
      delete: vi.fn(() => of(true)),
      archiveHabit: vi.fn(() => of({} as Habit)),
    };
    snackBarMock = { open: vi.fn() };
    notificationServiceMock = {
      dashboard$: notificationDashboardSubject.asObservable(),
      refreshByUserId: vi.fn(),
    };

    await TestBed.configureTestingModule({
      declarations: [Habits],
      imports: [CommonModule, ReactiveFormsModule, MatMenuModule],
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
          provide: LoadingService,
          useValue: {
            loading$: loadingSubject.asObservable(),
          },
        },
        {
          provide: NotificationService,
          useValue: notificationServiceMock,
        },
        {
          provide: MatDialog,
          useValue: {
            open: vi.fn(() => ({ afterClosed: () => of(null) })),
          },
        },
        {
          provide: MatSnackBar,
          useValue: snackBarMock,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Habits);
    fixture.detectChanges();
  });

  it('loads the dashboard for the current user on init', () => {
    expect(habitServiceMock.refreshByUserId).toHaveBeenCalledWith(1);
    expect(notificationServiceMock.refreshByUserId).toHaveBeenCalledWith(1);
  });

  it('renders habit cards with title, streak and compact weekly indicators', () => {
    dashboardSubject.next(baseDashboard);
    fixture.detectChanges();

    const card = view().querySelector('[data-testid="habit-card"]') as HTMLElement;
    const weekIndicators = view().querySelectorAll('.week-indicator');

    expect(card).toBeTruthy();
    expect(card.textContent).toContain(baseHabit.title);
    expect(card.textContent).toContain('Sequência de 5 dias');
    expect(card.textContent).toContain('Todos os dias');
    expect(weekIndicators.length).toBe(7);
  });

  it('renders the empty state when there are no habits', () => {
    dashboardSubject.next({
      ...baseDashboard,
      activeHabits: [],
      dailyProgress: {
        totalHabits: 0,
        dueToday: 0,
        completedToday: 0,
        completionRate: 0,
      },
      weeklySuccessRate: 0,
      totalCompletions: 0,
    });
    fixture.detectChanges();

    expect(view().querySelector('[data-testid="habits-empty-state"]')).toBeTruthy();
    expect(view().textContent).toContain('Nenhum hábito ainda');
  });

  it('toggles today when a habit card is clicked', () => {
    dashboardSubject.next(baseDashboard);
    fixture.detectChanges();

    const card = view().querySelector('[data-testid="habit-card"]') as HTMLElement;

    card.click();

    expect(habitServiceMock.toggleCompletion).toHaveBeenCalledWith(baseHabit.id);
  });

  it('uses backend dashboard values for daily progress and stats', () => {
    dashboardSubject.next(baseDashboard);
    fixture.detectChanges();

    expect(view().textContent).toContain('1/2');
    expect(view().textContent).toContain('50% concluído');
    expect(view().textContent).toContain('72%');
    expect(view().textContent).toContain('41');
    expect(view().textContent).toContain('melhor: 11 dias');
  });

  it('renders subtle reminder visibility when reminder data is available', () => {
    dashboardSubject.next(baseDashboard);
    notificationDashboardSubject.next({
      nextReminder: {
        title: 'Beber água pela manhã',
        reminderTime: '09:00:00',
      },
      habitsAtRisk: [
        {
          message: 'Sua sequência de 5 dias ainda está ativa. Conclua Beber água pela manhã hoje.',
        },
      ],
      smartMotivations: ['Você concluiu 80% dos seus hábitos na semana passada.'],
    });
    fixture.detectChanges();

    expect(view().textContent).toContain('Próximo lembrete');
    expect(view().textContent).toContain('09:00 · Beber água pela manhã');
    expect(view().textContent).toContain('Incentivo inteligente');
  });

  it('shows a rollback error snackbar when completion toggle fails', () => {
    habitServiceMock.toggleCompletion.mockReturnValueOnce(
      throwError(() => new Error('toggle failed')),
    );
    dashboardSubject.next(baseDashboard);
    fixture.detectChanges();

    const card = view().querySelector('[data-testid="habit-card"]') as HTMLElement;
    card.click();

    expect(snackBarMock.open).toHaveBeenCalledWith(
      'Não foi possível atualizar este hábito. Seu progresso foi restaurado.',
      'Fechar',
      expect.objectContaining({ panelClass: ['snackbar-error'] }),
    );
  });

  it('does not toggle completion when the overflow menu is clicked', () => {
    dashboardSubject.next(baseDashboard);
    fixture.detectChanges();

    const menuButton = view().querySelector('.habit-menu-trigger') as HTMLButtonElement;
    menuButton.click();

    expect(habitServiceMock.toggleCompletion).not.toHaveBeenCalled();
  });
});
