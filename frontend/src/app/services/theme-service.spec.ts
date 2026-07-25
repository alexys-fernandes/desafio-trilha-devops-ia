import { ThemeMode, ThemeService, ResolvedTheme } from './theme-service';

const THEME_STORAGE_KEY = 'habitapp_theme_mode';

describe('ThemeService', () => {
  let mediaQueryMatches = false;
  let mediaQueryListener: ((event: MediaQueryListEvent) => void) | undefined;

  function mockSystemTheme(matchesDarkTheme: boolean): void {
    mediaQueryMatches = matchesDarkTheme;
    mediaQueryListener = undefined;

    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: vi.fn().mockImplementation((query: string): MediaQueryList => {
        const mediaQuery = {
          media: query,
          onchange: null,
          get matches() {
            return mediaQueryMatches;
          },
          addEventListener: vi.fn(
            (_eventName: string, listener: EventListenerOrEventListenerObject) => {
              if (typeof listener === 'function') {
                mediaQueryListener = listener as (event: MediaQueryListEvent) => void;
              }
            },
          ),
          removeEventListener: vi.fn(),
          addListener: vi.fn(),
          removeListener: vi.fn(),
          dispatchEvent: vi.fn(),
        };

        return mediaQuery as unknown as MediaQueryList;
      }),
    });
  }

  function getCurrentResolvedTheme(service: ThemeService): ResolvedTheme {
    let currentTheme: ResolvedTheme = 'light';
    const subscription = service.resolvedTheme$.subscribe((theme) => {
      currentTheme = theme;
    });

    subscription.unsubscribe();

    return currentTheme;
  }

  function getCurrentMode(service: ThemeService): ThemeMode {
    let currentMode: ThemeMode = 'system';
    const subscription = service.mode$.subscribe((mode) => {
      currentMode = mode;
    });

    subscription.unsubscribe();

    return currentMode;
  }

  beforeEach(() => {
    localStorage.removeItem(THEME_STORAGE_KEY);
    document.documentElement.classList.remove('theme-light', 'theme-dark');
    document.documentElement.style.colorScheme = '';
    mockSystemTheme(false);
  });

  it('starts in light mode without saved preference', () => {
    mockSystemTheme(true);

    const service = new ThemeService();

    expect(service.getCurrentMode()).toBe('light');
    expect(getCurrentMode(service)).toBe('light');
    expect(getCurrentResolvedTheme(service)).toBe('light');
    expect(document.documentElement.classList.contains('theme-light')).toBe(true);
  });

  it('persists manual theme and applies the matching document class', () => {
    const service = new ThemeService();

    service.setMode('dark');

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
    expect(service.getCurrentMode()).toBe('dark');
    expect(getCurrentResolvedTheme(service)).toBe('dark');
    expect(document.documentElement.classList.contains('theme-dark')).toBe(true);
    expect(document.documentElement.classList.contains('theme-light')).toBe(false);
  });

  it('toggles manual theme from light to dark and updates color-scheme', () => {
    const service = new ThemeService();

    service.setMode('light');
    service.toggleManualTheme();

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
    expect(service.getCurrentMode()).toBe('dark');
    expect(document.documentElement.classList.contains('theme-light')).toBe(false);
    expect(document.documentElement.classList.contains('theme-dark')).toBe(true);
    expect(document.documentElement.style.colorScheme).toBe('dark');
  });

  it('removes manual preference in system mode and reacts to system changes', () => {
    const service = new ThemeService();

    service.setMode('light');
    service.setMode('system');
    mediaQueryMatches = true;
    mediaQueryListener?.({ matches: true } as MediaQueryListEvent);

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBeNull();
    expect(service.getCurrentMode()).toBe('system');
    expect(getCurrentResolvedTheme(service)).toBe('dark');
    expect(document.documentElement.classList.contains('theme-dark')).toBe(true);
  });
});
