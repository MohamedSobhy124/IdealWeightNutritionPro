import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminPromoCodeListItem } from '../../../core/models/admin-promo.models';
import { AdminPromoService } from '../../../core/services/admin-promo.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  CAMPAIGN_TABS,
  CAMPAIGN_VARIANT,
  campaignState,
  type CampaignState,
  type CampaignTab,
} from '../../../core/utils/campaign-status';

import { formatIwnCurrency } from '../../../core/utils/iwn-currency';import {
  UiActionMenuComponent,
  UiAlertComponent,
  UiBadgeComponent,
  type UiBadgeVariant,
  type UiMenuItem,
  type UiTableTab,
  UiCellDirective,
  UiDataTableComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
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
  templateUrl: './admin-promo-codes.component.html',
  styleUrl: './admin-promo-codes.component.css',
})
export class AdminPromoCodesComponent implements OnInit {
  private readonly promoApi = inject(AdminPromoService);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);

  readonly items = signal<AdminPromoCodeListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);

  get cols(): UiTableColumn[] {
    return [
      { key: 'code', header: this.t('promoCode'), sortable: true, mobile: 'primary', hideable: false },
      { key: 'description', header: this.t('description'), sortable: true, hiddenByDefault: true },
      { key: 'discount', header: this.t('promoDiscount'), noExport: true },
      { key: 'dates', header: this.t('promoDates'), noExport: true },
      { key: 'usage', header: this.t('promoUsage'), align: 'end' },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: '', noExport: true, hideable: false, width: '3.5rem', align: 'end', mobile: 'actions' },
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

  readonly view = signal<CampaignTab>('all');

  readonly tabs = computed<UiTableTab[]>(() => {
    this.locale.isArabic();
    const counts: Record<CampaignTab, number> = { live: 0, scheduled: 0, ended: 0, off: 0, all: 0 };
    for (const item of this.items()) {
      counts[campaignState(item)]++;
      counts.all++;
    }
    const labels: Record<CampaignTab, AdminUiKey> = {
      live: 'campaignLive',
      scheduled: 'campaignScheduled',
      ended: 'campaignEnded',
      off: 'campaignOff',
      all: 'filterAll',
    };
    return CAMPAIGN_TABS.map((value) => ({ value, label: this.t(labels[value]), count: counts[value] }));
  });

  readonly visible = computed(() => {
    const view = this.view();
    return view === 'all' ? this.items() : this.items().filter((item) => campaignState(item) === view);
  });

  onTab(value: string): void {
    this.view.set((CAMPAIGN_TABS as readonly string[]).includes(value) ? (value as CampaignTab) : 'live');
  }

  state(item: AdminPromoCodeListItem): CampaignState {
    return campaignState(item);
  }

  stateVariant(item: AdminPromoCodeListItem): UiBadgeVariant {
    return CAMPAIGN_VARIANT[campaignState(item)];
  }

  stateLabel(item: AdminPromoCodeListItem): string {
    const key: Record<CampaignState, AdminUiKey> = {
      live: 'campaignLive',
      scheduled: 'campaignScheduled',
      ended: 'campaignEnded',
      off: 'campaignOff',
    };
    return this.t(key[campaignState(item)]);
  }

  rowMenu(item: AdminPromoCodeListItem): UiMenuItem[] {
    return [
      { id: 'edit', label: this.t('edit'), icon: 'edit', link: ['/admin/promo-codes', item.id] },
      item.isActive
        ? { id: 'toggle', label: this.t('turnOff'), icon: 'pause_circle' }
        : { id: 'toggle', label: this.t('turnOn'), icon: 'play_circle' },
      { id: 'delete', label: this.t('remove'), icon: 'delete_outline', danger: true, dividerBefore: true },
    ];
  }

  onRowAction(id: string, item: AdminPromoCodeListItem): void {
    if (id === 'toggle') this.toggle(item);
    if (id === 'delete') void this.remove(item);
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
    this.promoApi.toggle(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((p) => (p.id === item.id ? { ...p, isActive: !p.isActive } : p))
        );
        this.toast.success(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }

  async remove(item: AdminPromoCodeListItem): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('confirmDeletePromo'), item.code + '. ' + this.t('actionCannotBeUndone'), this.t('remove')))) return;
    this.busyId.set(item.id);
    this.promoApi.delete(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((p) => p.id !== item.id));
        this.toast.success(this.t('updated'));
        this.busyId.set(null);
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }
}
