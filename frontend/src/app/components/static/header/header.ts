import { DOCUMENT } from '@angular/common';
import { Component, HostListener, Inject, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { Observable, Subject, takeUntil } from 'rxjs';
import { UserService } from '../../../services/user-service';
import { UserResponse } from '../../../models/user-responde.model';
import { UserDialog } from '../../views/dialogs/user-dialog/user-dialog';
import { ConfirmDialog } from '../../views/dialogs/confirm-dialog/confirm-dialog';
import { ResolvedTheme, ThemeMode, ThemeService } from '../../../services/theme-service';

interface NavigationItem {
  label: string;
  icon: string;
  link: string;
}

@Component({
  selector: 'app-header',
  templateUrl: './header.html',
  styleUrl: './header.scss',
  standalone: false,
})
export class Header implements OnInit, OnDestroy {
  isLoggedIn$!: Observable<UserResponse | null>;
  isSidebarCollapsed = false;
  readonly themeMode$: Observable<ThemeMode>;
  readonly resolvedTheme$: Observable<ResolvedTheme>;
  readonly navigationItems: NavigationItem[] = [
    { label: 'Painel', icon: 'space_dashboard', link: '/habits' },
    { label: 'Análises', icon: 'insights', link: '/insights' },
    { label: 'Sequências', icon: 'local_fire_department', link: '/streaks' },
    { label: 'Conquistas', icon: 'emoji_events', link: '/achievements' },
    { label: 'Recorrências', icon: 'repeat', link: '/recurrences' },
    { label: 'Notificações', icon: 'notifications', link: '/notifications' },
    { label: 'Coach', icon: 'smart_toy', link: '/coach' }
  ];
  private isAuthenticated = false;
  private sidebarWasToggled = false;
  private readonly destroy$ = new Subject<void>();
  private readonly largeScreenQuery = '(min-width: 992px)';

  constructor(
    private userService: UserService,
    private router: Router,
    private dialog: MatDialog,
    private themeService: ThemeService,
    @Inject(DOCUMENT) private document: Document,
  ) {
    this.themeMode$ = this.themeService.mode$;
    this.resolvedTheme$ = this.themeService.resolvedTheme$;
  }

  ngOnInit(): void {
    this.isLoggedIn$ = this.userService.getUser();
    this.isSidebarCollapsed = !this.isLargeScreen();

    this.isLoggedIn$.pipe(takeUntil(this.destroy$)).subscribe((user) => {
      this.isAuthenticated = Boolean(user);
      this.syncShellState();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.clearShellState();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if (this.sidebarWasToggled) {
      return;
    }

    this.isSidebarCollapsed = !this.isLargeScreen();
    this.syncShellState();
  }

  toggleSidebar(): void {
    this.sidebarWasToggled = true;
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
    this.syncShellState();
  }

  openEditUserDialog(user: UserResponse): void {
    this.dialog.open(UserDialog, {
      width: '450px',
      data: user,
      panelClass: 'custom-dialog-container',
    });
  }

  toggleTheme(): void {
    this.themeService.toggleManualTheme();
  }

  logout(): void {
    const dialogRef = this.dialog.open(ConfirmDialog, {
      width: '400px',
      panelClass: 'custom-dialog-container',
      data: {
        title: 'Sair',
        message: 'Deseja sair da sua conta?',
        confirmBtnText: 'Sair',
      },
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.userService.logout();
        this.router.navigate(['/login']);
      }
    });
  }

  private isLargeScreen(): boolean {
    return globalThis.window?.matchMedia?.(this.largeScreenQuery).matches ?? true;
  }

  private syncShellState(): void {
    const { body } = this.document;

    body.classList.toggle('app-shell-authenticated', this.isAuthenticated);
    body.classList.toggle('app-sidebar-collapsed', this.isAuthenticated && this.isSidebarCollapsed);
    body.classList.toggle('app-sidebar-open', this.isAuthenticated && !this.isSidebarCollapsed);

    if (!this.isAuthenticated) {
      body.classList.remove('app-sidebar-collapsed', 'app-sidebar-open');
    }
  }

  private clearShellState(): void {
    this.document.body.classList.remove(
      'app-shell-authenticated',
      'app-sidebar-collapsed',
      'app-sidebar-open',
    );
  }
}
