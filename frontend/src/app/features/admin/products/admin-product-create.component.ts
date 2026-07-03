import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { Brand, Category } from '../../../core/models/catalogue.models';
import { AdminProductService } from '../../../core/services/admin-product.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
} from '../../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiEmptyStateComponent,
    UiSkeletonComponent,
  ],
  templateUrl: './admin-product-create.component.html',
  styleUrl: './admin-product-create.component.css',
})
export class AdminProductCreateComponent implements OnInit {
  private readonly productsApi = inject(AdminProductService);
  private readonly catalogue = inject(CatalogueService);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);

  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);

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
        this.error.set('Could not load categories.');
        this.loading.set(false);
      },
    });

    this.catalogue.listBrands().subscribe({
      next: (brands: Brand[]) => this.brands.set(brands),
    });
  }

  submit(): void {
    if (!this.title.trim() || this.categoryId == null) {
      this.saveError.set('Title and category are required.');
      return;
    }

    this.submitting.set(true);
    this.saveError.set(null);

    this.productsApi
      .create({
        title: this.title.trim(),
        titleAr: this.titleAr.trim() || undefined,
        description: this.description.trim() || undefined,
        slug: this.slug.trim() || undefined,
        categoryId: this.categoryId,
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
          void this.router.navigate(['/admin/products', product.id]);
        },
        error: (err) => {
          this.saveError.set(err.error?.errors?.[0] ?? 'Could not create product.');
          this.submitting.set(false);
        },
      });
  }
}
