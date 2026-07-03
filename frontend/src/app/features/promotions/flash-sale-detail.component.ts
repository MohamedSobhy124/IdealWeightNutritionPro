import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { CatalogueService } from '../../core/services/catalogue.service';
import { FlashSaleService } from '../../core/services/flash-sale.service';
import { LocaleService } from '../../core/services/locale.service';
import { FlashSaleCountdownComponent } from './flash-sale-countdown.component';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { FlashSaleDetail, FlashSaleItem } from '../../core/models/flash-sale.models';
import {
  isVariableProduct,
  productQuickAddButtonLabel,
  quickAddProduct,
  shouldNavigateToProductForQuickAdd,
} from '../../core/utils/product-quick-add';

import { UiEmptyStateComponent, UiPageHeaderComponent, UiProductCardComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    FlashSaleCountdownComponent,
    UiPageHeaderComponent,
    UiProductCardComponent,
    UiSkeletonComponent,
    UiEmptyStateComponent,
  ],
  templateUrl: './flash-sale-detail.component.html',
  styleUrl: './flash-sale-detail.component.css',
})
export class FlashSaleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly flashSales = inject(FlashSaleService);
  private readonly cart = inject(CartService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly sale = signal<FlashSaleDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly adding = signal(false);
  readonly cartMessage = signal<string | null>(null);
  readonly cartMessageItemId = signal<number | null>(null);

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  saleName(sale: FlashSaleDetail): string {
    return this.locale.pick(sale.name, sale.nameAr ?? '');
  }

  itemTitle(item: FlashSaleItem): string {
    return item.title;
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set(this.t('flashSaleNotFound'));
      this.loading.set(false);
      return;
    }

    this.flashSales.getActive(id).subscribe({
      next: (sale) => {
        this.sale.set(sale);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('flashSaleNotFound'));
        this.loading.set(false);
      },
    });
  }

  addLabel(item: FlashSaleItem): string {
    if (this.adding()) {
      return this.t('adding');
    }

    const inStock = item.availableQuantity > 0;
    if (inStock && isVariableProduct(item.productType) && !item.productVariantId) {
      return this.t('chooseVariant');
    }

    return productQuickAddButtonLabel(
      { inStock, productType: item.productType },
      { addToCart: this.t('addToCart'), chooseVariant: this.t('chooseVariant') },
    );
  }

  addToCart(item: FlashSaleItem): void {
    const product = {
      id: item.productId,
      slug: item.slug,
      inStock: item.availableQuantity > 0,
      productType: item.productType,
      displayVariantId: item.productVariantId,
    };

    if (shouldNavigateToProductForQuickAdd(product, { pinnedVariantId: item.productVariantId })) {
      void this.router.navigate(['/product', item.slug]);
      return;
    }

    this.adding.set(true);
    this.cartMessage.set(null);

    quickAddProduct(this.router, this.cart, product, {
      pinnedVariantId: item.productVariantId,
      flashSaleItemId: item.id,
      onAdded: () => {
        this.cartMessageItemId.set(item.id);
        this.cartMessage.set(this.t('addedFlashPrice'));
        this.adding.set(false);
      },
      onFailed: () => {
        this.cartMessageItemId.set(item.id);
        this.cartMessage.set(this.t('addToCartFailed'));
        this.adding.set(false);
      },
    });
  }
}
