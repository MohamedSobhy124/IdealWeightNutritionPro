import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChartData } from 'chart.js';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { UI, UiKey } from '../../../core/i18n/ui-text';
import { AdminDashboard } from '../../../core/models/admin-dashboard.models';
import { AdminDashboardService } from '../../../core/services/admin-dashboard.service';
import { AuthService } from '../../../core/services/auth.service';
import { LocaleService } from '../../../core/services/locale.service';
import { statusLabel } from '../../../core/utils/status-label';
import {
  statusVariant,
  UiAlertComponent,
  UiBadgeComponent,
  UiBadgeVariant,
  UiCardComponent,
  UiChartComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiPageHeaderComponent,
  UiStatCardComponent,
  type UiTableColumn,
} from '../../../shared/ui';

interface AttentionTile {
  label: string;
  sub: string;
  value: number;
  icon: string;
  tone: 'warning' | 'info' | 'brand' | 'danger' | 'neutral';
  link: string;
  params: Record<string, string> | null;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    UiAlertComponent,
    RouterLink,
    DatePipe,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiStatCardComponent,
    UiCardComponent,
    UiChartComponent,
    UiBadgeComponent,
    UiDataTableComponent,
    UiCellDirective,
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css',
})
export class AdminDashboardComponent implements OnInit {
  private readonly dashboardApi = inject(AdminDashboardService);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly data = signal<AdminDashboard | null>(null);
  readonly isAdmin = computed(() => this.auth.profile()?.roles.includes('Admin') ?? false);

  readonly quickActions = computed(() => {
    const actions: { labelKey: AdminUiKey; link: string; icon: string; adminOnly?: boolean }[] = [
      { labelKey: 'navOrders', link: '/admin/orders', icon: 'receipt_long' },
      { labelKey: 'navReturns', link: '/admin/returns', icon: 'assignment_return' },
      { labelKey: 'newProduct', link: '/admin/products/new', icon: 'add_box', adminOnly: true },
      { labelKey: 'navFlashSales', link: '/admin/flash-sales/new', icon: 'bolt', adminOnly: true },
      { labelKey: 'navPromoCodes', link: '/admin/promo-codes/new', icon: 'confirmation_number', adminOnly: true },
      { labelKey: 'navBlog', link: '/admin/blog/new', icon: 'article', adminOnly: true },
    ];
    return actions.filter((action) => this.isAdmin() || !action.adminOnly);
  });

  readonly orderStatusChart = computed<ChartData<'doughnut'>>(() => {
    const d = this.data();
    return {
      labels: [
        this.t('filterPending'),
        this.t('filterProcessing'),
        this.t('filterShipped'),
        this.t('filterDelivered'),
        this.t('filterCancelled'),
      ],
      datasets: [
        {
          data: d
            ? [d.ordersPending, d.ordersProcessing, d.ordersShipped, d.ordersDelivered, d.ordersCancelled]
            : [],
          // Chart.js needs HEX (not CSS vars). Nutrition brand chart palette.
          backgroundColor: ['#B45309', '#175CD3', '#6BBA98', '#0B6A56', '#B42318'],
          borderWidth: 0,
        },
      ],
    };
  });

  readonly operationsChart = computed<ChartData<'bar'>>(() => {
    const d = this.data();
    return {
      labels: [
        this.t('pendingReturns'),
        this.t('lowStockProducts'),
        this.t('pendingStockNotifications'),
        this.t('servicePurchasesTotal'),
      ],
      datasets: [
        {
          label: '',
          data: d
            ? [d.pendingReturns, d.lowStockProducts, d.pendingStockNotifications, d.servicePurchasesPending]
            : [],
          backgroundColor: 'rgb(11 106 86 / 0.85)',
          borderRadius: 8,
          maxBarThickness: 48,
        },
      ],
    };
  });

  readonly doughnutOptions = {
    cutout: '64%',
    plugins: { legend: { position: 'bottom' as const } },
  };

  readonly barOptions = {
    plugins: { legend: { display: false } },
    scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
  };

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  labelStatus(status: string): string {
    return statusLabel(status, (key: UiKey) => this.locale.pick(UI[key].en, UI[key].ar));
  }

  get recentOrderCols(): UiTableColumn[] {
    return [
      { key: 'id', header: this.t('orderNumber'), sortable: true },
      { key: 'customerName', header: this.t('customer') },
      { key: 'orderStatus', header: this.t('productStatus') },
      { key: 'paymentStatus', header: this.t('payment') },
      { key: 'orderTotal', header: this.t('totalLabel'), align: 'end' },
    ];
  }

  readonly statusVariant: (status: string) => UiBadgeVariant = statusVariant;

  /** "Needs attention" tiles: each links to the list already filtered to that work. */
  attention(d: AdminDashboard): AttentionTile[] {
    const items: AttentionTile[] = [
      { label: this.t('filterPending'), sub: this.t('navOrders'), value: d.ordersPending, icon: 'pending_actions', tone: 'warning', link: '/admin/orders', params: { status: 'Pending' } },
      { label: this.t('pendingReturns'), sub: this.t('navReturns'), value: d.pendingReturns, icon: 'assignment_return', tone: 'info', link: '/admin/returns', params: null },
      { label: this.t('pendingStockNotifications'), sub: this.t('navStockNotifications'), value: d.pendingStockNotifications, icon: 'notifications_active', tone: 'brand', link: '/admin/stock-notifications', params: null },
    ];
    if (this.isAdmin()) {
      items.push(
        { label: this.t('lowStockProducts'), sub: d.outOfStockProducts + ' ' + this.t('outOfStockProducts'), value: d.lowStockProducts, icon: 'inventory', tone: 'danger', link: '/admin/products', params: { filter: 'lowstock' } },
        { label: this.t('servicePurchasesTotal'), sub: this.t('filterPending'), value: d.servicePurchasesPending, icon: 'shopping_bag', tone: 'neutral', link: '/admin/service-purchases', params: null },
      );
    }
    return items;
  }

  ngOnInit(): void {
    this.dashboardApi.getDashboard().subscribe({
      next: (dashboard) => {
        this.data.set(dashboard);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadDashboardError'));
        this.loading.set(false);
      },
    });
  }
}
