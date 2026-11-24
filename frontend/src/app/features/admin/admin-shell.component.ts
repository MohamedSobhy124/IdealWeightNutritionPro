import { A11yModule } from '@angular/cdk/a11y';
import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  HostListener,
  inject,
  OnInit,
  PLATFORM_ID,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter } from 'rxjs/operators';
import { ADMIN_UI, AdminUiKey } from '../../core/i18n/admin-ui-text';
import { AdminDashboardService } from '../../core/services/admin-dashboard.service';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { NotificationBellComponent } from '../../shared/notification-bell/notification-bell.component';
import { UiThemeToggleComponent } from '../../shared/ui/ui-theme-toggle.component';

interface NavItem {
  labelKey: AdminUiKey;
  link: string;
  icon: string;
  exact?: boolean;
  adminOnly?: boolean;
}

interface NavGroup {
  labelKey: AdminUiKey;
  items: NavItem[];
}

interface Breadcrumb {
  label: string;
  link: string | null;
}

interface PaletteItem {
  label: string;
  link: string;
  icon: string;
  group: string;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    NotificationBellComponent,
    UiThemeToggleComponent,
    A11yModule,
  ],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.css',
})
export class AdminShellComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly dashboardApi = inject(AdminDashboardService);
  readonly locale = inject(LocaleService);

  readonly isAdmin = computed(
    () => this.auth.profile()?.roles.includes('Admin') ?? false
  );
  readonly profile = this.auth.profile;

  readonly collapsed = signal(false);
  readonly mobileOpen = signal(false);
  readonly userMenuOpen = signal(false);
  readonly currentUrl = signal('/admin/dashboard');
  readonly paletteOpen = signal(false);
  readonly paletteQuery = signal('');
  readonly paletteIndex = signal(0);
  readonly density = signal<'comfortable' | 'compact'>('comfortable');
  /** Pending work per nav link, from the dashboard API (refreshed on navigation, at most once a minute). */
  readonly navCounts = signal<Record<string, number>>({});
  private countsLoadedAt = 0;
  /** Last nav pages visited, newest first (stored per browser). */
  readonly recentLinks = signal<string[]>([]);
  private readonly paletteInput = viewChild<ElementRef<HTMLInputElement>>('paletteInput');

  constructor() {
    effect(() => {
      if (!this.paletteOpen()) return;
      const input = this.paletteInput();
      if (!input) return;
      queueMicrotask(() => input.nativeElement.focus());
    });
  }

  private readonly groups: NavGroup[] = [
    {
      labelKey: 'groupOverview',
      items: [
        { labelKey: 'navDashboard', link: '/admin/dashboard', icon: 'dashboard', exact: true },
      ],
    },
    {
      labelKey: 'groupSales',
      items: [
        { labelKey: 'navOrders', link: '/admin/orders', icon: 'receipt_long' },
        { labelKey: 'navReturns', link: '/admin/returns', icon: 'assignment_return' },
        { labelKey: 'navStockNotifications', link: '/admin/stock-notifications', icon: 'notifications_active' },
        { labelKey: 'navReviews', link: '/admin/reviews', icon: 'star' },
      ],
    },
    {
      labelKey: 'groupCatalogue',
      items: [
        { labelKey: 'navProducts', link: '/admin/products', icon: 'inventory_2', adminOnly: true },
        { labelKey: 'navCategories', link: '/admin/categories', icon: 'category', adminOnly: true },
        { labelKey: 'navBrands', link: '/admin/brands', icon: 'sell', adminOnly: true },
      ],
    },
    {
      labelKey: 'groupMarketing',
      items: [
        { labelKey: 'navPromoCodes', link: '/admin/promo-codes', icon: 'confirmation_number', adminOnly: true },
        { labelKey: 'navFlashSales', link: '/admin/flash-sales', icon: 'bolt', adminOnly: true },
        { labelKey: 'navComboOffers', link: '/admin/combo-offers', icon: 'redeem', adminOnly: true },
        { labelKey: 'navNewsletter', link: '/admin/newsletter', icon: 'mail', adminOnly: true },
      ],
    },
    {
      labelKey: 'groupServicesGroup',
      items: [
        { labelKey: 'navServices', link: '/admin/services', icon: 'medical_services', adminOnly: true },
        { labelKey: 'navServiceOffers', link: '/admin/service-offers', icon: 'local_offer', adminOnly: true },
        { labelKey: 'navServicePurchases', link: '/admin/service-purchases', icon: 'shopping_bag', adminOnly: true },
      ],
    },
    {
      labelKey: 'groupContent',
      items: [
        { labelKey: 'navBlog', link: '/admin/blog', icon: 'article', adminOnly: true },
        { labelKey: 'navVideoBanner', link: '/admin/video-banner', icon: 'smart_display', adminOnly: true },
      ],
    },
    {
      labelKey: 'groupSettings',
      items: [
        { labelKey: 'navCities', link: '/admin/cities', icon: 'location_city', adminOnly: true },
        { labelKey: 'navCompanies', link: '/admin/companies', icon: 'business', adminOnly: true },
        { labelKey: 'navUsers', link: '/admin/users', icon: 'group', adminOnly: true },
      ],
    },
  ];

  readonly visibleGroups = computed(() => {
    const admin = this.isAdmin();
    return this.groups
      .map((group) => ({
        ...group,
        items: group.items.filter((item) => admin || !item.adminOnly),
      }))
      .filter((group) => group.items.length > 0);
  });

  readonly paletteItems = computed<PaletteItem[]>(() => {
    this.locale.locale();
    const items: PaletteItem[] = [];
    for (const group of this.visibleGroups()) {
      for (const item of group.items) {
        items.push({
          label: this.t(item.labelKey),
          link: item.link,
          icon: item.icon,
          group: this.t(group.labelKey),
        });
      }
    }
    if (this.isAdmin()) {
      const create = this.t('newBadge');
      const extras: { labelKey: AdminUiKey; link: string; icon: string }[] = [
        { labelKey: 'newProduct', link: '/admin/products/new', icon: 'add_box' },
        { labelKey: 'navPromoCodes', link: '/admin/promo-codes/new', icon: 'confirmation_number' },
        { labelKey: 'navFlashSales', link: '/admin/flash-sales/new', icon: 'bolt' },
        { labelKey: 'navComboOffers', link: '/admin/combo-offers/new', icon: 'redeem' },
        { labelKey: 'navBlog', link: '/admin/blog/new', icon: 'article' },
        { labelKey: 'navServices', link: '/admin/services/new', icon: 'medical_services' },
      ];
      for (const extra of extras) {
        items.push({ label: this.t(extra.labelKey), link: extra.link, icon: extra.icon, group: create });
      }
    }
    return items;
  });

  readonly paletteResults = computed(() => {
    const raw = this.paletteQuery().trim();
    const query = raw.toLowerCase();
    const items = this.paletteItems();
    if (!query) {
      const recentGroup = this.t('paletteRecent');
      const recent = this.recentLinks()
        .map((link) => items.find((i) => i.link === link))
        .filter((i): i is PaletteItem => !!i)
        .map((i) => ({ ...i, group: recentGroup }));
      const rest = items.filter((i) => !recent.some((r) => r.link === i.link));
      return [...recent, ...rest].slice(0, 12);
    }
    const extra: PaletteItem[] = [];
    const id = raw.replace(/^#/, '');
    if (/^\d+$/.test(id)) {
      const g = this.t('paletteRecords');
      extra.push({ label: `${this.t('orderNumber')} #${id}`, link: `/admin/orders/${id}`, icon: 'receipt_long', group: g });
      extra.push({ label: `${this.t('returnNumber')} #${id}`, link: `/admin/returns/${id}`, icon: 'assignment_return', group: g });
      if (this.isAdmin()) {
        extra.push({ label: `${this.t('navProducts')} #${id}`, link: `/admin/products/${id}`, icon: 'inventory_2', group: g });
      }
    } else if (raw.length >= 2) {
      const g = this.t('paletteSearchIn');
      const q = encodeURIComponent(raw);
      extra.push({ label: `${this.t('navOrders')}: “${raw}”`, link: `/admin/orders?q=${q}`, icon: 'search', group: g });
      if (this.isAdmin()) {
        extra.push({ label: `${this.t('navProducts')}: “${raw}”`, link: `/admin/products?q=${q}&filter=all`, icon: 'search', group: g });
      }
    }
    const matched = items.filter((item) => item.label.toLowerCase().includes(query) || item.group.toLowerCase().includes(query));
    return [...matched, ...extra].slice(0, 12);
  });

  navCount(link: string): number {
    return this.navCounts()[link] ?? 0;
  }

  private loadCounts(): void {
    if (!isPlatformBrowser(this.platformId) || Date.now() - this.countsLoadedAt < 60_000) return;
    this.countsLoadedAt = Date.now();
    this.dashboardApi.getDashboard().subscribe({
      next: (d) =>
        this.navCounts.set({
          '/admin/orders': d.ordersPending ?? 0,
          '/admin/returns': d.pendingReturns ?? 0,
          '/admin/stock-notifications': d.pendingStockNotifications ?? 0,
          '/admin/service-purchases': d.servicePurchasesPending ?? 0,
        }),
      error: () => (this.countsLoadedAt = 0),
    });
  }

  private rememberVisit(url: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const path = url.split(/[?#]/)[0];
    let best: string | null = null;
    for (const group of this.visibleGroups()) {
      for (const item of group.items) {
        if ((path === item.link || path.startsWith(item.link + '/')) && (!best || item.link.length > best.length)) best = item.link;
      }
    }
    if (!best) return;
    const next = [best, ...this.recentLinks().filter((l) => l !== best)].slice(0, 4);
    this.recentLinks.set(next);
    try {
      localStorage.setItem('iwn-admin-recent', JSON.stringify(next));
    } catch {
      /* storage unavailable */
    }
  }

  readonly activePaletteIndex = computed(() => {
    const length = this.paletteResults().length;
    if (length === 0) return 0;
    return Math.min(this.paletteIndex(), length - 1);
  });

  readonly activeLabel = computed(() => {
    const url = this.currentUrl();
    let match: NavItem | null = null;
    for (const group of this.visibleGroups()) {
      for (const item of group.items) {
        if (url === item.link || url.startsWith(item.link + '/')) {
          if (!match || item.link.length > match.link.length) match = item;
        }
      }
    }
    return match ? this.t(match.labelKey) : this.t('navDashboard');
  });

  readonly breadcrumbs = computed<Breadcrumb[]>(() => {
    const segments = this.currentUrl().split(/[?#]/)[0].split('/').filter(Boolean).slice(1);
    const crumbs: Breadcrumb[] = [{ label: this.t('adminTitle'), link: '/admin/dashboard' }];
    if (segments.length === 0 || segments[0] === 'dashboard') {
      crumbs.push({ label: this.t('navDashboard'), link: null });
      return crumbs;
    }
    let link = '/admin';
    segments.forEach((segment, index) => {
      link += `/${segment}`;
      crumbs.push({
        label: index === 0 ? this.navLabel(link) ?? segment : this.segmentLabel(segment),
        link: index === segments.length - 1 ? null : link,
      });
    });
    return crumbs;
  });

  private navLabel(link: string): string | null {
    for (const group of this.groups) {
      const item = group.items.find((i) => i.link === link);
      if (item) return this.t(item.labelKey);
    }
    return null;
  }

  private segmentLabel(segment: string): string {
    if (segment === 'new') return this.t('newBadge');
    if (segment === 'areas') return this.t('remoteAreas');
    return /^\d+$/.test(segment) ? `#${segment}` : decodeURIComponent(segment);
  }

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  ngOnInit(): void {
    this.currentUrl.set(this.router.url);
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe((e) => {
        this.currentUrl.set((e as NavigationEnd).urlAfterRedirects);
        this.rememberVisit((e as NavigationEnd).urlAfterRedirects);
        this.loadCounts();
        this.mobileOpen.set(false);
        this.userMenuOpen.set(false);
        this.paletteOpen.set(false);
      });

    if (!this.auth.profile()) {
      this.auth.loadProfile().subscribe();
    }

    if (isPlatformBrowser(this.platformId)) {
      this.collapsed.set(localStorage.getItem('iwn-admin-collapsed') === '1');
      this.density.set(localStorage.getItem('iwn-admin-density') === 'compact' ? 'compact' : 'comfortable');
      try {
        const stored = JSON.parse(localStorage.getItem('iwn-admin-recent') ?? '[]');
        if (Array.isArray(stored)) this.recentLinks.set(stored.filter((x) => typeof x === 'string').slice(0, 4));
      } catch {
        /* ignore corrupt storage */
      }
    }
    this.loadCounts();
  }

  toggleCollapsed(): void {
    const next = !this.collapsed();
    this.collapsed.set(next);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('iwn-admin-collapsed', next ? '1' : '0');
    }
  }

  initials(): string {
    const name = this.profile()?.fullName || this.profile()?.email || 'A';
    return name
      .split(/[\s@.]+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase())
      .join('');
  }

  openPalette(): void {
    this.paletteQuery.set('');
    this.paletteIndex.set(0);
    this.paletteOpen.set(true);
    this.userMenuOpen.set(false);
  }

  closePalette(): void {
    this.paletteOpen.set(false);
  }

  openPaletteItem(link: string): void {
    this.closePalette();
    void this.router.navigateByUrl(link);
  }

  toggleDensity(): void {
    const next = this.density() === 'compact' ? 'comfortable' : 'compact';
    this.density.set(next);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('iwn-admin-density', next);
    }
  }

  onPaletteKey(event: KeyboardEvent): void {
    const results = this.paletteResults();
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.paletteIndex.set(Math.min(this.activePaletteIndex() + 1, Math.max(results.length - 1, 0)));
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.paletteIndex.set(Math.max(this.activePaletteIndex() - 1, 0));
    } else if (event.key === 'Enter') {
      const item = results[this.activePaletteIndex()];
      if (!item) return;
      event.preventDefault();
      this.openPaletteItem(item.link);
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.closePalette();
    }
  }

  logout(): void {
    this.auth.logout();
  }

  @HostListener('document:keydown', ['$event'])
  onDocKey(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      if (this.paletteOpen()) this.closePalette();
      else this.openPalette();
      return;
    }
    if (event.key === 'Escape' && this.paletteOpen()) {
      this.closePalette();
    } else if (event.key === 'Escape' && this.userMenuOpen()) {
      this.userMenuOpen.set(false);
    }
  }

  @HostListener('document:click', ['$event'])
  onDocClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.user-menu')) this.userMenuOpen.set(false);
  }
}
