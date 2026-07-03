import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { CatalogueService } from '../../core/services/catalogue.service';
import { LocaleService } from '../../core/services/locale.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { CartItem, CartPromo } from '../../core/models/cart.models';
import { UiCardComponent, UiEmptyStateComponent, UiFormFieldComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [RouterLink, FormsModule, IwnCurrencyPipe, UiPageHeaderComponent, UiSkeletonComponent, UiEmptyStateComponent, UiCardComponent, UiFormFieldComponent],
  templateUrl: './cart-page.component.html',
  styleUrl: './cart-page.component.css',
})
export class CartPageComponent implements OnInit {
  readonly cartService = inject(CartService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly items = signal<CartItem[]>([]);
  readonly subtotal = signal(0);
  readonly discount = signal(0);
  readonly total = signal(0);
  readonly appliedPromo = signal<CartPromo | null>(null);
  readonly loading = signal(true);
  readonly updating = signal(false);
  readonly error = signal<string | null>(null);
  readonly promoError = signal<string | null>(null);

  promoCode = '';

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
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
  }): void {
    this.items.set(cart.items);
    this.subtotal.set(cart.subtotal);
    this.discount.set(cart.discount ?? 0);
    this.total.set(cart.total ?? cart.subtotal);
    this.appliedPromo.set(cart.appliedPromo ?? null);
  }

  refresh(): void {
    this.error.set(null);
    this.loading.set(true);
    this.cartService.load().subscribe({
      next: (cart) => {
        this.applyCart(cart);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('cartLoadError'));
        this.loading.set(false);
      },
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
    this.updating.set(true);
    this.cartService.removeItem(item.lineId).subscribe({
      next: (cart) => {
        this.applyCart(cart);
        this.updating.set(false);
      },
      error: () => this.updating.set(false),
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
