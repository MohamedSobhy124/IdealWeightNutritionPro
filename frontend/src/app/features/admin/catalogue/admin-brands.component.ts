import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminBrand, UpsertAdminBrandRequest } from '../../../core/models/admin-catalogue.models';
import { AdminCatalogueService } from '../../../core/services/admin-catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiBadgeComponent,
  UiCardComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiFilterBarComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  type UiTableColumn,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    FormsModule,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiFilterBarComponent,
  ],
  templateUrl: './admin-brands.component.html',
  styleUrl: './admin-brands.component.css',
})
export class AdminBrandsComponent implements OnInit {
  private readonly catalogueApi = inject(AdminCatalogueService);
  readonly locale = inject(LocaleService);

  readonly items = signal<AdminBrand[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);
  readonly showForm = signal(false);
  readonly editingId = signal<number | null>(null);

  includeDeleted = false;
  name = '';
  nameAr = '';
  description = '';
  descriptionAr = '';
  imageUrl = '';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'title', header: this.t('title'), sortable: true },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true },
    ];
  }

  brandName(b: AdminBrand): string {
    return this.locale.pick(b.name, b.nameAr);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.catalogueApi.listBrands(this.includeDeleted).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadBrandsError'));
        this.loading.set(false);
      },
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.name = '';
    this.nameAr = '';
    this.description = '';
    this.descriptionAr = '';
    this.imageUrl = '';
    this.showForm.set(true);
    this.message.set(null);
  }

  openEdit(item: AdminBrand): void {
    this.editingId.set(item.id);
    this.name = item.name;
    this.nameAr = item.nameAr;
    this.description = item.description ?? '';
    this.descriptionAr = item.descriptionAr ?? '';
    this.imageUrl = item.imageUrl ?? '';
    this.showForm.set(true);
    this.message.set(null);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submit(): void {
    if (!this.name.trim() || !this.nameAr.trim()) {
      this.message.set(this.t('brandRequiredFields'));
      return;
    }

    const request: UpsertAdminBrandRequest = {
      name: this.name.trim(),
      nameAr: this.nameAr.trim(),
      description: this.description.trim() || undefined,
      descriptionAr: this.descriptionAr.trim() || undefined,
      imageUrl: this.imageUrl.trim() || undefined,
    };

    const editId = this.editingId();
    this.busyId.set(editId ?? -1);
    this.message.set(null);

    const op =
      editId != null
        ? this.catalogueApi.updateBrand(editId, request)
        : this.catalogueApi.createBrand(request);

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

  remove(item: AdminBrand): void {
    if (!confirm(this.t('confirmDeleteBrand'))) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.catalogueApi.deleteBrand(item.id).subscribe({
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
