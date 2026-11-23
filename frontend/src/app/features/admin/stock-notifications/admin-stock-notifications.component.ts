import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import {
  AdminStockNotificationListItem,
  AdminStockNotificationService,
} from '../../../core/services/admin-stock-notification.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiFilterBarComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    FormsModule,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiFilterBarComponent,
  ],
  templateUrl: './admin-stock-notifications.component.html',
  styleUrl: './admin-stock-notifications.component.css',
})
export class AdminStockNotificationsComponent implements OnInit {
  private readonly api = inject(AdminStockNotificationService);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);

  readonly items = signal<AdminStockNotificationListItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly deactivatingId = signal<number | null>(null);

  activeOnly = true;
  pendingOnly = false;
  search = '';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'product', header: this.t('productLabel') },
      { key: 'email', header: this.t('email') },
      { key: 'phoneNumber', header: this.t('phone'), hideOnMobile: true },
      { key: 'createdDate', header: this.t('date'), hideOnMobile: true },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
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
        activeOnly: this.activeOnly,
        pendingOnly: this.pendingOnly,
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

  clearFilters(): void {
    this.activeOnly = true;
    this.pendingOnly = false;
    this.search = '';
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
      confirmLabel: this.t('confirmAction'),
      danger: true,
      icon: 'notifications_off',
    });
    if (!confirmed) return;

    this.deactivatingId.set(item.id);
    this.message.set(null);
    this.api.deactivate(item.id).subscribe({
      next: (res) => {
        this.message.set(res.message);
        this.deactivatingId.set(null);
        this.load();
      },
      error: () => {
        this.error.set(this.t('deactivateStockNotificationError'));
        this.deactivatingId.set(null);
      },
    });
  }
}
