import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';

const STORAGE_KEY = 'iwn-theme';

export type ThemeMode = 'light' | 'dark' | 'system';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  readonly mode = signal<ThemeMode>('light');
  readonly isDark = signal(false);

  constructor() {
    if (this.isBrowser) {
      const stored = localStorage.getItem(STORAGE_KEY) as ThemeMode | null;
      if (stored === 'light' || stored === 'dark' || stored === 'system') {
        this.mode.set(stored);
      }
      this.apply();
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        if (this.mode() === 'system') this.apply();
      });
    }
  }

  setMode(mode: ThemeMode): void {
    this.mode.set(mode);
    if (this.isBrowser) {
      localStorage.setItem(STORAGE_KEY, mode);
      this.apply();
    }
  }

  toggle(): void {
    const next = this.isDark() ? 'light' : 'dark';
    this.setMode(next);
  }

  /** Call before bootstrap to avoid flash (inline script in index.html). */
  static resolveInitialDark(): boolean {
    if (typeof window === 'undefined') return false;
    const stored = localStorage.getItem(STORAGE_KEY) as ThemeMode | null;
    if (stored === 'dark') return true;
    if (stored === 'light') return false;
    if (stored === 'system') return window.matchMedia('(prefers-color-scheme: dark)').matches;
    return false;
  }

  private apply(): void {
    if (!this.isBrowser) return;
    const dark =
      this.mode() === 'dark' ||
      (this.mode() === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);
    this.isDark.set(dark);
    document.documentElement.classList.toggle('dark', dark);
  }
}
