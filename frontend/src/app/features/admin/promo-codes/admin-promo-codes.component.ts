import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminPromoCodeListItem } from '../../../core/models/admin-promo.models';
import { AdminPromoService } from '../../../core/services/admin-promo.service';
import { LocaleService } from '../../../core/services/locale.service';
import { formatIwnCurrency } from '../../../core/utils/iwn-currency';import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
  ],
  templateUrl: './admin-promo-codes.component.html',
  styleUrl: './admin-promo-codes.component.css',
})
export class AdminPromoCodesComponent implements OnInit {
  private readonly promoApi = inject(AdminPromoService);
  readonly locale = inject(LocaleService);

  readonly items = signal<AdminPromoCodeListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);

  get cols(): UiTableColumn[] {
    return [
      { key: 'code', header: this.t('promoCode'), sortable: true },
      { key: 'description', header: this.t('description'), sortable: true },
      { key: 'discount', header: this.t('promoDiscount'), noExport: true },
      { key: 'dates', header: this.t('promoDates'), noExport: true },
      { key: 'usage', header: this.t('promoUsage'), align: 'end' },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
    ];
  }

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  discountLabel(item: AdminPromoCodeListItem): string {
    const type = String(item.discountType);
    if (type === 'Percentage' || type === '1') {
      return `${item.discountValue}%`;
    }
    return formatIwnCurrency(item.discountValue, this.locale);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.promoApi.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadPromoCodesError'));
        this.loading.set(false);
      },
    });
  }

  toggle(item: AdminPromoCodeListItem): void {
    this.busyId.set(item.id);
    this.message.set(null);
    this.promoApi.toggle(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((p) => (p.id === item.id ? { ...p, isActive: !p.isActive } : p))
        );
        this.message.set(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.message.set(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }

  remove(item: AdminPromoCodeListItem): void {
    if (!confirm(this.t('confirmDeletePromo'))) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.promoApi.delete(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((p) => p.id !== item.id));
        this.message.set(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.message.set(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }
}
