import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminCity, UpsertAdminCityRequest } from '../../../core/models/admin-delivery.models';
import { AdminDeliveryService } from '../../../core/services/admin-delivery.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiFormFieldComponent,
  UiModalComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiFormFieldComponent,
    UiModalComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
  ],
  templateUrl: './admin-cities.component.html',
  styleUrl: './admin-cities.component.css',
})
export class AdminCitiesComponent implements OnInit {
  private readonly deliveryApi = inject(AdminDeliveryService);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);

  readonly items = signal<AdminCity[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);
  readonly showForm = signal(false);
  readonly editingId = signal<number | null>(null);

  name = '';
  nameAr = '';
  emirate = '';
  emirateAr = '';
  deliveryCharge = 0;
  isActive = true;
  displayOrder = 0;

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      {
        key: 'city',
        header: this.t('city'),
        sortable: true,
        mobile: 'primary',
        value: (row) => this.cityName(row as unknown as AdminCity),
        searchText: (row) => {
          const item = row as unknown as AdminCity;
          return `${item.name} ${item.nameAr ?? ''}`;
        },
      },
      {
        key: 'emirate',
        header: this.t('emirate'),
        hideOnMobile: true,
        value: (row) => {
          const item = row as unknown as AdminCity;
          return this.locale.pick(item.emirate, item.emirateAr ?? item.emirate);
        },
        searchText: (row) => {
          const item = row as unknown as AdminCity;
          return `${item.emirate} ${item.emirateAr ?? ''}`;
        },
      },
      { key: 'deliveryCharge', header: this.t('deliveryCharge'), align: 'end' },
      {
        key: 'remoteAreas',
        header: this.t('remoteAreas'),
        hideOnMobile: true,
        value: (row) => (row as unknown as AdminCity).remoteAreaCount ?? 0,
      },
      {
        key: 'status',
        header: this.t('productStatus'),
        mobile: 'secondary',
        value: (row) => ((row as unknown as AdminCity).isActive ? this.t('active') : this.t('inactive')),
      },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true, mobile: 'actions' },
    ];
  }

  cityName(c: AdminCity): string {
    return this.locale.pick(c.name, c.nameAr ?? c.name);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.deliveryApi.listCities().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadCitiesError'));
        this.loading.set(false);
      },
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.name = '';
    this.nameAr = '';
    this.emirate = '';
    this.emirateAr = '';
    this.deliveryCharge = 0;
    this.isActive = true;
    this.displayOrder = 0;
    this.showForm.set(true);
    this.message.set(null);
  }

  openEdit(item: AdminCity): void {
    this.editingId.set(item.id);
    this.name = item.name;
    this.nameAr = item.nameAr ?? '';
    this.emirate = item.emirate;
    this.emirateAr = item.emirateAr ?? '';
    this.deliveryCharge = item.deliveryCharge;
    this.isActive = item.isActive;
    this.displayOrder = item.displayOrder;
    this.showForm.set(true);
    this.message.set(null);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submit(): void {
    if (!this.name.trim() || !this.emirate.trim()) {
      this.message.set(this.t('cityRequiredFields'));
      return;
    }

    const request: UpsertAdminCityRequest = {
      name: this.name.trim(),
      nameAr: this.nameAr.trim() || undefined,
      emirate: this.emirate.trim(),
      emirateAr: this.emirateAr.trim() || undefined,
      deliveryCharge: this.deliveryCharge,
      isActive: this.isActive,
      displayOrder: this.displayOrder,
    };

    const editId = this.editingId();
    this.busyId.set(editId ?? -1);
    this.message.set(null);

    const op =
      editId != null
        ? this.deliveryApi.updateCity(editId, request)
        : this.deliveryApi.createCity(request);

    op.subscribe({
      next: () => {
        this.showForm.set(false);
        this.editingId.set(null);
        this.busyId.set(null);
        this.message.set(this.t('saved'));
        this.load();
      },
      error: () => {
        this.message.set(this.t('saveFailed'));
        this.busyId.set(null);
      },
    });
  }

  async remove(item: AdminCity): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('confirmDeleteCity'), this.t('actionCannotBeUndone')))) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.deliveryApi.deleteCity(item.id).subscribe({
      next: () => {
        this.message.set(this.t('updated'));
        this.busyId.set(null);
        this.load();
      },
      error: () => {
        this.message.set(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }
}
