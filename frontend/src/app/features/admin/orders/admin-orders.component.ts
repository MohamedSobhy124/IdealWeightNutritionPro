import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { UI, UiKey } from '../../../core/i18n/ui-text';
import { AdminOrderListItem, AdminOrderStatistics } from '../../../core/models/admin-order.models';
import { AdminOrderService } from '../../../core/services/admin-order.service';
import { AuthService } from '../../../core/services/auth.service';
import { LocaleService } from '../../../core/services/locale.service';
import { statusLabel } from '../../../core/utils/status-label';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  statusVariant,
  UiActionMenuComponent,
  UiAlertComponent,
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiPageHeaderComponent,
  type UiBadgeVariant,
  type UiMenuItem,
  type UiTableColumn,
  type UiTableFilterChip,
  type UiTableTab,
} from '../../../shared/ui';

/** Status tabs: API status value ('' = all) and the statistics field that counts it. */
const STATUS_TABS: { value: string; label: AdminUiKey; stat: keyof AdminOrderStatistics }[] = [
  { value: '', label: 'filterAll', stat: 'all' },
  { value: 'Pending', label: 'filterPending', stat: 'pending' },
  { value: 'Paid', label: 'filterApproved', stat: 'approved' },
  { value: 'Processing', label: 'filterProcessing', stat: 'processing' },
  { value: 'Shipped', label: 'filterShipped', stat: 'shipped' },
  { value: 'Delivered', label: 'filterDelivered', stat: 'delivered' },
  { value: 'Cancelled', label: 'filterCancelled', stat: 'cancelled' },
];

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
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
    UiActionMenuComponent,
    UiAlertComponent,
  ],
  templateUrl: './admin-orders.component.html',
  styleUrl: './admin-orders.component.css',
})
export class AdminOrdersComponent implements OnInit {
  private readonly ordersApi = inject(AdminOrderService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(UiToastService);
  readonly locale = inject(LocaleService);

  readonly isAdmin = computed(() => this.auth.profile()?.roles.includes('Admin') ?? false);
  readonly orders = signal<AdminOrderListItem[]>([]);
  readonly stats = signal<AdminOrderStatistics | null>(null);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(50);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly exporting = signal(false);
  readonly exportingProfits = signal(false);
  readonly filtersOpen = signal(false);

  readonly status = signal('');
  /** Applied (not draft) secondary filters. */
  readonly applied = signal({ paymentStatus: '', paymentMethod: '', dateFrom: '', dateTo: '' });
  search = '';

  // Draft values bound to the filter panel; copied to `applied` on Apply.
  paymentStatus = '';
  paymentMethod = '';
  dateFrom = '';
  dateTo = '';

  readonly paymentStatusOptions = ['Pending', 'Approved', 'Rejected', 'Refunded'];
  readonly paymentMethodOptions = ['COD', 'Geidea', 'Tamara', 'Tabby'];

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  statusText(status: string): string {
    return statusLabel(status, (key: UiKey) => this.locale.pick(UI[key].en, UI[key].ar));
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'id', header: this.t('orderNumber'), sortable: true, mobile: 'primary', hideable: false },
      { key: 'customerName', header: this.t('customer'), mobile: 'secondary' },
      { key: 'city', header: this.t('city'), hideOnMobile: true },
      { key: 'orderStatus', header: this.t('productStatus') },
      { key: 'paymentStatus', header: this.t('payment') },
      { key: 'orderTotal', header: this.t('totalLabel'), align: 'end', sortable: true },
    ];
  }

  readonly tabs = computed<UiTableTab[]>(() => {
    const s = this.stats();
    this.locale.isArabic();
    return STATUS_TABS.map((tab) => ({ value: tab.value, label: this.t(tab.label), count: s ? s[tab.stat] : null }));
  });

  readonly subtitle = computed(() => {
    const s = this.stats();
    if (!s) return `${this.totalCount()} ${this.t('ordersSubtitle')}`;
    return s.pending ? `${s.all} ${this.t('ordersSubtitle')} · ${s.pending} ${this.t('toProcess')}` : `${s.all} ${this.t('ordersSubtitle')}`;
  });

  readonly chips = computed<UiTableFilterChip[]>(() => {
    const a = this.applied();
    const list: UiTableFilterChip[] = [];
    if (a.paymentStatus) list.push({ key: 'paymentStatus', label: `${this.t('paymentStatus')}: ${this.statusText(a.paymentStatus)}` });
    if (a.paymentMethod) list.push({ key: 'paymentMethod', label: `${this.t('paymentMethod')}: ${a.paymentMethod}` });
    if (a.dateFrom) list.push({ key: 'dateFrom', label: `${this.t('dateFrom')}: ${a.dateFrom}` });
    if (a.dateTo) list.push({ key: 'dateTo', label: `${this.t('dateTo')}: ${a.dateTo}` });
    return list;
  });

  readonly headerMenu = computed<UiMenuItem[]>(() => [
    {
      id: 'profits',
      label: this.exportingProfits() ? this.t('exporting') : this.t('exportProductProfits'),
      icon: 'insights',
      disabled: this.exportingProfits(),
      hidden: !this.isAdmin(),
    },
  ]);

  readonly statusVariant: (status: string) => UiBadgeVariant = statusVariant;

  ngOnInit(): void {
    if (!this.auth.profile()) this.auth.loadProfile().subscribe();
    const params = this.route.snapshot.queryParamMap;
    const st = params.get('status');
    if (st && STATUS_TABS.some((x) => x.value === st)) this.status.set(st);
    this.search = params.get('q') ?? '';
    this.loadStats();
    this.load();
  }

  loadStats(): void {
    this.ordersApi.getStatistics().subscribe({ next: (stats) => this.stats.set(stats), error: () => this.stats.set(null) });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const a = this.applied();
    this.ordersApi
      .listOrders({
        status: this.status() || undefined,
        paymentStatus: a.paymentStatus || undefined,
        paymentMethod: a.paymentMethod || undefined,
        dateFrom: a.dateFrom || undefined,
        dateTo: a.dateTo || undefined,
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

  onTab(value: string): void {
    this.status.set(value);
    this.syncUrl();
    this.reload();
  }

  onSearch(term: string): void {
    if (term === this.search) return;
    this.search = term;
    this.syncUrl();
    this.reload();
  }

  toggleFilters(): void {
    const a = this.applied();
    this.paymentStatus = a.paymentStatus;
    this.paymentMethod = a.paymentMethod;
    this.dateFrom = a.dateFrom;
    this.dateTo = a.dateTo;
    this.filtersOpen.update((v) => !v);
  }

  applyFilters(): void {
    this.applied.set({
      paymentStatus: this.paymentStatus,
      paymentMethod: this.paymentMethod,
      dateFrom: this.dateFrom,
      dateTo: this.dateTo,
    });
    this.filtersOpen.set(false);
    this.reload();
  }

  removeFilter(key: string): void {
    this.applied.update((a) => ({ ...a, [key]: '' }));
    this.reload();
  }

  clearFilters(): void {
    this.applied.set({ paymentStatus: '', paymentMethod: '', dateFrom: '', dateTo: '' });
    this.paymentStatus = this.paymentMethod = this.dateFrom = this.dateTo = '';
    this.reload();
  }

  private reload(): void {
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }

  onHeaderAction(id: string): void {
    if (id === 'profits') this.exportProductProfits();
  }

  exportCsv(): void {
    this.exporting.set(true);
    const a = this.applied();
    this.ordersApi
      .exportCsv({
        status: this.status() || undefined,
        paymentStatus: a.paymentStatus || undefined,
        paymentMethod: a.paymentMethod || undefined,
        dateFrom: a.dateFrom || undefined,
        dateTo: a.dateTo || undefined,
        search: this.search || undefined,
      })
      .subscribe({
        next: (blob) => {
          this.download(blob, `orders-${new Date().toISOString().slice(0, 10)}.csv`);
          this.exporting.set(false);
        },
        error: () => {
          this.toast.error(this.t('exportFailed'));
          this.exporting.set(false);
        },
      });
  }

  exportProductProfits(): void {
    this.exportingProfits.set(true);
    const a = this.applied();
    this.ordersApi
      .exportProductProfits({ dateFrom: a.dateFrom || undefined, dateTo: a.dateTo || undefined })
      .subscribe({
        next: (blob) => {
          this.download(blob, `product-profits-${new Date().toISOString().slice(0, 10)}.csv`);
          this.exportingProfits.set(false);
        },
        error: () => {
          this.toast.error(this.t('exportFailed'));
          this.exportingProfits.set(false);
        },
      });
  }

  private download(blob: Blob, name: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = name;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  private syncUrl(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { status: this.status() || null, q: this.search || null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
