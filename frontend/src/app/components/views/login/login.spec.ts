import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BehaviorSubject, of } from 'rxjs';
import { Login } from './login';
import { LoadingService } from '../../../services/loading-service';
import { UserService } from '../../../services/user-service';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let loadingSubject: BehaviorSubject<boolean>;

  function view(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    loadingSubject = new BehaviorSubject<boolean>(false);

    await TestBed.configureTestingModule({
      declarations: [Login],
      imports: [CommonModule, FormsModule],
      providers: [
        {
          provide: UserService,
          useValue: {
            login: vi.fn(() => of({ id: 1, name: 'Victor', email: 'victor@example.com' })),
            register: vi.fn(() => of({ id: 1, name: 'Victor', email: 'victor@example.com' })),
          },
        },
        {
          provide: LoadingService,
          useValue: {
            loading$: loadingSubject.asObservable(),
          },
        },
        {
          provide: Router,
          useValue: {
            navigate: vi.fn(),
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

    fixture = TestBed.createComponent(Login);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('renders only email and password fields in login mode', () => {
    expect(view().querySelector('[data-testid="auth-name-input"]')).toBeNull();
    expect(view().querySelector('[data-testid="auth-email-input"]')).toBeTruthy();
    expect(view().querySelector('[data-testid="auth-password-input"]')).toBeTruthy();
    expect(view().querySelector('#login-tab')?.getAttribute('aria-selected')).toBe('true');
  });

  it('renders the name field when account creation mode is selected', async () => {
    const registerTab = view().querySelector('#register-tab') as HTMLButtonElement;

    registerTab.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(view().querySelector('[data-testid="auth-name-input"]')).toBeTruthy();
    expect(view().querySelector('[data-testid="auth-email-input"]')).toBeTruthy();
    expect(view().querySelector('[data-testid="auth-password-input"]')).toBeTruthy();
    expect(view().querySelector('#register-tab')?.getAttribute('aria-selected')).toBe('true');
  });

  it('keeps the submit button disabled while the form is invalid', () => {
    const submitButton = view().querySelector('.auth-submit') as HTMLButtonElement;

    expect(submitButton.disabled).toBe(true);
  });
});
