import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';

export type AppLocale = 'en' | 'ar';

const STORAGE_KEY = 'iwn-locale';

@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly platformId = inject(PLATFORM_ID);

  readonly locale = signal<AppLocale>(this.readStored());
  readonly isArabic = computed(() => this.locale() === 'ar');

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      this.applyDocument(this.locale());
    }
  }

  pick(en: string, ar?: string | null): string {
    if (this.isArabic() && ar && ar.trim()) return ar;
    return en;
  }

  setLocale(locale: AppLocale): void {
    this.locale.set(locale);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(STORAGE_KEY, locale);
      this.applyDocument(locale);
    }
  }

  toggle(): void {
    this.setLocale(this.locale() === 'ar' ? 'en' : 'ar');
  }

  private readStored(): AppLocale {
    if (!isPlatformBrowser(this.platformId)) {
      return 'ar';
    }
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'en' || stored === 'ar') {
      return stored;
    }
    return 'ar';
  }

  private applyDocument(locale: AppLocale): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    document.documentElement.lang = locale;
    document.documentElement.dir = locale === 'ar' ? 'rtl' : 'ltr';
  }
}
