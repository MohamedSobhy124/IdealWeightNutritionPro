import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import {
  AdminServicePurchaseListItem,
  AdminServicePurchaseStatistics,
} from '../../../core/models/admin-service-purchase.models';
import { AdminServicePurchaseService } from '../../../core/services/admin-service-purchase.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiFilterBarComponent,
  UiPageHeaderComponent,
  UiPaginationComponent,
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
    UiFilterBarComponent,
    UiPaginationComponent,
  ],
  templateUrl: './admin-service-purchases.component.html',
  styleUrl: './admin-service-purchases.component.css',
})
export class AdminServicePurchasesComponent implements OnInit {
  private readonly purchasesApi = inject(AdminServicePurchaseService);
  readonly locale = inject(LocaleService);

  readonly items = signal<AdminServicePurchaseListItem[]>([]);
  readonly stats = signal<AdminServicePurchaseStatistics | null>(null);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly exporting = signal(false);

  paymentStatus = '';
  serviceStatus = '';
  dateFrom = '';
  dateTo = '';
  search = '';

  readonly paymentStatusOptions = ['', 'Approved', 'Pending', 'Rejected', 'Cancelled'];
  readonly serviceStatusOptions = ['', 'Active', 'Expired', 'Cancelled', 'Suspended'];

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'id', header: this.t('returnId'), sortable: true },
      { key: 'serviceTitle', header: this.t('reviewService') },
      { key: 'customerName', header: this.t('customer') },
      { key: 'totalAmount', header: this.t('totalLabel'), align: 'end', hideOnMobile: true },
      { key: 'amountPaid', header: this.t('amountPaid'), align: 'end', hideOnMobile: true },
      { key: 'paymentStatus', header: this.t('paymentStatus') },
      { key: 'serviceStatus', header: this.t('serviceStatus'), hideOnMobile: true },
      { key: 'purchaseDate', header: this.t('date'), hideOnMobile: true },
      { key: 'actions', header: this.t('actions'), noExport: true },
    ];
  }

  statusVariant(status: string): UiBadgeVariant {
    const x = (status || '').toLowerCase();
    if (x.includes('approve') || x.includes('active') || x.includes('paid')) return 'success';
    if (x.includes('reject') || x.includes('cancel') || x.includes('expired') || x.includes('suspend')) return 'danger';
    if (x.includes('pending')) return 'warning';
    return 'neutral';
  }

  ngOnInit(): void {
    this.loadStats();
    this.load();
  }

  loadStats(): void {
    this.purchasesApi.getStatistics().subscribe({
      next: (stats) => this.stats.set(stats),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.purchasesApi
      .list({
        paymentStatus: this.paymentStatus || undefined,
        serviceStatus: this.serviceStatus || undefined,
        dateFrom: this.dateFrom || undefined,
        dateTo: this.dateTo || undefined,
        search: this.search || undefined,
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (res) => {
          this.items.set(res.items);
          this.totalCount.set(res.totalCount);
          this.page.set(res.page);
          this.pageSize.set(res.pageSize);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(this.t('loadServicePurchasesError'));
          this.loading.set(false);
        },
      });
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  clearFilters(): void {
    this.paymentStatus = '';
    this.serviceStatus = '';
    this.dateFrom = '';
    this.dateTo = '';
    this.search = '';
    this.page.set(1);
    this.load();
  }

  goToPage(nextPage: number): void {
    this.page.set(nextPage);
    this.load();
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.message.set(null);
    this.purchasesApi
      .exportCsv({
        paymentStatus: this.paymentStatus || undefined,
        serviceStatus: this.serviceStatus || undefined,
        dateFrom: this.dateFrom || undefined,
        dateTo: this.dateTo || undefined,
        search: this.search || undefined,
      })
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          anchor.href = url;
          anchor.download = `service-purchases-${new Date().toISOString().slice(0, 10)}.csv`;
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
}
