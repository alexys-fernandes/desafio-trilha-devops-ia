import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import {
  AchievementSet,
  MonthlyChallengeSet,
  MotivationSummary
} from '../../../models/motivation.model';
import { MotivationService } from '../../../services/motivation-service';
import { UserService } from '../../../services/user-service';
import { Achievements } from './achievements';

describe('Achievements', () => {
  let fixture: ComponentFixture<Achievements>;
  let motivationServiceMock: {
    getSummary: ReturnType<typeof vi.fn>;
    getAchievements: ReturnType<typeof vi.fn>;
    getMonthlyChallenges: ReturnType<typeof vi.fn>;
  };

  const summary: MotivationSummary = {
    userId: 1,
    date: '2026-06-05',
    consistencyScore: 84,
    currentOverallStreak: 4,
    longestOverallStreak: 12,
    unlockedAchievements: 1,
    totalAchievements: 3,
    activeMonthlyChallenges: 2,
    mostAtRiskHabit: null,
    motivationalInsights: [
      'Seu ritmo está melhorando nesta semana.',
      'Consistência mensal está 80% completo.',
    ],
  };

  const achievementSet: AchievementSet = {
    userId: 1,
    date: '2026-06-05',
    consistencyScore: 84,
    unlockedCount: 1,
    totalCount: 3,
    achievements: [
      {
        id: 'first-check',
        title: 'Primeira marcação',
        description: 'Conclua seu primeiro hábito programado.',
        icon: 'check_circle',
        category: 'Fundação',
        currentValue: 1,
        targetValue: 1,
        progressPercent: 100,
        isUnlocked: true,
        message: 'Liberada',
      },
      {
        id: 'ten-checks',
        title: 'Dez marcações',
        description: 'Alcance 10 conclusões de hábitos no total.',
        icon: 'done_all',
        category: 'Fundação',
        currentValue: 6,
        targetValue: 10,
        progressPercent: 60,
        isUnlocked: false,
        message: 'Construa uma base visível.',
      },
      {
        id: 'week-streak',
        title: 'Sequência de sete dias',
        description: 'Mantenha uma sequência geral perfeita por 7 dias programados.',
        icon: 'local_fire_department',
        category: 'Sequência',
        currentValue: 4,
        targetValue: 7,
        progressPercent: 57,
        isUnlocked: false,
        message: 'Proteja todos os dias programados por uma semana.',
      },
    ],
  };

  const monthlyChallenges: MonthlyChallengeSet = {
    userId: 1,
    monthLabel: 'junho 2026',
    startDate: '2026-06-01',
    endDate: '2026-06-05',
    challenges: [
      {
        id: 'monthly-consistency',
        title: 'Consistência mensal',
        description: 'Alcance uma taxa de conclusão de 85% nos hábitos programados deste mês.',
        icon: 'calendar_month',
        currentValue: 80,
        targetValue: 85,
        progressPercent: 94,
        isCompleted: false,
        message: 'Mantenha o mês estável.',
      },
      {
        id: 'perfect-days',
        title: 'Dias perfeitos',
        description: 'Finalize todos os hábitos programados em 10 dias deste mês.',
        icon: 'verified',
        currentValue: 2,
        targetValue: 10,
        progressPercent: 20,
        isCompleted: false,
        message: 'Acumule mais dias perfeitos.',
      },
    ],
  };

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function configureComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      declarations: [Achievements],
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
      getSummary: vi.fn(() => of(summary)),
      getAchievements: vi.fn(() => of(achievementSet)),
      getMonthlyChallenges: vi.fn(() => of(monthlyChallenges)),
    };

    await configureComponent();
  });

  it('renders achievement progress and monthly challenges', () => {
    fixture = TestBed.createComponent(Achievements);
    fixture.detectChanges();

    expect(view().textContent).toContain('Progresso que merece atenção.');
    expect(view().textContent).toContain('1');
    expect(view().textContent).toContain('Consistência mensal');
    expect(view().textContent).toContain('Primeira marcação');
    expect(view().textContent).toContain('Dez marcações');
    expect(view().querySelectorAll('[data-testid="monthly-challenge-card"]').length).toBe(2);
    expect(view().querySelectorAll('[data-testid="achievement-card"]').length).toBe(3);
  });

  it('shows loading while achievement requests are pending', async () => {
    TestBed.resetTestingModule();
    const pendingSummary = new Subject<MotivationSummary>();
    motivationServiceMock = {
      getSummary: vi.fn(() => pendingSummary.asObservable()),
      getAchievements: vi.fn(() => of(achievementSet)),
      getMonthlyChallenges: vi.fn(() => of(monthlyChallenges)),
    };
    await configureComponent();

    fixture = TestBed.createComponent(Achievements);
    fixture.detectChanges();

    expect(view().querySelector('[data-testid="achievements-loading"]')).toBeTruthy();
  });

  it('clears loading when achievement data emits before all requests complete', async () => {
    TestBed.resetTestingModule();
    const pendingSummary = new Subject<MotivationSummary>();
    motivationServiceMock = {
      getSummary: vi.fn(() => pendingSummary.asObservable()),
      getAchievements: vi.fn(() => of(achievementSet)),
      getMonthlyChallenges: vi.fn(() => of(monthlyChallenges)),
    };
    await configureComponent();

    fixture = TestBed.createComponent(Achievements);
    fixture.detectChanges();
    pendingSummary.next(summary);
    await fixture.whenStable();

    expect(fixture.componentInstance.loading).toBe(false);
    expect(fixture.componentInstance.view?.summary).toEqual(summary);
    expect(view().querySelector('[data-testid="achievements-loading"]')).toBeFalsy();
    expect(view().textContent).toContain('Progresso que merece atenção.');
  });

  it('shows an empty state when no achievement definitions are available', () => {
    motivationServiceMock.getAchievements.mockReturnValueOnce(of({
      ...achievementSet,
      unlockedCount: 0,
      totalCount: 0,
      achievements: [],
    }));

    fixture = TestBed.createComponent(Achievements);
    fixture.detectChanges();

    expect(view().querySelector('[data-testid="achievements-empty-state"]')).toBeTruthy();
    expect(view().textContent).toContain('Conclua hábitos programados para liberar conquistas.');
  });
});
