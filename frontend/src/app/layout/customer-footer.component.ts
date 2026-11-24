import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, PLATFORM_ID, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { UI, UiKey } from '../core/i18n/ui-text';
import { AuthService } from '../core/services/auth.service';
import { LocaleService } from '../core/services/locale.service';
import { NewsletterService } from '../core/services/newsletter.service';
import {
  clearGuestNewsletterEmail,
  readGuestNewsletterEmail,
  writeGuestNewsletterEmail,
} from '../core/services/newsletter-storage';
import { UiConfirmService } from '../shared/ui/ui-confirm.service';

const COPY = {
  about: {
    en: 'Original supplements, healthy food and expert nutrition advice, with fast delivery across the UAE.',
    ar: 'مكملات أصلية وأغذية صحية ونصائح تغذية من الخبراء، مع توصيل سريع في جميع أنحاء الإمارات.',
  },
  allProducts: { en: 'All products', ar: 'كل المنتجات' },
  company: { en: 'Company', ar: 'الشركة' },
  visit: { en: 'Our branches', ar: 'فروعنا' },
  rights: { en: 'All rights reserved.', ar: 'جميع الحقوق محفوظة.' },
  securePay: { en: 'Secure payment', ar: 'دفع آمن' },
} as const;

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-customer-footer',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './customer-footer.component.html',
  styleUrl: './customer-footer.component.css',
})
export class CustomerFooterComponent {
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);
  private readonly newsletterApi = inject(NewsletterService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);

  readonly year = new Date().getFullYear();
  readonly newsletterBusy = signal(false);
  readonly newsletterSubscribed = signal(false);
  readonly newsletterStatusLoaded = signal(false);
  readonly newsletterMessage = signal<string | null>(null);
  readonly newsletterError = signal(false);
  readonly guestNewsletterEmail = signal('');

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated()) {
        untracked(() => this.scheduleNonCritical(() => this.loadNewsletterStatus()));
      } else {
        this.newsletterMessage.set(null);
        if (isPlatformBrowser(this.platformId)) {
          untracked(() => this.initGuestNewsletterStatus());
        } else {
          this.newsletterSubscribed.set(false);
          this.newsletterStatusLoaded.set(true);
        }
      }
    });
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  c(key: keyof typeof COPY): string {
    return this.locale.pick(COPY[key].en, COPY[key].ar);
  }

  subscribeNewsletter(): void {
    if (!this.auth.isAuthenticated()) return;
    this.newsletterBusy.set(true);
    this.newsletterMessage.set(null);
    this.newsletterError.set(false);
    const email = this.auth.profile()?.email;
    this.newsletterApi.subscribe(email).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.newsletterMessage.set(res.message || this.t('newsletterSubscribed'));
        this.newsletterSubscribed.set(true);
        this.newsletterBusy.set(false);
      },
      error: (err) => {
        this.newsletterMessage.set(err?.error?.errors?.[0] ?? this.t('newsletterSubscribeFailed'));
        this.newsletterError.set(true);
        this.newsletterBusy.set(false);
      },
    });
  }

  subscribeGuestNewsletter(): void {
    const email = this.guestNewsletterEmail().trim();
    if (!email || !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) {
      this.newsletterMessage.set(this.t('newsletterInvalidEmail'));
      this.newsletterError.set(true);
      return;
    }

    this.newsletterBusy.set(true);
    this.newsletterMessage.set(null);
    this.newsletterError.set(false);
    this.newsletterApi.subscribe(email, 'Footer').pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        writeGuestNewsletterEmail(email);
        this.newsletterMessage.set(res.message || this.t('newsletterSubscribed'));
        this.newsletterSubscribed.set(true);
        this.newsletterBusy.set(false);
      },
      error: (err) => {
        this.newsletterMessage.set(err?.error?.errors?.[0] ?? this.t('newsletterSubscribeFailed'));
        this.newsletterError.set(true);
        this.newsletterBusy.set(false);
      },
    });
  }

  async unsubscribeNewsletter(): Promise<void> {
    const confirmed = await this.confirmDialog.ask({
      title: this.t('newsletterConfirmUnsubscribe'),
      confirmLabel: this.t('newsletterUnsubscribe'),
      icon: 'unsubscribe',
    });
    if (!confirmed) return;

    this.newsletterBusy.set(true);
    this.newsletterMessage.set(null);
    this.newsletterError.set(false);

    const email = this.auth.isAuthenticated()
      ? this.auth.profile()?.email
      : this.guestNewsletterEmail().trim() || undefined;

    this.newsletterApi.unsubscribe(email).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        if (!this.auth.isAuthenticated()) {
          clearGuestNewsletterEmail();
        }
        this.newsletterMessage.set(res.message || this.t('newsletterUnsubscribed'));
        this.newsletterSubscribed.set(false);
        this.newsletterBusy.set(false);
      },
      error: (err) => {
        this.newsletterMessage.set(err?.error?.errors?.[0] ?? this.t('newsletterUnsubscribeFailed'));
        this.newsletterError.set(true);
        this.newsletterBusy.set(false);
      },
    });
  }

  private loadNewsletterStatus(): void {
    this.newsletterStatusLoaded.set(false);
    this.newsletterApi.getStatus().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (status) => {
        this.newsletterSubscribed.set(status.isSubscribed);
        this.newsletterStatusLoaded.set(true);
      },
      error: () => {
        this.newsletterSubscribed.set(false);
        this.newsletterStatusLoaded.set(true);
      },
    });
  }

  private initGuestNewsletterStatus(): void {
    const storedEmail = readGuestNewsletterEmail();
    if (!storedEmail) {
      this.newsletterSubscribed.set(false);
      this.newsletterStatusLoaded.set(true);
      return;
    }

    this.guestNewsletterEmail.set(storedEmail);
    this.loadGuestNewsletterStatus(storedEmail);
  }

  private loadGuestNewsletterStatus(email: string): void {
    this.newsletterStatusLoaded.set(false);
    this.newsletterApi.getStatus(email).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (status) => {
        this.newsletterSubscribed.set(status.isSubscribed);
        this.newsletterStatusLoaded.set(true);
      },
      error: () => {
        this.newsletterSubscribed.set(false);
        this.newsletterStatusLoaded.set(true);
      },
    });
  }

  private scheduleNonCritical(task: () => void): void {
    if (!isPlatformBrowser(this.platformId)) {
      task();
      return;
    }
    const win = window as Window & {
      requestIdleCallback?: (cb: () => void, options?: { timeout: number }) => number;
    };
    if (typeof win.requestIdleCallback === 'function') {
      win.requestIdleCallback(() => task(), { timeout: 2000 });
      return;
    }
    window.setTimeout(task, 250);
  }
}
