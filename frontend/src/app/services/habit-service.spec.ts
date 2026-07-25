import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { HabitDashboardResponse } from '../models/habit.model';
import { HabitService } from './habit-service';

describe('HabitService', () => {
  let service: HabitService;
  let httpMock: HttpTestingController;

  const apiDashboard = {
    userId: 1,
    date: '2026-06-05',
    activeHabits: [
      {
        id: 1,
        userId: 1,
        title: 'Morning water',
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
        isArchived: false,
        isDueToday: true,
        completedToday: false,
        currentStreak: 2,
        longestStreak: 6,
        weeklySuccessRate: 50,
        totalCompletions: 7,
        weeklyIndicators: [
          { date: '2026-06-01', dayOfWeek: 'Monday', isDue: true, isCompleted: true },
          { date: '2026-06-02', dayOfWeek: 'Tuesday', isDue: true, isCompleted: false },
          { date: '2026-06-03', dayOfWeek: 'Wednesday', isDue: true, isCompleted: true },
          { date: '2026-06-04', dayOfWeek: 'Thursday', isDue: true, isCompleted: false },
          { date: '2026-06-05', dayOfWeek: 'Friday', isDue: true, isCompleted: false },
          { date: '2026-06-06', dayOfWeek: 'Saturday', isDue: true, isCompleted: false },
          { date: '2026-06-07', dayOfWeek: 'Sunday', isDue: true, isCompleted: false },
        ],
      },
    ],
    dailyProgress: {
      totalHabits: 1,
      dueToday: 1,
      completedToday: 0,
      completionRate: 0,
    },
    weeklySuccessRate: 50,
    totalCompletions: 7,
  };

  function currentDashboard(): HabitDashboardResponse | null {
    let dashboard: HabitDashboardResponse | null = null;
    const subscription = service.dashboard$.subscribe((value) => {
      dashboard = value;
    });

    subscription.unsubscribe();
    return dashboard;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [HabitService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(HabitService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads habit dashboard data from the new endpoint', () => {
    let response: HabitDashboardResponse | undefined;

    service.getDashboard(1).subscribe((dashboard) => {
      response = dashboard;
    });

    const request = httpMock.expectOne(`${environment.apiUrl}/habit/user/1/dashboard`);
    expect(request.request.method).toBe('GET');
    request.flush(apiDashboard);

    expect(response?.activeHabits[0].isCompletedToday).toBe(false);
    expect(response?.activeHabits[0].recurrenceLabel).toBe('Todos os dias');
    expect(currentDashboard()?.dailyProgress.completedToday).toBe(0);
  });

  it('optimistically toggles completion and keeps backend values on success', () => {
    service.getDashboard(1).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/habit/user/1/dashboard`).flush(apiDashboard);

    service.toggleCompletion(1).subscribe();

    const optimisticDashboard = currentDashboard();
    expect(optimisticDashboard?.activeHabits[0].isCompletedToday).toBe(true);
    expect(optimisticDashboard?.dailyProgress.completedToday).toBe(1);

    const request = httpMock.expectOne(`${environment.apiUrl}/habit/1/toggle-completion`);
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.has('X-Skip-Loading')).toBe(true);

    request.flush({
      habitId: 1,
      completedDate: '2026-06-05',
      completedToday: true,
      completedAt: '2026-06-05T13:00:00',
      habit: {
        ...apiDashboard.activeHabits[0],
        completedToday: true,
        currentStreak: 3,
        longestStreak: 6,
        weeklySuccessRate: 60,
        totalCompletions: 8,
        weeklyIndicators: apiDashboard.activeHabits[0].weeklyIndicators.map((indicator) =>
          indicator.date === '2026-06-05' ? { ...indicator, isCompleted: true } : indicator
        ),
      },
    });

    const dashboard = currentDashboard();
    expect(dashboard?.activeHabits[0].currentStreak).toBe(3);
    expect(dashboard?.totalCompletions).toBe(8);
  });

  it('rolls back optimistic completion when the API fails', () => {
    let failed = false;

    service.getDashboard(1).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/habit/user/1/dashboard`).flush(apiDashboard);

    service.toggleCompletion(1).subscribe({
      error: () => {
        failed = true;
      },
    });

    expect(currentDashboard()?.activeHabits[0].isCompletedToday).toBe(true);

    const request = httpMock.expectOne(`${environment.apiUrl}/habit/1/toggle-completion`);
    request.flush({ message: 'Failed' }, { status: 500, statusText: 'Server Error' });

    expect(failed).toBe(true);
    expect(currentDashboard()?.activeHabits[0].isCompletedToday).toBe(false);
    expect(currentDashboard()?.dailyProgress.completedToday).toBe(0);
  });
});
