import { isPlatformBrowser } from '@angular/common';
import {
  Component,
  computed,
  HostListener,
  inject,
  OnInit,
  PLATFORM_ID,
  signal,
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

@Component({
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    FormsModule,
    NotificationBellComponent,
    UiThemeToggleComponent,
  ],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.css',
})
export class AdminShellComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  readonly locale = inject(LocaleService);

  readonly isAdmin = computed(
    () => this.auth.profile()?.roles.includes('Admin') ?? false
  );
  readonly profile = this.auth.profile;

  readonly collapsed = signal(false);
  readonly mobileOpen = signal(false);
  readonly userMenuOpen = signal(false);
  readonly searchQuery = signal('');
  readonly currentUrl = signal('/admin/dashboard');

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

  readonly searchResults = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    if (!q) return [];
    const results: NavItem[] = [];
    for (const group of this.visibleGroups()) {
      for (const item of group.items) {
        if (this.t(item.labelKey).toLowerCase().includes(q)) results.push(item);
      }
    }
    return results.slice(0, 8);
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

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  ngOnInit(): void {
    this.currentUrl.set(this.router.url);
    this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe((e) => {
        this.currentUrl.set((e as NavigationEnd).urlAfterRedirects);
        this.mobileOpen.set(false);
        this.userMenuOpen.set(false);
      });

    const check = (profile: { roles: string[] } | null) => {
      const allowed =
        profile &&
        (profile.roles.includes('Admin') || profile.roles.includes('Employee'));
      if (!allowed) this.router.navigate(['/account']);
    };

    if (!this.auth.profile()) {
      this.auth.loadProfile().subscribe({ next: check });
    } else {
      check(this.auth.profile());
    }

    if (isPlatformBrowser(this.platformId)) {
      this.collapsed.set(localStorage.getItem('iwn-admin-collapsed') === '1');
    }
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

  onSearchSelect(): void {
    this.searchQuery.set('');
  }

  logout(): void {
    this.auth.logout();
  }

  @HostListener('document:click', ['$event'])
  onDocClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.user-menu')) this.userMenuOpen.set(false);
    if (!target.closest('.shell-search')) this.searchQuery.set('');
  }
}
