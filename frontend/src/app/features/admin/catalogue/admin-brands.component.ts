import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminBrand, UpsertAdminBrandRequest } from '../../../core/models/admin-catalogue.models';
import { AdminCatalogueService } from '../../../core/services/admin-catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiFilterBarComponent,
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
    UiPageHeaderComponent,
    UiFormFieldComponent,
    UiModalComponent,
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
  private readonly confirmDialog = inject(UiConfirmService);

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
      {
        key: 'title',
        header: this.t('title'),
        sortable: true,
        mobile: 'primary',
        value: (row) => this.brandName(row as unknown as AdminBrand),
        searchText: (row) => {
          const item = row as unknown as AdminBrand;
          return `${item.name} ${item.nameAr}`;
        },
      },
      {
        key: 'status',
        header: this.t('productStatus'),
        mobile: 'secondary',
        value: (row) => ((row as unknown as AdminBrand).isDeleted ? this.t('deleted') : this.t('active')),
      },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true, mobile: 'actions' },
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

  async remove(item: AdminBrand): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('confirmDeleteBrand'), this.t('actionCannotBeUndone')))) return;
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
