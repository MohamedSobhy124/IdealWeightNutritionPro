import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, OnInit, PLATFORM_ID, signal, untracked } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs/operators';
import { UI, UiKey } from './core/i18n/ui-text';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/cart.service';
import { LocaleService } from './core/services/locale.service';
import { SeoService } from './core/services/seo.service';
import { WishlistService } from './core/services/wishlist.service';
import { FlashSaleBannerComponent } from './features/promotions/flash-sale-banner.component';
import { CustomerBottomNavComponent } from './layout/customer-bottom-nav.component';
import { CustomerDrawerComponent } from './layout/customer-drawer.component';
import { CustomerFooterComponent } from './layout/customer-footer.component';
import { CustomerHeaderComponent } from './layout/customer-header.component';
import { CustomerShellState } from './layout/customer-shell.state';
import { ContactFloatingComponent } from './shared/contact-floating/contact-floating.component';
import { UiFeedbackHostComponent } from './shared/ui/ui-feedback-host.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    FlashSaleBannerComponent,
    ContactFloatingComponent,
    UiFeedbackHostComponent,
    CustomerHeaderComponent,
    CustomerDrawerComponent,
    CustomerFooterComponent,
    CustomerBottomNavComponent,
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);
  private readonly wishlist = inject(WishlistService);
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);
  private readonly platformId = inject(PLATFORM_ID);
  readonly locale = inject(LocaleService);

  /** Eager: overlay dismissal and the back button must track navigation on /admin too. */
  private readonly shell = inject(CustomerShellState);

  readonly isAdminRoute = signal(this.detectAdminRoute());

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  constructor() {
    void this.shell;

    effect(() => {
      if (this.auth.isAuthenticated()) {
        untracked(() => {
          this.scheduleNonCritical(() => this.wishlist.loadProductIds().subscribe());
          if (!this.auth.profile()) {
            this.auth.loadProfile().subscribe();
          }
        });
      } else {
        this.wishlist.clearLocal();
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
}
