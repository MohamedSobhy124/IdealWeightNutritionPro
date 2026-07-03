import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminFlashSaleListItem } from '../../../core/models/admin-flash-sale.models';
import { AdminFlashSaleService } from '../../../core/services/admin-flash-sale.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
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
  templateUrl: './admin-flash-sales.component.html',
  styleUrl: './admin-flash-sales.component.css',
})
export class AdminFlashSalesComponent implements OnInit {
  private readonly flashApi = inject(AdminFlashSaleService);
  readonly locale = inject(LocaleService);

  readonly items = signal<AdminFlashSaleListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'title', header: this.t('title'), sortable: true },
      { key: 'dates', header: this.t('promoDates'), hideOnMobile: true },
      { key: 'productCount', header: this.t('items'), align: 'end' },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
    ];
  }

  displayName(item: AdminFlashSaleListItem): string {
    return this.locale.pick(item.name, item.nameAr ?? item.name);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.flashApi.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadFlashSalesError'));
        this.loading.set(false);
      },
    });
  }

  toggle(item: AdminFlashSaleListItem): void {
    this.busyId.set(item.id);
    this.message.set(null);
    this.flashApi.toggle(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((f) => (f.id === item.id ? { ...f, isActive: !f.isActive } : f))
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

  remove(item: AdminFlashSaleListItem): void {
    if (!confirm(this.t('confirmDeleteFlashSale'))) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.flashApi.delete(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((f) => f.id !== item.id));
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
