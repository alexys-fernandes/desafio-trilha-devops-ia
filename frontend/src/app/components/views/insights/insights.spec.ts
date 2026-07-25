import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import {
  AnalyticsOverview,
  CalendarAnalytics,
  HabitAnalytics,
  TrendAnalytics
} from '../../../models/analytics.model';
import { Habit } from '../../../models/habit.model';
import { AnalyticsService } from '../../../services/analytics-service';
import { HabitService } from '../../../services/habit-service';
import { UserService } from '../../../services/user-service';
import { Insights } from './insights';

describe('Insights', () => {
  let fixture: ComponentFixture<Insights>;
  let analyticsServiceMock: {
    getOverview: ReturnType<typeof vi.fn>;
    getCalendar: ReturnType<typeof vi.fn>;
    getTrends: ReturnType<typeof vi.fn>;
    getHabitAnalytics: ReturnType<typeof vi.fn>;
  };
  let habitServiceMock: {
    getHabitsByUser: ReturnType<typeof vi.fn>;
  };

  const overview: AnalyticsOverview = {
    userId: 1,
    date: '2026-06-05',
    totalActiveHabits: 2,
    totalCompletions: 24,
    averageCompletionRate: 87,
    bestHabit: {
      habitId: 1,
      title: 'Leitura',
      icon: '📖',
      color: '',
      category: 'Mente',
      completionRate: 95,
      currentStreak: 8,
      longestStreak: 12,
      totalCompletions: 18,
    },
    weakestHabit: {
      habitId: 2,
      title: 'Treino',
      icon: '🏋️',
      color: '',
      category: 'Saúde',
      completionRate: 62,
      currentStreak: 1,
      longestStreak: 4,
      totalCompletions: 6,
    },
    bestDayOfWeek: {
      dayOfWeek: 'Monday',
      scheduledHabits: 4,
      completedHabits: 4,
      completionRate: 100,
    },
    weakestDayOfWeek: {
      dayOfWeek: 'Saturday',
      scheduledHabits: 4,
      completedHabits: 1,
      completionRate: 25,
    },
    currentOverallStreak: 3,
    longestOverallStreak: 9,
    weeklyCompletionTrend: createTrendDays(7, 2, 2),
    monthlyCompletionTrend: createTrendDays(30, 2, 1),
  };

  const calendar: CalendarAnalytics = {
    userId: 1,
    startDate: '2026-03-08',
    endDate: '2026-06-05',
    days: [
      { date: '2026-06-01', scheduledCount: 2, completedCount: 2, completionRate: 100, status: 'perfect' },
      { date: '2026-06-02', scheduledCount: 2, completedCount: 1, completionRate: 50, status: 'partial' },
      { date: '2026-06-03', scheduledCount: 2, completedCount: 0, completionRate: 0, status: 'missed' },
    ],
  };

  const trends: TrendAnalytics = {
    userId: 1,
    date: '2026-06-05',
    last7Days: {
      label: 'Últimos 7 dias',
      startDate: '2026-05-30',
      endDate: '2026-06-05',
      scheduledHabits: 14,
      completedHabits: 13,
      completionRate: 93,
      dailyBreakdown: createTrendDays(7, 2, 2),
    },
    last30Days: {
      label: 'Últimos 30 dias',
      startDate: '2026-05-07',
      endDate: '2026-06-05',
      scheduledHabits: 60,
      completedHabits: 48,
      completionRate: 80,
      dailyBreakdown: createTrendDays(30, 2, 1),
    },
    last90Days: {
      label: 'Últimos 90 dias',
      startDate: '2026-03-08',
      endDate: '2026-06-05',
      scheduledHabits: 180,
      completedHabits: 144,
      completionRate: 80,
      dailyBreakdown: createTrendDays(90, 2, 1),
    },
  };

  const habits: Habit[] = [
    createHabit(1, 'Leitura'),
    createHabit(2, 'Treino'),
  ];

  const habitAnalytics: HabitAnalytics[] = [
    createHabitAnalytics(1, 'Leitura', '📖', 95),
    createHabitAnalytics(2, 'Treino', '🏋️', 62),
  ];

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function configureComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      declarations: [Insights],
      imports: [CommonModule],
      providers: [
        {
          provide: AnalyticsService,
          useValue: analyticsServiceMock,
        },
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
      ],
    }).compileComponents();
  }

  beforeEach(async () => {
    analyticsServiceMock = {
      getOverview: vi.fn(() => of(overview)),
      getCalendar: vi.fn(() => of(calendar)),
      getTrends: vi.fn(() => of(trends)),
      getHabitAnalytics: vi.fn((_: number, habitId: number) =>
        of(habitAnalytics.find((habit) => habit.habitId === habitId)!)
      ),
    };
    habitServiceMock = {
      getHabitsByUser: vi.fn(() => of(habits)),
    };

    await configureComponent();
  });

  it('renders overview, insight messages, calendar heatmap and habit breakdown', () => {
    fixture = TestBed.createComponent(Insights);
    fixture.detectChanges();

    expect(view().textContent).toContain('Sua consistência, mapeada com clareza.');
    expect(view().textContent).toContain('87%');
    expect(view().textContent).toContain('Você tem mais consistência em segundas-feiras.');
    expect(view().textContent).toContain('Leitura tem sua sequência mais forte.');
    expect(view().querySelector('[data-testid="calendar-heatmap"]')).toBeTruthy();
    expect(view().querySelectorAll('.heatmap-cell').length).toBeGreaterThanOrEqual(3);
    expect(view().querySelectorAll('[data-testid="habit-insight-card"]').length).toBe(2);
  });

  it('shows a loading state while analytics are pending', async () => {
    TestBed.resetTestingModule();
    const pendingOverview = new Subject<AnalyticsOverview>();
    analyticsServiceMock = {
      getOverview: vi.fn(() => pendingOverview.asObservable()),
      getCalendar: vi.fn(() => of(calendar)),
      getTrends: vi.fn(() => of(trends)),
      getHabitAnalytics: vi.fn(() => of(habitAnalytics[0])),
    };
    habitServiceMock = {
      getHabitsByUser: vi.fn(() => of(habits)),
    };
    await configureComponent();

    fixture = TestBed.createComponent(Insights);
    fixture.detectChanges();

    expect(view().querySelector('[data-testid="insights-loading"]')).toBeTruthy();
  });

  it('clears loading when analytics data emits before the request completes', async () => {
    TestBed.resetTestingModule();
    const pendingOverview = new Subject<AnalyticsOverview>();
    analyticsServiceMock = {
      getOverview: vi.fn(() => pendingOverview.asObservable()),
      getCalendar: vi.fn(() => of(calendar)),
      getTrends: vi.fn(() => of(trends)),
      getHabitAnalytics: vi.fn((_: number, habitId: number) =>
        of(habitAnalytics.find((habit) => habit.habitId === habitId)!)
      ),
    };
    habitServiceMock = {
      getHabitsByUser: vi.fn(() => of(habits)),
    };
    await configureComponent();

    fixture = TestBed.createComponent(Insights);
    fixture.detectChanges();
    pendingOverview.next(overview);
    await fixture.whenStable();

    expect(fixture.componentInstance.loading).toBe(false);
    expect(fixture.componentInstance.view?.overview).toEqual(overview);
    expect(view().querySelector('[data-testid="insights-loading"]')).toBeFalsy();
    expect(view().textContent).toContain('Sua consistência, mapeada com clareza.');
  });

  it('shows a helpful empty state when there is not enough history', () => {
    analyticsServiceMock.getOverview.mockReturnValueOnce(of({
      ...overview,
      totalActiveHabits: 0,
      totalCompletions: 0,
      averageCompletionRate: 0,
      bestHabit: null,
      weakestHabit: null,
      bestDayOfWeek: null,
      weakestDayOfWeek: null,
      currentOverallStreak: 0,
      longestOverallStreak: 0,
      weeklyCompletionTrend: createTrendDays(7, 0, 0),
      monthlyCompletionTrend: createTrendDays(30, 0, 0),
    }));
    habitServiceMock.getHabitsByUser.mockReturnValueOnce(of([]));

    fixture = TestBed.createComponent(Insights);
    fixture.detectChanges();

    expect(view().querySelector('[data-testid="insights-empty-state"]')).toBeTruthy();
    expect(view().textContent).toContain('Conclua alguns hábitos para liberar análises.');
    expect(view().textContent).toContain('Seus padrões aparecerão aqui após alguns dias de acompanhamento.');
  });
});

