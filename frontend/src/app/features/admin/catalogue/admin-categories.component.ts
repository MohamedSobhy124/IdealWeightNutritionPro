import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminCategory, UpsertAdminCategoryRequest } from '../../../core/models/admin-catalogue.models';
import { AdminCatalogueService } from '../../../core/services/admin-catalogue.service';
import { AdminMediaService } from '../../../core/services/admin-service.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiActionMenuComponent,
  UiAlertComponent,
  UiFormFieldComponent,
  UiModalComponent,
  UiPageHeaderComponent,
  type UiMenuItem,
  type UiTableColumn,
  type UiTableTab,
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
    UiActionMenuComponent,
    UiAlertComponent,
    UiModalComponent,
  ],
  templateUrl: './admin-categories.component.html',
  styleUrl: './admin-categories.component.css',
})
export class AdminCategoriesComponent implements OnInit {
  private readonly catalogueApi = inject(AdminCatalogueService);
  private readonly mediaApi = inject(AdminMediaService);
  readonly catalogue = inject(CatalogueService);
  readonly uploadingImage = signal(false);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);

  readonly items = signal<AdminCategory[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  /** Validation/save problem shown inside the drawer. */
  readonly formError = signal<string | null>(null);
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
        hideable: false,
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
      { key: 'actions', header: '', noExport: true, hideable: false, width: '3.5rem', align: 'end', mobile: 'actions' },
    ];
  }

  categoryName(c: AdminCategory): string {
    return this.locale.pick(c.name, c.nameAr);
  }

  readonly tabs = computed<UiTableTab[]>(() => {
    this.locale.isArabic();
    return [
      { value: 'active', label: this.t('active') },
      { value: 'all', label: this.t('productFilterAll') },
    ];
  });

  readonly activeTab = signal<'active' | 'all'>('active');

  onTab(value: string): void {
    this.activeTab.set(value === 'all' ? 'all' : 'active');
    this.includeDeleted = value === 'all';
    this.load();
  }

  rowMenu(item: AdminCategory): UiMenuItem[] {
    return [
      { id: 'edit', label: this.t('edit'), icon: 'edit' },
      { id: 'delete', label: this.t('remove'), icon: 'delete_outline', danger: true, dividerBefore: true, hidden: item.isDeleted },
    ];
  }

  onRowAction(id: string, item: AdminCategory): void {
    if (id === 'edit') this.openEdit(item);
    if (id === 'delete') void this.remove(item);
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
    this.formError.set(null);
  }

  openEdit(item: AdminCategory): void {
    this.editingId.set(item.id);
    this.name = item.name;
    this.nameAr = item.nameAr;
    this.description = item.description;
    this.descriptionAr = item.descriptionAr;
    this.imageUrl = item.imageUrl ?? '';
    this.showForm.set(true);
    this.formError.set(null);
  }

  cancelForm(): void {
    this.showForm.set(false);
    this.editingId.set(null);
  }

  submit(): void {
    if (!this.name.trim() || !this.nameAr.trim()) {
      this.formError.set(this.t('categoryRequiredFields'));
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
    this.formError.set(null);

    const op =
      editId != null
        ? this.catalogueApi.updateCategory(editId, request)
        : this.catalogueApi.createCategory(request);

    op.subscribe({
      next: () => {
        this.showForm.set(false);
        this.editingId.set(null);
        this.busyId.set(null);
        this.toast.success(this.t('saved'));
        this.load();
      },
      error: () => {
        this.formError.set(this.t('saveFailed'));
        this.busyId.set(null);
      },
    });
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const id = this.editingId();
    if (!file || id == null) return;
    this.uploadingImage.set(true);
    this.formError.set(null);
    this.mediaApi.uploadCategoryImage(id, file).subscribe({
      next: (res) => {
        this.imageUrl = res.imageUrl;
        this.uploadingImage.set(false);
        this.load();
      },
      error: (err) => {
        this.formError.set(err?.error?.errors?.[0] ?? this.t('uploadFailed'));
        this.uploadingImage.set(false);
      },
    });
    input.value = '';
  }

  async remove(item: AdminCategory): Promise<void> {
    if (!(await this.confirmDialog.danger(this.t('confirmDeleteCategory'), this.categoryName(item) + '. ' + this.t('actionCannotBeUndone'), this.t('remove')))) return;
    this.busyId.set(item.id);
    this.formError.set(null);
    this.catalogueApi.deleteCategory(item.id).subscribe({
      next: () => {
        this.toast.success(this.t('updated'));
        this.busyId.set(null);
        this.load();
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.busyId.set(null);
      },
    });
  }
}
