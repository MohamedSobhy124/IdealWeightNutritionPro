import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminFlashSaleListItem } from '../../../core/models/admin-flash-sale.models';
import { AdminFlashSaleService } from '../../../core/services/admin-flash-sale.service';
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
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiActionMenuComponent,
    UiAlertComponent,
  ],
  templateUrl: './admin-flash-sales.component.html',
  styleUrl: './admin-flash-sales.component.css',
})
export class AdminFlashSalesComponent implements OnInit {
  private readonly flashApi = inject(AdminFlashSaleService);
  readonly locale = inject(LocaleService);
  readonly catalogue = inject(CatalogueService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);

  readonly items = signal<AdminFlashSaleListItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'title', header: this.t('title'), sortable: true, mobile: 'primary', hideable: false, value: (row) => this.displayName(row as unknown as AdminFlashSaleListItem) },
      { key: 'dates', header: this.t('promoDates'), hideOnMobile: true },
      { key: 'productCount', header: this.t('items'), align: 'end' },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: '', noExport: true, hideable: false, width: '3.5rem', align: 'end', mobile: 'actions' },
    ];
  }

  displayName(item: AdminFlashSaleListItem): string {
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

  state(item: AdminFlashSaleListItem): CampaignState {
    return campaignState(item);
  }

  stateVariant(item: AdminFlashSaleListItem): UiBadgeVariant {
    return CAMPAIGN_VARIANT[campaignState(item)];
  }

  stateLabel(item: AdminFlashSaleListItem): string {
    const key: Record<CampaignState, AdminUiKey> = {
      live: 'campaignLive',
      scheduled: 'campaignScheduled',
      ended: 'campaignEnded',
      off: 'campaignOff',
    };
    return this.t(key[campaignState(item)]);
  }

  rowMenu(item: AdminFlashSaleListItem): UiMenuItem[] {
    return [
      { id: 'edit', label: this.t('edit'), icon: 'edit', link: ['/admin/flash-sales', item.id] },
      item.isActive
        ? { id: 'toggle', label: this.t('turnOff'), icon: 'pause_circle' }
        : { id: 'toggle', label: this.t('turnOn'), icon: 'play_circle' },
      { id: 'delete', label: this.t('remove'), icon: 'delete_outline', danger: true, dividerBefore: true },
    ];
  }

  onRowAction(id: string, item: AdminFlashSaleListItem): void {
    if (id === 'toggle') this.toggle(item);
    if (id === 'delete') void this.remove(item);
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
    this.flashApi.toggle(item.id).subscribe({
      next: () => {
        this.items.update((list) =>
          list.map((f) => (f.id === item.id ? { ...f, isActive: !f.isActive } : f))
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

  async remove(item: AdminFlashSaleListItem): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('confirmDeleteFlashSale'), this.displayName(item) + '. ' + this.t('actionCannotBeUndone'), this.t('remove')))) return;
    this.busyId.set(item.id);
    this.flashApi.delete(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((f) => f.id !== item.id));
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
