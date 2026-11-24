import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { CartService } from '../../core/services/cart.service';
import { CatalogueService } from '../../core/services/catalogue.service';
import { LocaleService } from '../../core/services/locale.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { AddCartItemRequest, CartFreeDelivery, CartItem, CartPromo } from '../../core/models/cart.models';
import { formatIwnCurrency } from '../../core/utils/iwn-currency';
import { UiEmptyStateComponent, UiSkeletonComponent, UiToastService } from '../../shared/ui';

/** Cart-only copy not (yet) in the shared ui-text dictionary. */
const COPY = {
  itemsOne: { en: '1 item', ar: 'منتج واحد' },
  itemsMany: { en: '{n} items', ar: '{n} منتجات' },
  stepDone: { en: 'Done', ar: 'تأكيد' },
  shippingAtCheckout: { en: 'Calculated at checkout', ar: 'يُحتسب عند إتمام الطلب' },
  emptyHint: {
    en: 'Looks like you have not added anything yet. Explore our original supplements and healthy picks.',
    ar: 'يبدو أنك لم تضف أي منتج بعد. تصفّح مكملاتنا الأصلية واختياراتنا الصحية.',
  },
  weAccept: { en: 'We accept', ar: 'نقبل' },
  progress: { en: 'Checkout progress', ar: 'مراحل الطلب' },
} as const;

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [RouterLink, FormsModule, IwnCurrencyPipe, UiSkeletonComponent, UiEmptyStateComponent],
  templateUrl: './cart-page.component.html',
  styleUrl: './cart-page.component.css',
})
export class CartPageComponent implements OnInit {
  readonly cartService = inject(CartService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);
  private readonly toast = inject(UiToastService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly items = signal<CartItem[]>([]);
  readonly subtotal = signal(0);
  readonly discount = signal(0);
  readonly total = signal(0);
  readonly appliedPromo = signal<CartPromo | null>(null);
  readonly freeDelivery = signal<CartFreeDelivery | null>(null);

  /** 0–100: how close the subtotal is to the free-delivery threshold. */
  readonly freeDeliveryProgress = computed(() => {
    const fd = this.freeDelivery();
    const threshold = fd?.threshold ?? 0;
    if (!fd?.available || threshold <= 0) return 100;
    return Math.min(100, Math.max(0, Math.round((this.subtotal() / threshold) * 100)));
  });

  readonly freeDeliveryRemainingText = computed(() =>
    this.t('freeDeliveryRemaining').replace(
      '{amount}',
      formatIwnCurrency(this.freeDelivery()?.amountRemaining ?? 0, this.locale),
    ),
  );
  readonly loading = signal(true);
  readonly updating = signal(false);
  readonly error = signal<string | null>(null);
  readonly promoError = signal<string | null>(null);

  promoCode = '';

  /** View-only: total units in the cart, for the header count. */
  readonly itemCount = computed(() => this.items().reduce((sum, item) => sum + item.quantity, 0));
  readonly paymentBadges = ['Visa', 'Mastercard', 'Apple Pay', 'Tabby', 'Tamara'] as const;

  copy(key: keyof typeof COPY): string {
    return this.locale.pick(COPY[key].en, COPY[key].ar);
  }

  countLabel(): string {
    const n = this.itemCount();
    return n === 1 ? this.copy('itemsOne') : this.copy('itemsMany').replace('{n}', String(n));
  }

  lineLink(item: CartItem): (string | number)[] {
    return item.comboOfferId ? ['/combos', item.comboOfferId] : ['/product', item.slug];
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  lineTitle(item: CartItem): string {
    return this.locale.pick(item.title, item.titleAr);
  }

  ngOnInit(): void {
    this.refresh();
  }

  private applyCart(cart: {
    items: CartItem[];
    subtotal: number;
    discount?: number;
    total?: number;
    appliedPromo?: CartPromo;
    freeDelivery?: CartFreeDelivery | null;
  }): void {
    this.items.set(cart.items);
    this.subtotal.set(cart.subtotal);
    this.discount.set(cart.discount ?? 0);
    this.total.set(cart.total ?? cart.subtotal);
    this.appliedPromo.set(cart.appliedPromo ?? null);
    this.freeDelivery.set(cart.freeDelivery ?? null);
  }

  refresh(): void {
    this.error.set(null);
    this.loading.set(true);
    if (!isPlatformBrowser(this.platformId)) return;
    this.cartService.load().subscribe({
      next: (cart) => {
        this.applyCart(cart);
        this.fillMissingArabicTitles(cart.items);
      },
      error: () => {
        this.error.set(this.t('cartLoadError'));
        this.loading.set(false);
      },
    });
  }

  private fillMissingArabicTitles(items: CartItem[]): void {
    const missing = items.filter((item) => item.titleAr == null && !item.comboOfferId && item.slug);
    if (missing.length === 0) {
      this.loading.set(false);
      return;
    }

    forkJoin(
      missing.map((item) =>
        this.catalogue.getProductBySlug(item.slug).pipe(
          map((product) => ({ lineId: item.lineId, titleAr: product.titleAr })),
          catchError(() => of(null)),
        ),
      ),
    ).subscribe((rows) => {
      const titles = new Map(
        rows.flatMap((row) => (row?.titleAr ? [[row.lineId, row.titleAr] as const] : [])),
      );
      if (titles.size > 0) {
        this.items.update((current) =>
          current.map((item) => (titles.has(item.lineId) ? { ...item, titleAr: titles.get(item.lineId) } : item)),
        );
      }
      this.loading.set(false);
    });
  }

  applyPromo(): void {
    const code = this.promoCode.trim();
    if (!code) return;

    this.updating.set(true);
    this.promoError.set(null);
    this.cartService.applyPromo(code).subscribe({
      next: (cart) => {
        this.applyCart(cart);
        this.promoCode = '';
        this.updating.set(false);
      },
      error: (err) => {
        this.promoError.set(err.error?.errors?.[0] ?? this.t('invalidPromo'));
        this.updating.set(false);
      },
    });
  }

  removePromo(): void {
    this.updating.set(true);
    this.cartService.removePromo().subscribe({
      next: (cart) => {
        this.applyCart(cart);
        this.updating.set(false);
      },
      error: () => this.updating.set(false),
    });
  }

  changeQty(item: CartItem, quantity: number): void {
    this.updating.set(true);
    this.cartService.updateQuantity(item.lineId, quantity).subscribe({
      next: (cart) => {
        this.applyCart(cart);
        this.updating.set(false);
      },
      error: () => this.updating.set(false),
    });
  }

  remove(item: CartItem): void {
    const request: AddCartItemRequest = {
      productId: item.productId,
      quantity: item.quantity,
      productVariantId: item.productVariantId,
      flashSaleItemId: item.flashSaleItemId,
      comboOfferId: item.comboOfferId,
    };
    this.updating.set(true);
    this.cartService.removeItem(item.lineId).subscribe({
      next: (cart) => {
        this.applyCart(cart);
        this.updating.set(false);
        this.toast.info(this.t('cartItemRemoved'), {
          duration: 6000,
          actionLabel: this.t('undo'),
          action: () => this.restore(request),
        });
      },
      error: () => this.updating.set(false),
    });
  }

  private restore(request: AddCartItemRequest): void {
    this.updating.set(true);
    this.cartService.addItem(request).subscribe({
      next: (cart) => {
        this.applyCart(cart);
        this.updating.set(false);
      },
      error: () => {
        this.updating.set(false);
        this.toast.error(this.t('cartRestoreFailed'));
      },
    });
  }

  clearCart(): void {
    this.updating.set(true);
    this.cartService.clear().subscribe({
      next: (cart) => {
        this.applyCart(cart);
        this.updating.set(false);
      },
      error: () => this.updating.set(false),
    });
  }
}
