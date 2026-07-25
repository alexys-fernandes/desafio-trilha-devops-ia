import { CommonModule } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, Subject } from 'rxjs';
import {
  NotificationPreference,
  ReminderDashboard
} from '../../../models/notification.model';
import { NotificationService } from '../../../services/notification-service';
import { UserService } from '../../../services/user-service';
import { Notifications } from './notifications';

describe('Notifications', () => {
  let fixture: ComponentFixture<Notifications>;
  let notificationServiceMock: {
    getPreferences: ReturnType<typeof vi.fn>;
    updatePreferences: ReturnType<typeof vi.fn>;
    getDashboard: ReturnType<typeof vi.fn>;
  };

  const preferences: NotificationPreference = {
    id: 1,
    userId: 1,
    notificationsEnabled: true,
    quietHoursStart: '22:00:00',
    quietHoursEnd: '07:00:00',
    reminderSoundEnabled: true,
    motivationalNotificationsEnabled: true,
    achievementNotificationsEnabled: true,
    streakRiskNotificationsEnabled: true,
    defaultReminderTime: '09:00:00',
    defaultReminderType: 'Standard',
    createdAt: new Date('2026-06-01T08:00:00'),
    isDeleted: false,
  };

  const dashboard: ReminderDashboard = {
    userId: 1,
    date: '2026-06-05',
    generatedAt: '2026-06-05T08:00:00',
    preferences,
    nextReminder: {
      habitId: 1,
      title: 'Leitura',
      icon: '📚',
      color: '',
      category: 'Mente',
      scheduledDate: '2026-06-05',
      reminderTime: '09:00:00',
      scheduledAt: '2026-06-05T09:00:00',
      timezone: 'America/Sao_Paulo',
      reminderType: 'Standard',
      message: 'Hora de Leitura.',
      isCompleted: false,
      isSuppressedByQuietHours: false,
    },
    upcomingReminders: [],
    missedHabits: [],
    habitsAtRisk: [
      {
        habitId: 1,
        title: 'Leitura',
        icon: '📚',
        color: '',
        category: 'Mente',
        currentStreak: 4,
        longestStreak: 7,
        daysUntilPersonalRecord: 3,
        riskLevel: 'medium',
        message: 'Sua sequência de 4 dias ainda está ativa.',
      },
    ],
    smartMotivations: ['Você concluiu 92% dos seus hábitos na semana passada.'],
    payloads: [],
  };

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function configureComponent(): Promise<void> {
    await TestBed.configureTestingModule({
      declarations: [Notifications],
      imports: [CommonModule, FormsModule],
      providers: [
        {
          provide: NotificationService,
          useValue: notificationServiceMock,
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
  }

  beforeEach(async () => {
    notificationServiceMock = {
      getPreferences: vi.fn(() => of(preferences)),
      updatePreferences: vi.fn(() => of(preferences)),
      getDashboard: vi.fn(() => of(dashboard)),
    };

    await configureComponent();
  });

  it('renders notification settings and reminder preview', () => {
    fixture = TestBed.createComponent(Notifications);
    fixture.detectChanges();

    expect(view().textContent).toContain('Mantenha a consistência sem ruído extra.');
    expect(view().textContent).toContain('Ativar notificações');
    expect(view().textContent).toContain('Estilo padrão do lembrete');
    expect(view().textContent).toContain('09:00 · Leitura');
    expect(view().textContent).toContain('Você concluiu 92% dos seus hábitos na semana passada.');
  });

  it('saves normalized notification preferences', () => {
    fixture = TestBed.createComponent(Notifications);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    component.formData.notificationsEnabled = false;
    component.formData.quietHoursStart = '21:30';
    component.formData.quietHoursEnd = '06:15';
    component.formData.defaultReminderTime = '08:45';
    component.saveSettings();

    expect(notificationServiceMock.updatePreferences).toHaveBeenCalledWith(1, expect.objectContaining({
      notificationsEnabled: false,
      quietHoursStart: '21:30:00',
      quietHoursEnd: '06:15:00',
      defaultReminderTime: '08:45:00',
    }));
  });

  it('shows loading while settings are pending', async () => {
    TestBed.resetTestingModule();
    const pendingDashboard = new Subject<ReminderDashboard>();
    notificationServiceMock = {
      getPreferences: vi.fn(() => of(preferences)),
      updatePreferences: vi.fn(() => of(preferences)),
      getDashboard: vi.fn(() => pendingDashboard.asObservable()),
    };
    await configureComponent();

    fixture = TestBed.createComponent(Notifications);
    fixture.detectChanges();

    expect(view().querySelector('[data-testid="notifications-loading"]')).toBeTruthy();
  });

  it('clears loading when settings emit before the request completes', async () => {
    TestBed.resetTestingModule();
    const pendingDashboard = new Subject<ReminderDashboard>();
    notificationServiceMock = {
      getPreferences: vi.fn(() => of(preferences)),
      updatePreferences: vi.fn(() => of(preferences)),
      getDashboard: vi.fn(() => pendingDashboard.asObservable()),
    };
    await configureComponent();

    fixture = TestBed.createComponent(Notifications);
    fixture.detectChanges();
    pendingDashboard.next(dashboard);
    await fixture.whenStable();

    expect(fixture.componentInstance.loading).toBe(false);
    expect(fixture.componentInstance.view?.dashboard).toEqual(dashboard);
    expect(view().querySelector('[data-testid="notifications-loading"]')).toBeFalsy();
    expect(view().textContent).toContain('Mantenha a consistência sem ruído extra.');
  });
});
