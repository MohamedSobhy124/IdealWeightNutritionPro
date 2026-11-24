import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import {
  AdminStockNotificationListItem,
  AdminStockNotificationService,
} from '../../../core/services/admin-stock-notification.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiActionMenuComponent,
  UiAlertComponent,
  UiPageHeaderComponent,
  type UiMenuItem,
  type UiTableColumn,
  type UiTableTab,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiActionMenuComponent,
    UiAlertComponent,
  ],
  templateUrl: './admin-stock-notifications.component.html',
  styleUrl: './admin-stock-notifications.component.css',
})
export class AdminStockNotificationsComponent implements OnInit {
  private readonly api = inject(AdminStockNotificationService);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);

  readonly items = signal<AdminStockNotificationListItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  /** waiting = active and not yet notified; active = all active; all = everything. */
  readonly view = signal<'waiting' | 'active' | 'all'>('waiting');
  readonly deactivatingId = signal<number | null>(null);

  search = '';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  readonly tabs = computed<UiTableTab[]>(() => {
    this.locale.isArabic();
    return [
      { value: 'waiting', label: this.t('waiting') },
      { value: 'active', label: this.t('activeOnly') },
      { value: 'all', label: this.t('filterAll') },
    ];
  });

  rowMenu(item: AdminStockNotificationListItem): UiMenuItem[] {
    return [
      { id: 'product', label: this.t('productLabel'), icon: 'inventory_2', link: ['/admin/products', item.productId] },
      { id: 'deactivate', label: this.t('deactivate'), icon: 'notifications_off', danger: true, dividerBefore: true, hidden: !item.isActive },
    ];
  }

  onRowAction(id: string, item: AdminStockNotificationListItem): void {
    if (id === 'deactivate') void this.deactivate(item);
  }

  onTab(value: string): void {
    this.view.set(value === 'all' || value === 'active' ? value : 'waiting');
    this.applyFilters();
  }

  onSearch(term: string): void {
    if (term === this.search) return;
    this.search = term;
    this.applyFilters();
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'product', header: this.t('productLabel'), mobile: 'primary', hideable: false },
      { key: 'email', header: this.t('email'), mobile: 'secondary' },
      { key: 'phoneNumber', header: this.t('phone'), hideOnMobile: true },
      { key: 'createdDate', header: this.t('date'), hideOnMobile: true },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: '', noExport: true, hideable: false, width: '3.5rem', align: 'end', mobile: 'actions' },
    ];
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .list({
        activeOnly: this.view() !== 'all',
        pendingOnly: this.view() === 'waiting',
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
          this.error.set(this.t('loadStockNotificationsError'));
          this.loading.set(false);
        },
      });
  }

  applyFilters(): void {
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

  async deactivate(item: AdminStockNotificationListItem): Promise<void> {
    const confirmed = await this.confirmDialog.ask({
      title: this.t('confirmDeactivateStockNotification'),
      message: `${item.email} · ${item.productTitle}`,
      confirmLabel: this.t('deactivate'),
      danger: true,
      icon: 'notifications_off',
    });
    if (!confirmed) return;

    this.deactivatingId.set(item.id);
    this.api.deactivate(item.id).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.deactivatingId.set(null);
        this.load();
      },
      error: () => {
        this.toast.error(this.t('deactivateStockNotificationError'));
        this.deactivatingId.set(null);
      },
    });
  }
}
