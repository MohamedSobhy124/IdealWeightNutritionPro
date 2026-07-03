import { isPlatformBrowser } from '@angular/common';
import { Component, effect, HostListener, inject, OnInit, PLATFORM_ID, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { UI, UiKey } from './core/i18n/ui-text';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/cart.service';
import { WishlistService } from './core/services/wishlist.service';
import { NewsletterService } from './core/services/newsletter.service';
import {
  clearGuestNewsletterEmail,
  readGuestNewsletterEmail,
  writeGuestNewsletterEmail,
} from './core/services/newsletter-storage';
import { LocaleService } from './core/services/locale.service';
import { SeoService } from './core/services/seo.service';
import { FlashSaleBannerComponent } from './features/promotions/flash-sale-banner.component';
import { ContactFloatingComponent } from './shared/contact-floating/contact-floating.component';
import { NotificationBellComponent } from './shared/notification-bell/notification-bell.component';
import { ThemeService } from './core/services/theme.service';
import { UiThemeToggleComponent } from './shared/ui/ui-theme-toggle.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    FlashSaleBannerComponent,
    ContactFloatingComponent,
    NotificationBellComponent,
    UiThemeToggleComponent,
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);
  readonly wishlist = inject(WishlistService);
  private readonly newsletterApi = inject(NewsletterService);
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);
  private readonly platformId = inject(PLATFORM_ID);
  readonly locale = inject(LocaleService);
  readonly theme = inject(ThemeService);

  readonly newsletterBusy = signal(false);
  readonly newsletterSubscribed = signal(false);
  readonly newsletterStatusLoaded = signal(false);
  readonly newsletterMessage = signal<string | null>(null);
  readonly newsletterError = signal(false);
  readonly guestNewsletterEmail = signal('');

  readonly mobileMenuOpen = signal(false);
  readonly searchOpen = signal(false);
  readonly userMenuOpen = signal(false);
  readonly headerSearch = signal('');
  readonly year = new Date().getFullYear();

  readonly isAdminRoute = signal(this.detectAdminRoute());
  readonly headerScrolled = signal(false);

  readonly primaryNav = [
    { labelKey: 'shop' as UiKey, link: '/shop', exact: false },
    { labelKey: 'navOffers' as UiKey, link: '/offers', exact: false },
    { labelKey: 'flashSales' as UiKey, link: '/flash-sales', exact: false },
    { labelKey: 'combos' as UiKey, link: '/combos', exact: false },
    { labelKey: 'services' as UiKey, link: '/services', exact: false },
    { labelKey: 'blog' as UiKey, link: '/blog', exact: false },
  ];

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  cartAriaLabel(): string {
    const count = this.cart.itemCount();
    return count > 0 ? this.t('cartWithCount').replace('{count}', String(count)) : this.t('cart');
  }

  wishlistAriaLabel(): string {
    const count = this.wishlist.count();
    return count > 0 ? this.t('wishlistWithCount').replace('{count}', String(count)) : this.t('wishlist');
  }

  submitSearch(): void {
    const q = this.headerSearch().trim();
    this.mobileMenuOpen.set(false);
    this.searchOpen.set(false);
    this.router.navigate(['/shop'], { queryParams: q ? { search: q } : {} });
  }

  openSearch(): void {
    this.searchOpen.set(true);
    this.mobileMenuOpen.set(false);
  }

  closeSearch(): void {
    this.searchOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.user-menu')) this.userMenuOpen.set(false);
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.headerScrolled.set(window.scrollY > 6);
  }

  constructor() {
    effect(() => {
      if (this.auth.isAuthenticated()) {
        untracked(() => {
          this.scheduleNonCritical(() => this.wishlist.loadProductIds().subscribe());
          if (!this.auth.profile()) {
            this.auth.loadProfile().subscribe();
          }
          this.scheduleNonCritical(() => this.loadNewsletterStatus());
        });
      } else {
        this.wishlist.clearLocal();
        this.newsletterMessage.set(null);
        if (isPlatformBrowser(this.platformId)) {
          untracked(() => this.initGuestNewsletterStatus());
        } else {
          this.newsletterSubscribed.set(false);
          this.newsletterStatusLoaded.set(true);
        }
      }
    });

    effect(() => {
      this.locale.locale();
      untracked(() => this.seo.refresh());
    });
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.applyCultureFromQuery();
      this.scheduleNonCritical(() => this.cart.load().subscribe());
      this.headerScrolled.set(window.scrollY > 6);
      this.isAdminRoute.set(this.detectAdminRoute());
      if (this.auth.isAuthenticated() && !this.auth.profile()) {
        this.auth.loadProfile().subscribe();
      }
    }
    this.applyRouteSeo();
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.applyRouteSeo();
        this.mobileMenuOpen.set(false);
        this.searchOpen.set(false);
        this.userMenuOpen.set(false);
        this.isAdminRoute.set((event as NavigationEnd).urlAfterRedirects.startsWith('/admin'));
      });

  }

  private detectAdminRoute(): boolean {
    if (isPlatformBrowser(this.platformId)) {
      return window.location.pathname.startsWith('/admin');
    }
    return this.router.url.startsWith('/admin');
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

  private applyCultureFromQuery(): void {
    const culture = new URLSearchParams(window.location.search).get('culture');
    if (culture === 'ar' || culture === 'en') {
      this.locale.setLocale(culture);
    }
  }

  private applyRouteSeo(): void {
    let route = this.router.routerState.snapshot.root;
    while (route.firstChild) {
      route = route.firstChild;
    }

    const data = route.data;
    if (data['seoDynamic']) {
      return;
    }
    if (data['seoHome']) {
      this.seo.reset();
      return;
    }
    const titleKey = data['seoTitle'] as UiKey | undefined;
    if (titleKey) {
      const descriptionKey = data['seoDescription'] as UiKey | undefined;
      this.seo.applyRoute(titleKey, descriptionKey);
      return;
    }
    this.seo.reset();
  }

  private loadNewsletterStatus(): void {
    this.newsletterStatusLoaded.set(false);
    this.newsletterApi.getStatus().subscribe({
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
    this.newsletterApi.getStatus(email).subscribe({
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

  subscribeNewsletter(): void {
    if (!this.auth.isAuthenticated()) return;
    this.newsletterBusy.set(true);
    this.newsletterMessage.set(null);
    this.newsletterError.set(false);
    const email = this.auth.profile()?.email;
    this.newsletterApi.subscribe(email).subscribe({
      next: (res) => {
        this.newsletterMessage.set(res.message || this.t('newsletterSubscribed'));
        this.newsletterSubscribed.set(true);
        this.newsletterBusy.set(false);
      },
      error: (err) => {
        this.newsletterMessage.set(
          err?.error?.errors?.[0] ?? this.t('newsletterSubscribeFailed')
        );
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
    this.newsletterApi.subscribe(email, 'Footer').subscribe({
      next: (res) => {
        writeGuestNewsletterEmail(email);
        this.newsletterMessage.set(res.message || this.t('newsletterSubscribed'));
        this.newsletterSubscribed.set(true);
        this.newsletterBusy.set(false);
      },
      error: (err) => {
        this.newsletterMessage.set(
          err?.error?.errors?.[0] ?? this.t('newsletterSubscribeFailed')
        );
        this.newsletterError.set(true);
        this.newsletterBusy.set(false);
      },
    });
  }

  unsubscribeNewsletter(): void {
    if (!confirm(this.t('newsletterConfirmUnsubscribe'))) return;

    this.newsletterBusy.set(true);
    this.newsletterMessage.set(null);
    this.newsletterError.set(false);

    const email = this.auth.isAuthenticated()
      ? this.auth.profile()?.email
      : this.guestNewsletterEmail().trim() || undefined;

    this.newsletterApi.unsubscribe(email).subscribe({
      next: (res) => {
        if (!this.auth.isAuthenticated()) {
          clearGuestNewsletterEmail();
        }
        this.newsletterMessage.set(res.message || this.t('newsletterUnsubscribed'));
        this.newsletterSubscribed.set(false);
        this.newsletterBusy.set(false);
      },
      error: (err) => {
        this.newsletterMessage.set(
          err?.error?.errors?.[0] ?? this.t('newsletterUnsubscribeFailed')
        );
        this.newsletterError.set(true);
        this.newsletterBusy.set(false);
      },
    });
  }
}