function createTrendDays(count: number, scheduledHabits: number, completedHabits: number) {
  return Array.from({ length: count }, (_, index) => ({
    date: `2026-05-${String(index + 1).padStart(2, '0')}`,
    scheduledHabits,
    completedHabits,
    completionRate: scheduledHabits ? Math.round((completedHabits / scheduledHabits) * 100) : 0,
  }));
}

function createHabit(id: number, title: string): Habit {
  return {
    id,
    title,
    icon: '✓',
    color: '',
    category: 'Personal',
    recurrenceType: 'Daily',
    recurrenceConfig: null,
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

function createHabitAnalytics(
  habitId: number,
  title: string,
  icon: string,
  completionRate: number,
): HabitAnalytics {
  return {
    userId: 1,
    habitId,
    title,
    icon,
    color: '',
    category: 'Personal',
    currentStreak: habitId === 1 ? 8 : 1,
    longestStreak: habitId === 1 ? 12 : 4,
    totalCompletions: habitId === 1 ? 18 : 6,
    completionRate,
    bestWeekday: {
      dayOfWeek: 'Monday',
      scheduledHabits: 4,
      completedHabits: 4,
      completionRate: 100,
    },
    weakestWeekday: {
      dayOfWeek: 'Saturday',
      scheduledHabits: 4,
      completedHabits: 1,
      completionRate: 25,
    },
    lastCompletedDate: '2026-06-05',
    missedScheduledDates: habitId === 1 ? [] : ['2026-06-03'],
    weeklyTrend: createTrendDays(7, 1, 1),
    monthlyTrend: createTrendDays(30, 1, 1),
  };
}
