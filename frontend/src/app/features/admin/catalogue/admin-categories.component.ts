import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminCategory, UpsertAdminCategoryRequest } from '../../../core/models/admin-catalogue.models';
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
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiFilterBarComponent,
    UiModalComponent,
  ],
  templateUrl: './admin-categories.component.html',
  styleUrl: './admin-categories.component.css',
})
export class AdminCategoriesComponent implements OnInit {
  private readonly catalogueApi = inject(AdminCatalogueService);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);

  readonly items = signal<AdminCategory[]>([]);
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
        value: (row) => this.categoryName(row as unknown as AdminCategory),
        searchText: (row) => {
          const item = row as unknown as AdminCategory;
          return `${item.name} ${item.nameAr}`;
        },
      },
      {
        key: 'status',
        header: this.t('productStatus'),
        mobile: 'secondary',
        value: (row) => ((row as unknown as AdminCategory).isDeleted ? this.t('deleted') : this.t('active')),
      },
      { key: 'actions', header: this.t('actions'), noExport: true, hideOnMobile: true, mobile: 'actions' },
    ];
  }

  categoryName(c: AdminCategory): string {
    return this.locale.pick(c.name, c.nameAr);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.catalogueApi.listCategories(this.includeDeleted).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadCategoriesError'));
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

  openEdit(item: AdminCategory): void {
    this.editingId.set(item.id);
    this.name = item.name;
    this.nameAr = item.nameAr;
    this.description = item.description;
    this.descriptionAr = item.descriptionAr;
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
      this.message.set(this.t('categoryRequiredFields'));
      return;
    }

    const request: UpsertAdminCategoryRequest = {
      name: this.name.trim(),
      nameAr: this.nameAr.trim(),
      description: this.description.trim(),
      descriptionAr: this.descriptionAr.trim(),
      imageUrl: this.imageUrl.trim() || undefined,
    };

    const editId = this.editingId();
    this.busyId.set(editId ?? -1);
    this.message.set(null);

    const op =
      editId != null
        ? this.catalogueApi.updateCategory(editId, request)
        : this.catalogueApi.createCategory(request);

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

  async remove(item: AdminCategory): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('confirmDeleteCategory'), this.t('actionCannotBeUndone')))) return;
    this.busyId.set(item.id);
    this.message.set(null);
    this.catalogueApi.deleteCategory(item.id).subscribe({
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
