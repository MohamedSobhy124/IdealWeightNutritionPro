import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminServiceListItem } from '../../../core/models/admin-service.models';
import { AdminServiceService } from '../../../core/services/admin-service.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
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
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiFilterBarComponent,
  ],
  templateUrl: './admin-services.component.html',
  styleUrl: './admin-services.component.css',
})
export class AdminServicesComponent implements OnInit {
  private readonly servicesApi = inject(AdminServiceService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly services = signal<AdminServiceListItem[]>([]);
  readonly showInactive = signal(true);

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'imageUrl', header: this.t('image'), noExport: true, hideOnMobile: true },
      { key: 'title', header: this.t('titleEn'), sortable: true },
      { key: 'price', header: this.t('price'), align: 'end' },
      { key: 'serviceType', header: this.t('serviceType'), hideOnMobile: true },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
    ];
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.servicesApi.list(this.showInactive()).subscribe({
      next: (items) => {
        this.services.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadServicesError'));
        this.loading.set(false);
      },
    });
  }

  toggleShowInactive(): void {
    this.showInactive.update((v) => !v);
    this.load();
  }

  toggleActive(id: number): void {
    this.servicesApi.toggle(id).subscribe({ next: () => this.load() });
  }

  async deleteService(id: number): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('confirmDeleteService'), this.t('actionCannotBeUndone')))) return;
    this.servicesApi.delete(id).subscribe({
      next: () => {
        this.toast.success(this.t('deletedToast'));
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.errors?.[0] ?? this.t('deleteFailed')),
    });
  }

  title(item: AdminServiceListItem): string {
    return this.locale.pick(item.title, item.titleAr ?? item.title);
  }
}
