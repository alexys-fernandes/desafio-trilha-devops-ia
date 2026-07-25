import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { provideRouter } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { Header } from './header';
import { UserService } from '../../../services/user-service';
import { ResolvedTheme, ThemeMode, ThemeService } from '../../../services/theme-service';
import { UserResponse } from '../../../models/user-responde.model';

describe('Header', () => {
  let fixture: ComponentFixture<Header>;
  let userSubject: BehaviorSubject<UserResponse | null>;
  let themeService: {
    mode$: Observable<ThemeMode>;
    resolvedTheme$: Observable<ResolvedTheme>;
    toggleManualTheme: ReturnType<typeof vi.fn>;
  };

  const user: UserResponse = {
    id: 1,
    name: 'Victor Mendes',
    email: 'victor@example.com',
  };

  beforeEach(async () => {
    userSubject = new BehaviorSubject<UserResponse | null>(user);
    themeService = {
      mode$: of('system'),
      resolvedTheme$: of('light'),
      toggleManualTheme: vi.fn(),
    };

    await TestBed.configureTestingModule({
      declarations: [Header],
      imports: [CommonModule, MatMenuModule, RouterModule],
      providers: [
        {
          provide: UserService,
          useValue: {
            getUser: () => userSubject.asObservable(),
            logout: vi.fn(),
          },
        },
        {
          provide: MatDialog,
          useValue: {
            open: vi.fn(),
          },
        },
        provideRouter([]),
        {
          provide: ThemeService,
          useValue: themeService,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Header);
    fixture.detectChanges();
  });

  afterEach(() => {
    fixture.destroy();
    document.body.classList.remove(
      'app-shell-authenticated',
      'app-sidebar-collapsed',
      'app-sidebar-open',
    );
  });

  it('calls ThemeService when the theme control is clicked', () => {
    const themeButton = fixture.nativeElement.querySelector('.sidebar-utility') as HTMLButtonElement;

    themeButton.click();

    expect(themeService.toggleManualTheme).toHaveBeenCalledTimes(1);
  });

  it('keeps the authenticated user name and profile menu trigger rendered', () => {
    const header = fixture.nativeElement as HTMLElement;
    const profileButton = header.querySelector('.btn-avatar') as HTMLButtonElement;

    expect(header.textContent).toContain(user.name);
    expect(profileButton).toBeTruthy();
    expect(profileButton.getAttribute('aria-label')).toContain(user.name);
  });

  it('renders the primary navigation inside the sidebar', () => {
    const header = fixture.nativeElement as HTMLElement;
    const links = Array.from(header.querySelectorAll('.sidebar-nav__label')).map((link) =>
      link.textContent?.trim(),
    );

    expect(links).toEqual([
      'Painel',
      'Análises',
      'Sequências',
      'Conquistas',
      'Recorrências',
      'Notificações',
    ]);
  });

  it('toggles the collapsible sidebar state', () => {
    const header = fixture.nativeElement as HTMLElement;
    const toggle = header.querySelector('.sidebar-toggle') as HTMLButtonElement;

    expect(header.querySelector('.sidebar-shell--collapsed')).toBeFalsy();
    expect(document.body.classList.contains('app-sidebar-open')).toBe(true);

    toggle.click();
    fixture.detectChanges();

    expect(header.querySelector('.sidebar-shell--collapsed')).toBeTruthy();
    expect(document.body.classList.contains('app-sidebar-collapsed')).toBe(true);
  });
});
