import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import {
  AdminCity,
  AdminRemoteArea,
  UpsertAdminRemoteAreaRequest,
} from '../../../core/models/admin-delivery.models';
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
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
  ],
  templateUrl: './admin-city-areas.component.html',
  styleUrl: './admin-city-areas.component.css',
})
export class AdminCityAreasComponent implements OnInit {
  private readonly deliveryApi = inject(AdminDeliveryService);
  private readonly route = inject(ActivatedRoute);
  readonly locale = inject(LocaleService);

  readonly city = signal<AdminCity | null>(null);
  readonly items = signal<AdminRemoteArea[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);
  readonly showForm = signal(false);
  readonly editingId = signal<number | null>(null);

  cityId: number | null = null;
  name = '';
  nameAr = '';
  deliveryCharge = 0;
  isActive = true;
  displayOrder = 0;

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'title', header: this.t('title'), sortable: true },
      { key: 'deliveryCharge', header: this.t('deliveryCharge'), align: 'end' },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
    ];
  }

  areaName(a: AdminRemoteArea): string {
    return this.locale.pick(a.name, a.nameAr ?? a.name);
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set(this.t('invalidCity'));
      this.loading.set(false);
      return;
    }
    this.cityId = id;
    this.load();
  }

  load(): void {
    if (!this.cityId) return;
    this.loading.set(true);
    this.error.set(null);
    this.deliveryApi.listCities().subscribe({
      next: (cities) => {
        const found = cities.find((c) => c.id === this.cityId) ?? null;
        this.city.set(found);
        if (!found) {
          this.error.set(this.t('cityNotFound'));
          this.loading.set(false);
          return;
        }
        this.deliveryApi.listRemoteAreas(this.cityId!).subscribe({
          next: (areas) => {
            this.items.set(areas);
            this.loading.set(false);
          },
          error: () => {
            this.error.set(this.t('loadRemoteAreasError'));
            this.loading.set(false);
          },
        });
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
    this.deliveryCharge = 0;
    this.isActive = true;
    this.displayOrder = 0;
    this.showForm.set(true);
    this.message.set(null);
  }

  openEdit(item: AdminRemoteArea): void {
    this.editingId.set(item.id);
    this.name = item.name;
    this.nameAr = item.nameAr ?? '';
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
    if (!this.cityId || !this.name.trim()) {
      this.message.set(this.t('remoteAreaRequiredFields'));
      return;
    }

    const request: UpsertAdminRemoteAreaRequest = {
      name: this.name.trim(),
      nameAr: this.nameAr.trim() || undefined,
      deliveryCharge: this.deliveryCharge,
      isActive: this.isActive,
      displayOrder: this.displayOrder,
    };

    const editId = this.editingId();
    this.busyId.set(editId ?? -1);
    this.message.set(null);

    const op =
      editId != null
        ? this.deliveryApi.updateRemoteArea(this.cityId, editId, request)
        : this.deliveryApi.createRemoteArea(this.cityId, request);

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

  remove(item: AdminRemoteArea): void {
    if (!confirm(this.t('confirmDeleteRemoteArea'))) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.deliveryApi.deleteRemoteArea(item.id).subscribe({
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
