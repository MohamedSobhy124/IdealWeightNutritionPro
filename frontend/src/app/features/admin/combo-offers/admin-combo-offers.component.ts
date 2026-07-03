import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminComboOfferListItem } from '../../../core/models/admin-combo-offer.models';
import { AdminComboOfferService } from '../../../core/services/admin-combo-offer.service';
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
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
  ],
  templateUrl: './admin-combo-offers.component.html',
  styleUrl: './admin-combo-offers.component.css',
})
export class AdminComboOffersComponent implements OnInit {
  private readonly comboApi = inject(AdminComboOfferService);
  readonly locale = inject(LocaleService);

  readonly items = signal<AdminComboOfferListItem[]>([]);
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
      { key: 'comboPrice', header: this.t('comboPrice'), align: 'end' },
      { key: 'dates', header: this.t('promoDates'), hideOnMobile: true },
      { key: 'productCount', header: this.t('items'), align: 'end' },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
    ];
  }

  displayName(item: AdminComboOfferListItem): string {
    return this.locale.pick(item.name, item.nameAr ?? item.name);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.comboApi.list().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadComboOffersError'));
        this.loading.set(false);
      },
    });
  }

  toggle(item: AdminComboOfferListItem): void {
    this.busyId.set(item.id);
    this.message.set(null);
    this.comboApi.toggle(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((c) => (c.id === item.id ? { ...c, isActive: !c.isActive } : c))
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

  remove(item: AdminComboOfferListItem): void {
    if (!confirm(this.t('confirmDeleteComboOffer'))) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.comboApi.delete(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((c) => c.id !== item.id));
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
