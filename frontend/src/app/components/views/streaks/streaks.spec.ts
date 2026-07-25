import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { StreakCenter } from '../../../models/motivation.model';
import { MotivationService } from '../../../services/motivation-service';
import { UserService } from '../../../services/user-service';
import { Streaks } from './streaks';

describe('Streaks', () => {
  let fixture: ComponentFixture<Streaks>;
  let motivationServiceMock: {
    getStreakCenter: ReturnType<typeof vi.fn>;
  };

  const streakCenter: StreakCenter = {
    userId: 1,
    date: '2026-06-05',
    consistencyScore: 82,
    currentOverallStreak: 4,
    longestOverallStreak: 12,
    habitsAtRisk: [
      {
        habitId: 2,
        title: 'Treino',
        icon: '🏋️',
        color: '',
        category: 'Saúde',
        currentStreak: 0,
        lastCompletedDate: '2026-06-04',
        nextScheduledDate: '2026-06-05',
        missedScheduledDatesCount: 1,
        riskLevel: 'high',
        message: 'Treino está programado para hoje e ainda está aberto.',
      },
    ],
    habitStreaks: [
      {
        habitId: 1,
        title: 'Leitura',
        icon: '📖',
        color: '',
        category: 'Mente',
        currentStreak: 8,
        longestStreak: 12,
        completionRate: 93,
        totalCompletions: 18,
        lastCompletedDate: '2026-06-05',
        status: 'protected',
        message: 'Leitura está protegida hoje.',
      },
      {
        habitId: 2,
        title: 'Treino',
        icon: '🏋️',
        color: '',
        category: 'Saúde',
        currentStreak: 0,
        longestStreak: 4,
        completionRate: 55,
        totalCompletions: 6,
        lastCompletedDate: '2026-06-04',
        status: 'at-risk',
        message: 'Treino está programado para hoje e ainda está aberto.',
      },
    ],
    motivationalInsights: [
      'Você está em uma sequência geral de 4 dias.',
      'Treino precisa de atenção hoje.',
    ],
  };

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function configureComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      declarations: [Streaks],
      imports: [CommonModule],
      providers: [
        {
          provide: MotivationService,
          useValue: motivationServiceMock,
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
    motivationServiceMock = {
      getStreakCenter: vi.fn(() => of(streakCenter)),
    };

    await configureComponent();
  });

  it('renders streak score, habits at risk and habit streak cards', () => {
    fixture = TestBed.createComponent(Streaks);
    fixture.detectChanges();

    expect(view().textContent).toContain('Mantenha a sequência ativa.');
    expect(view().textContent).toContain('82');
    expect(view().textContent).toContain('Treino está programado para hoje e ainda está aberto.');
    expect(view().querySelectorAll('[data-testid="habit-risk-card"]').length).toBe(1);
    expect(view().querySelectorAll('[data-testid="habit-streak-card"]').length).toBe(2);
  });

  it('shows loading while streak center request is pending', async () => {
    TestBed.resetTestingModule();
    const pendingStreakCenter = new Subject<StreakCenter>();
    motivationServiceMock = {
      getStreakCenter: vi.fn(() => pendingStreakCenter.asObservable()),
    };
    await configureComponent();

    fixture = TestBed.createComponent(Streaks);
    fixture.detectChanges();

    expect(view().querySelector('[data-testid="streaks-loading"]')).toBeTruthy();
  });

  it('clears loading when streak center emits before the request completes', async () => {
    TestBed.resetTestingModule();
    const pendingStreakCenter = new Subject<StreakCenter>();
    motivationServiceMock = {
      getStreakCenter: vi.fn(() => pendingStreakCenter.asObservable()),
    };
    await configureComponent();

    fixture = TestBed.createComponent(Streaks);
    fixture.detectChanges();
    pendingStreakCenter.next(streakCenter);
    await fixture.whenStable();

    expect(fixture.componentInstance.loading).toBe(false);
    expect(fixture.componentInstance.streakCenter).toEqual(streakCenter);
    expect(view().querySelector('[data-testid="streaks-loading"]')).toBeFalsy();
    expect(view().textContent).toContain('Mantenha a sequência ativa.');
  });

  it('shows an empty state when no habit streaks exist', () => {
    motivationServiceMock.getStreakCenter.mockReturnValueOnce(of({
      ...streakCenter,
      currentOverallStreak: 0,
      longestOverallStreak: 0,
      habitsAtRisk: [],
      habitStreaks: [],
      motivationalInsights: ['Conclua um hábito programado para iniciar uma nova sequência.'],
    }));

    fixture = TestBed.createComponent(Streaks);
    fixture.detectChanges();

    expect(view().querySelector('[data-testid="streaks-empty-state"]')).toBeTruthy();
    expect(view().textContent).toContain('Conclua um hábito programado para iniciar uma sequência.');
  });
});
