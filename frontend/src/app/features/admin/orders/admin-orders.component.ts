import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminOrderListItem, AdminOrderStatistics } from '../../../core/models/admin-order.models';
import { AdminOrderService } from '../../../core/services/admin-order.service';
import { AuthService } from '../../../core/services/auth.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiPageHeaderComponent,
  UiDataTableComponent,
  UiCellDirective,
  UiBadgeComponent,
  type UiBadgeVariant,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    IwnCurrencyPipe,
    FormsModule,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
  ],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.css',
})
export class AdminOrdersComponent implements OnInit {
  private readonly ordersApi = inject(AdminOrderService);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly isAdmin = computed(
    () => this.auth.profile()?.roles.includes('Admin') ?? false
  );
  readonly orders = signal<AdminOrderListItem[]>([]);
  readonly stats = signal<AdminOrderStatistics | null>(null);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(50);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly exporting = signal(false);
  readonly exportingProfits = signal(false);

  status = '';
  paymentStatus = '';
  paymentMethod = '';
  dateFrom = '';
  dateTo = '';
  search = '';

  readonly orderStatusOptions = ['', 'Pending', 'Paid', 'Processing', 'Shipped', 'Delivered', 'Cancelled'];
  readonly paymentStatusOptions = ['', 'Pending', 'Approved', 'Rejected', 'Refunded'];
  readonly paymentMethodOptions = ['', 'COD', 'Geidea', 'Tamara', 'Tabby'];

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'id', header: this.t('orderNumber'), sortable: true },
      { key: 'customerName', header: this.t('customer') },
      { key: 'city', header: this.t('city'), hideOnMobile: true },
      { key: 'orderStatus', header: this.t('productStatus') },
      { key: 'paymentStatus', header: this.t('payment') },
      { key: 'orderTotal', header: this.t('totalLabel'), align: 'end' },
    ];
  }

  statusVariant(status: string): UiBadgeVariant {
    const x = (status || '').toLowerCase();
    if (
      x.includes('deliver') ||
      x.includes('paid') ||
      x.includes('approve') ||
      x.includes('active') ||
      x.includes('complete')
    )
      return 'success';
    if (
      x.includes('cancel') ||
      x.includes('fail') ||
      x.includes('reject') ||
      x.includes('refund') ||
      x.includes('deleted') ||
      x.includes('out')
    )
      return 'danger';
    if (x.includes('ship')) return 'info';
    if (x.includes('process')) return 'brand';
    if (x.includes('pending') || x.includes('low')) return 'warning';
    return 'neutral';
  }

  ngOnInit(): void {
    if (!this.auth.profile()) {
      this.auth.loadProfile().subscribe();
    }
    this.loadStats();
    this.load();
  }

  loadStats(): void {
    this.ordersApi.getStatistics().subscribe({
      next: (stats) => this.stats.set(stats),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.ordersApi
      .listOrders({
        status: this.status || undefined,
        paymentStatus: this.paymentStatus || undefined,
        paymentMethod: this.paymentMethod || undefined,
        dateFrom: this.dateFrom || undefined,
        dateTo: this.dateTo || undefined,
        search: this.search || undefined,
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (res) => {
          this.orders.set(res.items);
          this.totalCount.set(res.totalCount);
          this.page.set(res.page);
          this.pageSize.set(res.pageSize);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.error?.errors?.[0] ?? this.t('loadOrdersError'));
          this.loading.set(false);
        },
      });
  }

  filterByStatus(status: string): void {
    this.status = status;
    this.page.set(1);
    this.load();
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.status = '';
    this.paymentStatus = '';
    this.paymentMethod = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.search = '';
    this.page.set(1);
    this.load();
  }

  prevPage(): void {
    if (this.page() <= 1) return;
    this.page.update((p) => p - 1);
    this.load();
  }

  nextPage(): void {
    const maxPage = Math.ceil(this.totalCount() / this.pageSize());
    if (this.page() >= maxPage) return;
    this.page.update((p) => p + 1);
    this.load();
  }

  totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.message.set(null);
    this.ordersApi
      .exportCsv({
        status: this.status || undefined,
        paymentStatus: this.paymentStatus || undefined,
        paymentMethod: this.paymentMethod || undefined,
        dateFrom: this.dateFrom || undefined,
        dateTo: this.dateTo || undefined,
        search: this.search || undefined,
      })
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          anchor.href = url;
          anchor.download = `orders-${new Date().toISOString().slice(0, 10)}.csv`;
          anchor.click();
          URL.revokeObjectURL(url);
          this.exporting.set(false);
        },
        error: () => {
          this.message.set(this.t('exportFailed'));
          this.exporting.set(false);
        },
      });
  }

  exportProductProfits(): void {
    this.exportingProfits.set(true);
    this.message.set(null);
    this.ordersApi
      .exportProductProfits({
        dateFrom: this.dateFrom || undefined,
        dateTo: this.dateTo || undefined,
      })
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          anchor.href = url;
          anchor.download = `product-profits-${new Date().toISOString().slice(0, 10)}.csv`;
          anchor.click();
          URL.revokeObjectURL(url);
          this.exportingProfits.set(false);
        },
        error: () => {
          this.message.set(this.t('exportFailed'));
          this.exportingProfits.set(false);
        },
      });
  }
}
