import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminComboOfferListItem } from '../../../core/models/admin-combo-offer.models';
import { AdminComboOfferService } from '../../../core/services/admin-combo-offer.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
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

import {
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
    DecimalPipe,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiActionMenuComponent,
    UiAlertComponent,
  ],
  templateUrl: './admin-combo-offers.component.html',
  styleUrl: './admin-combo-offers.component.css',
})
export class AdminComboOffersComponent implements OnInit {
  private readonly comboApi = inject(AdminComboOfferService);
  readonly locale = inject(LocaleService);
  readonly catalogue = inject(CatalogueService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);

  readonly items = signal<AdminComboOfferListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'title', header: this.t('title'), sortable: true, mobile: 'primary', hideable: false, value: (row) => this.displayName(row as unknown as AdminComboOfferListItem) },
      { key: 'comboPrice', header: this.t('comboPrice'), align: 'end' },
      { key: 'dates', header: this.t('promoDates'), hideOnMobile: true },
      { key: 'productCount', header: this.t('items'), align: 'end' },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: '', noExport: true, hideable: false, width: '3.5rem', align: 'end', mobile: 'actions' },
    ];
  }

  displayName(item: AdminComboOfferListItem): string {
    return this.locale.pick(item.name, item.nameAr ?? item.name);
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

  state(item: AdminComboOfferListItem): CampaignState {
    return campaignState(item);
  }

  stateVariant(item: AdminComboOfferListItem): UiBadgeVariant {
    return CAMPAIGN_VARIANT[campaignState(item)];
  }

  stateLabel(item: AdminComboOfferListItem): string {
    const key: Record<CampaignState, AdminUiKey> = {
      live: 'campaignLive',
      scheduled: 'campaignScheduled',
      ended: 'campaignEnded',
      off: 'campaignOff',
    };
    return this.t(key[campaignState(item)]);
  }

  rowMenu(item: AdminComboOfferListItem): UiMenuItem[] {
    return [
      { id: 'edit', label: this.t('edit'), icon: 'edit', link: ['/admin/combo-offers', item.id] },
      item.isActive
        ? { id: 'toggle', label: this.t('turnOff'), icon: 'pause_circle' }
        : { id: 'toggle', label: this.t('turnOn'), icon: 'play_circle' },
      { id: 'delete', label: this.t('remove'), icon: 'delete_outline', danger: true, dividerBefore: true },
    ];
  }

  onRowAction(id: string, item: AdminComboOfferListItem): void {
    if (id === 'toggle') this.toggle(item);
    if (id === 'delete') void this.remove(item);
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
    this.comboApi.toggle(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((c) => (c.id === item.id ? { ...c, isActive: !c.isActive } : c))
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

  async remove(item: AdminComboOfferListItem): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('confirmDeleteComboOffer'), this.displayName(item) + '. ' + this.t('actionCannotBeUndone'), this.t('remove')))) return;
    this.busyId.set(item.id);
    this.comboApi.delete(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((c) => c.id !== item.id));
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
