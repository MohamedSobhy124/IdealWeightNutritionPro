import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminCity, UpsertAdminCityRequest } from '../../../core/models/admin-delivery.models';
import { AdminDeliveryService } from '../../../core/services/admin-delivery.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiBadgeComponent,
  UiCardComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
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
      { key: 'city', header: this.t('city'), sortable: true },
      { key: 'emirate', header: this.t('emirate'), hideOnMobile: true },
      { key: 'deliveryCharge', header: this.t('deliveryCharge'), align: 'end' },
      { key: 'remoteAreas', header: this.t('remoteAreas'), hideOnMobile: true },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
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

  remove(item: AdminCity): void {
    if (!confirm(this.t('confirmDeleteCity'))) return;
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
