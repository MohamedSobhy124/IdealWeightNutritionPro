import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { UnsavedChangesForm } from '../../../core/guards/unsaved-changes-form';
import { Brand, Category } from '../../../core/models/catalogue.models';
import { AdminProductService } from '../../../core/services/admin-product.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  UiCardComponent,
  UiEmptyStateComponent,
  UiErrorSummaryComponent,
  UiFormBarComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  type UiFormError,
} from '../../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiEmptyStateComponent,
    UiSkeletonComponent,
    UiErrorSummaryComponent,
    UiFormBarComponent,
  ],
  templateUrl: './admin-product-create.component.html',
  styleUrl: './admin-product-create.component.css',
})
export class AdminProductCreateComponent extends UnsavedChangesForm implements OnInit {
  private readonly productsApi = inject(AdminProductService);
  private readonly catalogue = inject(CatalogueService);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);
  private readonly toast = inject(UiToastService);

  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly formErrors = signal<UiFormError[]>([]);

  title = '';
  titleAr = '';
  description = '';
  slug = '';
  categoryId: number | null = null;
  brandId: number | null = null;
  price = 0;
  listPrice = 0;
  storeCost: number | null = null;
  stockQuantity = 0;
  minimumStockAlert = 5;
  isNew = false;
  isTrending = false;
  allowFreeDelivery = false;
  freeDeliveryMinimumAmount = 0;
  expiryDate = '';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  ngOnInit(): void {
    this.catalogue.listCategories().subscribe({
      next: (categories: Category[]) => {
        this.categories.set(categories);
        if (categories.length > 0) {
          this.categoryId = categories[0].id;
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadCategoriesError'));
        this.loading.set(false);
      },
    });

    this.catalogue.listBrands().subscribe({
      next: (brands: Brand[]) => this.brands.set(brands),
    });
  }

  submit(): void {
    const errors = this.validate();
    this.formErrors.set(errors);
    const categoryId = this.categoryId;
    if (errors.length || categoryId == null) return;

    this.submitting.set(true);

    this.productsApi
      .create({
        title: this.title.trim(),
        titleAr: this.titleAr.trim() || undefined,
        description: this.description.trim() || undefined,
        slug: this.slug.trim() || undefined,
        categoryId,
        brandId: this.brandId ?? undefined,
        price: this.price,
        listPrice: this.listPrice > 0 ? this.listPrice : this.price,
        storeCost: this.storeCost,
        stockQuantity: this.stockQuantity,
        minimumStockAlert: this.minimumStockAlert,
        isNew: this.isNew,
        isTrending: this.isTrending,
        allowFreeDelivery: this.allowFreeDelivery,
        freeDeliveryMinimumAmount: this.freeDeliveryMinimumAmount,
        expiryDate: this.expiryDate || undefined,
      })
      .subscribe({
        next: (product) => {
          this.submitting.set(false);
          this.markSaved();
          this.toast.success(this.t('productCreated'));
          void this.router.navigate(['/admin/products', product.id]);
        },
        error: (err) => {
          this.toast.error(err.error?.errors?.[0] ?? this.t('saveFailed'));
          this.submitting.set(false);
        },
      });
  }

  private validate(): UiFormError[] {
    const errors: UiFormError[] = [];
    const bad = (n: unknown) => n === null || n === undefined || n === '' || Number.isNaN(Number(n)) || Number(n) < 0;
    if (!this.title.trim()) errors.push({ fieldId: 'pc-title', message: this.t('titleRequired') });
    if (this.categoryId == null) errors.push({ fieldId: 'pc-category', message: this.t('categoryRequired') });
    if (bad(this.price)) errors.push({ fieldId: 'pc-price', message: this.t('priceRequired') });
    if (bad(this.stockQuantity)) errors.push({ fieldId: 'pc-stock', message: this.t('stockRequired') });
    if (this.allowFreeDelivery && bad(this.freeDeliveryMinimumAmount)) {
      errors.push({ fieldId: 'pc-free-min', message: this.t('freeDeliveryMinimumInvalid') });
    }
    return errors;
  }

  fieldError(id: string): string | null {
    return this.formErrors().find((e) => e.fieldId === id)?.message ?? null;
  }

  cancel(): void {
    void this.router.navigate(['/admin/products']);
  }
}
