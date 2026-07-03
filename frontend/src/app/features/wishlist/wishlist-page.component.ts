import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import {
  ProductCardPricing,
  resolveProductCardPricing,
} from '../../core/utils/product-card-pricing';
import {
  isVariableProduct,
  quickAddProduct,
  shouldNavigateToProductForQuickAdd,
} from '../../core/utils/product-quick-add';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { FlashSaleProductPrice } from '../../core/models/flash-sale.models';
import { WishlistItem } from '../../core/models/wishlist.models';
import { CartService } from '../../core/services/cart.service';
import { CatalogueService } from '../../core/services/catalogue.service';
import { FlashSaleService } from '../../core/services/flash-sale.service';
import { LocaleService } from '../../core/services/locale.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { UiEmptyStateComponent, UiPageHeaderComponent, UiProductCardComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [RouterLink, UiPageHeaderComponent, UiEmptyStateComponent, UiSkeletonComponent, UiProductCardComponent],
  templateUrl: './wishlist-page.component.html',
  styleUrl: './wishlist-page.component.css',
})
export class WishlistPageComponent implements OnInit {
  private readonly wishlistApi = inject(WishlistService);
  private readonly cart = inject(CartService);
  private readonly router = inject(Router);
  private readonly flashSalesApi = inject(FlashSaleService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly items = signal<WishlistItem[]>([]);
  readonly flashPrices = signal<FlashSaleProductPrice[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly busyId = signal<number | null>(null);
  readonly message = signal<string | null>(null);

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  itemTitle(item: WishlistItem): string {
    return this.locale.pick(item.title, item.title);
  }

  cardPricing(item: WishlistItem): ProductCardPricing {
    return resolveProductCardPricing(
      {
        id: item.productId,
        price: item.price,
        listPrice: item.listPrice,
        displayVariantId: null,
      },
      this.flashPrices()
    );
  }

  ngOnInit(): void {
    forkJoin({
      wishlist: this.wishlistApi.load(),
      flashPrices: this.flashSalesApi
        .listProductPrices()
        .pipe(catchError(() => of([] as FlashSaleProductPrice[]))),
    }).subscribe({
      next: ({ wishlist, flashPrices }) => {
        this.items.set(wishlist.items);
        this.flashPrices.set(flashPrices);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('wishlistFailed'));
        this.loading.set(false);
      },
    });
  }

  refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.wishlistApi.load().subscribe({
      next: (res) => {
        this.items.set(res.items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('wishlistFailed'));
        this.loading.set(false);
      },
    });
  }

  remove(item: WishlistItem): void {
    this.busyId.set(item.id);
    this.message.set(null);
    this.wishlistApi.remove(item.id).subscribe({
      next: () => {
        this.items.update((list) => list.filter((i) => i.id !== item.id));
        this.wishlistApi.loadProductIds().subscribe();
        this.busyId.set(null);
      },
      error: () => {
        this.message.set(this.t('wishlistFailed'));
        this.busyId.set(null);
      },
    });
  }

  cartActionLabel(item: WishlistItem): string {
    if (!item.inStock) {
      return this.t('outOfStock');
    }

    if (isVariableProduct(item.productType)) {
      return this.t('chooseVariant');
    }

    return this.t('moveToCart');
  }

  addToCart(item: WishlistItem): void {
    if (!item.inStock) {
      return;
    }

    const product = {
      id: item.productId,
      slug: item.slug,
      inStock: item.inStock,
      productType: item.productType,
    };

    if (!shouldNavigateToProductForQuickAdd(product)) {
      this.busyId.set(item.id);
    }

    this.message.set(null);

    quickAddProduct(this.router, this.cart, product, {
      onAdded: () => {
        this.message.set(this.t('addedToCart'));
        this.busyId.set(null);
      },
      onFailed: () => {
        this.message.set(this.t('addToCartFailed'));
        this.busyId.set(null);
      },
    });
  }
}
