import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type ThemeMode = 'system' | 'light' | 'dark';
export type ResolvedTheme = 'light' | 'dark';

const THEME_STORAGE_KEY = 'habitapp_theme_mode';
const DARK_THEME_QUERY = '(prefers-color-scheme: dark)';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly mediaQuery = this.createMediaQuery();
  private readonly modeSubject = new BehaviorSubject<ThemeMode>(this.readStoredMode());
  private readonly resolvedThemeSubject = new BehaviorSubject<ResolvedTheme>(
    this.resolveTheme(this.modeSubject.value),
  );

  readonly mode$: Observable<ThemeMode> = this.modeSubject.asObservable();
  readonly resolvedTheme$: Observable<ResolvedTheme> = this.resolvedThemeSubject.asObservable();

  constructor() {
    this.applyResolvedTheme(this.resolvedThemeSubject.value);
    this.mediaQuery?.addEventListener('change', this.handleSystemThemeChange);
  }

  setMode(mode: ThemeMode): void {
    if (mode === 'system') {
      localStorage.removeItem(THEME_STORAGE_KEY);
    } else {
      localStorage.setItem(THEME_STORAGE_KEY, mode);
    }

    this.modeSubject.next(mode);
    this.updateResolvedTheme();
  }

  toggleManualTheme(): void {
    this.setMode(this.resolvedThemeSubject.value === 'dark' ? 'light' : 'dark');
  }

  getCurrentMode(): ThemeMode {
    return this.modeSubject.value;
  }

  private handleSystemThemeChange = (): void => {
    if (this.modeSubject.value === 'system') {
      this.updateResolvedTheme();
    }
  };

  private updateResolvedTheme(): void {
    const resolvedTheme = this.resolveTheme(this.modeSubject.value);

    this.resolvedThemeSubject.next(resolvedTheme);
    this.applyResolvedTheme(resolvedTheme);
  }

  private resolveTheme(mode: ThemeMode): ResolvedTheme {
    if (mode !== 'system') {
      return mode;
    }

    return this.mediaQuery?.matches ? 'dark' : 'light';
  }

  private applyResolvedTheme(theme: ResolvedTheme): void {
    const root = document.documentElement;

    root.classList.remove('theme-light', 'theme-dark');
    root.classList.add(`theme-${theme}`);
    root.style.colorScheme = theme;
  }

  private readStoredMode(): ThemeMode {
    const storedMode = localStorage.getItem(THEME_STORAGE_KEY);

    if (storedMode === 'light' || storedMode === 'dark' || storedMode === 'system') {
      return storedMode;
    }

    return 'light';
  }

  private createMediaQuery(): MediaQueryList | null {
    if (!window.matchMedia) {
      return null;
    }

    return window.matchMedia(DARK_THEME_QUERY);
  }
}
