import { DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminServiceOfferListItem } from '../../../core/models/admin-service-offer.models';
import { AdminServiceOfferService } from '../../../core/services/admin-service-offer.service';
import { LocaleService } from '../../../core/services/locale.service';
import { formatIwnCurrency } from '../../../core/utils/iwn-currency';
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
  templateUrl: './admin-service-offers.component.html',
  styleUrl: './admin-service-offers.component.css',
})
export class AdminServiceOffersComponent implements OnInit {
  private readonly offersApi = inject(AdminServiceOfferService);
  private readonly route = inject(ActivatedRoute);
  readonly locale = inject(LocaleService);

  readonly items = signal<AdminServiceOfferListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);
  readonly filterServiceId = signal<number | null>(null);

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'serviceTitle', header: this.t('reviewService'), sortable: true },
      { key: 'promoCode', header: this.t('promoCode'), hideOnMobile: true },
      { key: 'discount', header: this.t('promoDiscount') },
      { key: 'dates', header: this.t('promoDates'), hideOnMobile: true },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
    ];
  }

  discountLabel(item: AdminServiceOfferListItem): string {
    const type = String(item.discountType);
    if (type === 'Percentage' || type === '1') {
      return `${item.discountValue}%`;
    }
    return formatIwnCurrency(item.discountValue, this.locale);
  }

  ngOnInit(): void {
    const serviceIdParam = this.route.snapshot.queryParamMap.get('serviceSubscriptionId');
    if (serviceIdParam) {
      const id = Number(serviceIdParam);
      if (id) this.filterServiceId.set(id);
    }
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.offersApi.list(this.filterServiceId() ?? undefined).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadServiceOffersError'));
        this.loading.set(false);
      },
    });
  }

  clearServiceFilter(): void {
    this.filterServiceId.set(null);
    this.load();
  }

  toggle(item: AdminServiceOfferListItem): void {
    this.busyId.set(item.id);
    this.message.set(null);
    this.offersApi.toggle(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((o) => (o.id === item.id ? { ...o, isActive: !o.isActive } : o))
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

  remove(item: AdminServiceOfferListItem): void {
    if (!confirm(this.t('confirmDeleteServiceOffer'))) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.offersApi.delete(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((o) => o.id !== item.id));
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
