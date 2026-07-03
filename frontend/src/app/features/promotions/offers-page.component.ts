import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ProductListItem } from '../../core/models/catalogue.models';
import { ComboOfferSummary } from '../../core/models/combo-offer.models';
import { FlashSaleProductPrice, FlashSaleSummary } from '../../core/models/flash-sale.models';
import { CatalogueService } from '../../core/services/catalogue.service';
import { ComboOfferService } from '../../core/services/combo-offer.service';
import { FlashSaleService } from '../../core/services/flash-sale.service';
import { LocaleService } from '../../core/services/locale.service';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { WishlistService } from '../../core/services/wishlist.service';import { FlashSaleCountdownComponent } from './flash-sale-countdown.component';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
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
  UiBadgeComponent,
  UiEmptyStateComponent,
  UiPageHeaderComponent,
  UiProductCardComponent,
  UiSkeletonComponent,
  UiTabsComponent,
} from '../../shared/ui';

type OffersTab = 'discounted' | 'flash' | 'combo';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DecimalPipe,
    FlashSaleCountdownComponent,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiTabsComponent,
    UiProductCardComponent,
    UiSkeletonComponent,
    UiEmptyStateComponent,
    UiBadgeComponent,
  ],
  templateUrl: './offers-page.component.html',
  styleUrl: './offers-page.component.css',
})
export class OffersPageComponent implements OnInit {
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);
  readonly auth = inject(AuthService);
  readonly wishlist = inject(WishlistService);
  private readonly cart = inject(CartService);
  private readonly router = inject(Router);
  private readonly flashSalesApi = inject(FlashSaleService);
  private readonly combosApi = inject(ComboOfferService);

  readonly activeTab = signal<OffersTab>('discounted');
  readonly discounted = signal<ProductListItem[]>([]);
  readonly flashSales = signal<FlashSaleSummary[]>([]);
  readonly combos = signal<ComboOfferSummary[]>([]);
  readonly flashPrices = signal<FlashSaleProductPrice[]>([]);
  readonly loading = signal(true);
  readonly wishlistBusyId = signal<number | null>(null);
  readonly quickAddBusyId = signal<number | null>(null);
  readonly tabs = computed(() => {
    const items: { id: OffersTab; label: string }[] = [];
    if (this.discounted().length > 0) {
      items.push({ id: 'discounted', label: this.t('offersDiscountedTab') });
    }
    if (this.flashSales().length > 0) {
      items.push({ id: 'flash', label: this.t('offersFlashTab') });
    }
    if (this.combos().length > 0) {
      items.push({ id: 'combo', label: this.t('offersComboTab') });
    }
    return items;
  });

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  productTitle(p: ProductListItem): string {
    return this.locale.pick(p.title, p.titleAr);
  }

  flashName(sale: FlashSaleSummary): string {
    return this.locale.pick(sale.name, sale.nameAr);
  }

  comboName(combo: ComboOfferSummary): string {
    return this.locale.pick(combo.name, combo.nameAr);
  }

  setTab(tab: OffersTab): void {
    this.activeTab.set(tab);
  }

  cardPricing(product: ProductListItem): ProductCardPricing {
    return resolveProductCardPricing(product, this.flashPrices());
  }

  isInWishlist(productId: number): boolean {
    return this.wishlist.isInWishlist(productId);
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

  ngOnInit(): void {
    if (this.auth.isAuthenticated()) {
      this.wishlist.loadProductIds().subscribe();
    }

    forkJoin({      discounted: this.catalogue.listDiscountedProducts(24).pipe(catchError(() => of([] as ProductListItem[]))),
      flash: this.flashSalesApi.listActive().pipe(catchError(() => of([] as FlashSaleSummary[]))),
      flashPrices: this.flashSalesApi.listProductPrices().pipe(catchError(() => of([] as FlashSaleProductPrice[]))),
      combos: this.combosApi.listActive().pipe(catchError(() => of([] as ComboOfferSummary[]))),
    }).subscribe({
      next: ({ discounted, flash, flashPrices, combos }) => {
        this.discounted.set(discounted);
        this.flashSales.set(flash);
        this.flashPrices.set(flashPrices);
        this.combos.set(combos);
        if (discounted.length > 0) {
          this.activeTab.set('discounted');
        } else if (flash.length > 0) {
          this.activeTab.set('flash');
        } else if (combos.length > 0) {
          this.activeTab.set('combo');
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
