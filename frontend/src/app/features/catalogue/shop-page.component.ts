import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { CatalogueService } from '../../core/services/catalogue.service';
import { FlashSaleService } from '../../core/services/flash-sale.service';
import { LocaleService } from '../../core/services/locale.service';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { Category, Brand, ProductListItem } from '../../core/models/catalogue.models';
import { FlashSaleProductPrice } from '../../core/models/flash-sale.models';
import {
  ProductCardPricing,
  resolveProductCardPricing,
} from '../../core/utils/product-card-pricing';
import {
  productQuickAddButtonLabel,
  quickAddProduct,
  shouldNavigateToProductForQuickAdd,
} from '../../core/utils/product-quick-add';
import {
  UiEmptyStateComponent,
  UiFilterDrawerComponent,
  UiPageHeaderComponent,
  UiPaginationComponent,
  UiProductCardComponent,
  UiQuickViewComponent,
  UiSkeletonComponent,
  UiFormFieldComponent,
} from '../../shared/ui';

@Component({
  standalone: true,
  imports: [
    ReactiveFormsModule,
    UiProductCardComponent,
    UiFilterDrawerComponent,
    UiPaginationComponent,
    UiQuickViewComponent,
    UiSkeletonComponent,
    UiPageHeaderComponent,
    UiEmptyStateComponent,
    UiFormFieldComponent,
  ],
  templateUrl: './shop-page.component.html',
  styleUrl: './shop-page.component.css',
})
export class ShopPageComponent implements OnInit {
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);
  readonly wishlist = inject(WishlistService);
  private readonly flashSalesApi = inject(FlashSaleService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly pageSize = 24;

  readonly products = signal<ProductListItem[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly wishlistBusyId = signal<number | null>(null);
  readonly quickAddBusyId = signal<number | null>(null);
  readonly flashPrices = signal<FlashSaleProductPrice[]>([]);
  readonly mobileFiltersOpen = signal(false);
  readonly quickViewProduct = signal<ProductListItem | null>(null);

  readonly filters = this.fb.nonNullable.group({
    search: [''],
    categoryId: [''],
    brandId: [''],
    sortBy: [''],
  });

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  productTitle(p: ProductListItem): string {
    return this.locale.pick(p.title, p.titleAr);
  }

  categoryName(c: Category): string {
    return this.locale.pick(c.name, c.nameAr);
  }

  brandName(b: Brand): string {
    return this.locale.pick(b.name, b.nameAr);
  }

  isInWishlist(productId: number): boolean {
    return this.wishlist.isInWishlist(productId);
  }

  cardPricing(product: ProductListItem): ProductCardPricing {
    return resolveProductCardPricing(product, this.flashPrices());
  }

  ngOnInit(): void {
    this.flashSalesApi
      .listProductPrices()
      .pipe(catchError(() => of([] as FlashSaleProductPrice[])))
      .subscribe((prices) => this.flashPrices.set(prices));

    if (this.auth.isAuthenticated()) {
      this.wishlist.loadProductIds().subscribe();
    }

    this.catalogue.listCategories().subscribe({
      next: (cats) => this.categories.set(cats),
      error: () => this.categories.set([]),
    });

    this.catalogue.listBrands().subscribe({
      next: (brands) => this.brands.set(brands),
      error: () => this.brands.set([]),
    });

    this.route.queryParamMap.subscribe((params) => {
      const pageParam = Number(params.get('page') ?? '1');
      this.page.set(Number.isFinite(pageParam) && pageParam > 0 ? pageParam : 1);
      this.filters.patchValue({
        search: params.get('search') ?? '',
        categoryId: params.get('categoryId') ?? '',
        brandId: params.get('brandId') ?? '',
        sortBy: params.get('sortBy') ?? '',
      });
      this.load();
    });
  }

  toggleWishlist(event: Event, productId: number): void {
    event.preventDefault();
    event.stopPropagation();
    if (!this.auth.isAuthenticated()) return;
    this.wishlistBusyId.set(productId);
    this.wishlist.toggle(productId).subscribe({
      next: () => this.wishlistBusyId.set(null),
      error: () => this.wishlistBusyId.set(null),
    });
  }

  quickAddLabel(product: ProductListItem): string {
    return productQuickAddButtonLabel(product, {
      addToCart: this.t('addToCart'),
      chooseVariant: this.t('chooseVariant'),
    });
  }

  quickAdd(product: ProductListItem): void {
    if (!shouldNavigateToProductForQuickAdd(product)) {
      this.quickAddBusyId.set(product.id);
    }

    quickAddProduct(this.router, this.cart, product, {
      onAdded: () => this.quickAddBusyId.set(null),
      onFailed: () => this.quickAddBusyId.set(null),
    });
  }

  openQuickView(product: ProductListItem): void {
    this.quickViewProduct.set(product);
  }

  closeQuickView(): void {
    this.quickViewProduct.set(null);
  }

  quickViewAdd(): void {
    const product = this.quickViewProduct();
    if (!product) return;
    this.quickAdd(product);
    this.closeQuickView();
  }

  applyFilters(): void {
    this.syncUrl(1);
  }

  clearFilters(): void {
    this.filters.reset({ search: '', categoryId: '', brandId: '', sortBy: '' });
    this.syncUrl(1);
  }

  applyMobileFilters(): void {
    this.mobileFiltersOpen.set(false);
    this.syncUrl(1);
  }

  goToPage(nextPage: number): void {
    this.syncUrl(nextPage);
    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  private syncUrl(page: number): void {
    const { search, categoryId, brandId, sortBy } = this.filters.getRawValue();
    const queryParams: Record<string, string> = {};
    if (search) queryParams['search'] = search;
    if (categoryId) queryParams['categoryId'] = categoryId;
    if (brandId) queryParams['brandId'] = brandId;
    if (sortBy) queryParams['sortBy'] = sortBy;
    if (page > 1) queryParams['page'] = String(page);
    this.router.navigate([], { relativeTo: this.route, queryParams });
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    const { search, categoryId, brandId, sortBy } = this.filters.getRawValue();
    const catId = categoryId ? Number(categoryId) : undefined;
    const brand = brandId ? Number(brandId) : undefined;

    this.catalogue
      .listProducts({
        search: search || undefined,
        categoryId: catId,
        brandId: brand,
        sortBy: sortBy || undefined,
        page: this.page(),
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (result) => {
          this.products.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(this.t('productsLoadError'));
          this.loading.set(false);
        },
      });
  }
}
