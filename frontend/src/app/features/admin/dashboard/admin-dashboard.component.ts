import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ChartData } from 'chart.js';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminDashboard } from '../../../core/models/admin-dashboard.models';
import { AdminDashboardService } from '../../../core/services/admin-dashboard.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiBadgeComponent,
  UiBadgeVariant,
  UiCardComponent,
  UiChartComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiEmptyStateComponent,
  UiPageHeaderComponent,
  UiStatCardComponent,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiStatCardComponent,
    UiCardComponent,
    UiChartComponent,
    UiBadgeComponent,
    UiEmptyStateComponent,
    UiDataTableComponent,
    UiCellDirective,
  ],
  templateUrl: './admin-dashboard.component.html',
})
export class AdminDashboardComponent implements OnInit {
  private readonly dashboardApi = inject(AdminDashboardService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly data = signal<AdminDashboard | null>(null);

  readonly quickActions = [
    { labelKey: 'newProduct' as AdminUiKey, link: '/admin/products/new', icon: 'add_box' },
    { labelKey: 'navOrders' as AdminUiKey, link: '/admin/orders', icon: 'receipt_long' },
    { labelKey: 'navFlashSales' as AdminUiKey, link: '/admin/flash-sales/new', icon: 'bolt' },
    { labelKey: 'navPromoCodes' as AdminUiKey, link: '/admin/promo-codes/new', icon: 'confirmation_number' },
    { labelKey: 'navBlog' as AdminUiKey, link: '/admin/blog/new', icon: 'article' },
    { labelKey: 'navReturns' as AdminUiKey, link: '/admin/returns', icon: 'assignment_return' },
  ];

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
          backgroundColor: ['#f59e0b', '#3b82f6', '#8b5cf6', '#059669', '#ef4444'],
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
          backgroundColor: '#059669',
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

  get recentOrderCols(): UiTableColumn[] {
    return [
      { key: 'id', header: this.t('orderNumber'), sortable: true },
      { key: 'customerName', header: this.t('customer') },
      { key: 'orderStatus', header: this.t('productStatus') },
      { key: 'paymentStatus', header: this.t('payment') },
      { key: 'orderTotal', header: this.t('totalLabel'), align: 'end' },
    ];
  }

  statusVariant(status: string): UiBadgeVariant {
    const s = (status || '').toLowerCase();
    if (s.includes('deliver') || s.includes('paid') || s.includes('approve') || s.includes('complete')) return 'success';
    if (s.includes('cancel') || s.includes('fail') || s.includes('reject') || s.includes('refund')) return 'danger';
    if (s.includes('ship')) return 'info';
    if (s.includes('process')) return 'brand';
    if (s.includes('pending')) return 'warning';
    return 'neutral';
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
