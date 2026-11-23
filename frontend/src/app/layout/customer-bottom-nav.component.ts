import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { UI, UiKey } from '../core/i18n/ui-text';
import { AuthService } from '../core/services/auth.service';
import { CartService } from '../core/services/cart.service';
import { LocaleService } from '../core/services/locale.service';
import { WishlistService } from '../core/services/wishlist.service';

interface BottomNavItem {
  labelKey: UiKey;
  link: string;
  exact: boolean;
  icon: string;
  activeIcon: string;
  badge: number;
  ariaLabel: string | null;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-customer-bottom-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './customer-bottom-nav.component.html',
  styleUrl: './customer-bottom-nav.component.css',
})
export class CustomerBottomNavComponent {
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);
  readonly wishlist = inject(WishlistService);
  readonly locale = inject(LocaleService);

  readonly items = computed<BottomNavItem[]>(() => {
    const authed = this.auth.isAuthenticated();
    return [
      { labelKey: 'navHome', link: '/', exact: true, icon: 'home', activeIcon: 'home', badge: 0, ariaLabel: null },
      { labelKey: 'shop', link: '/shop', exact: false, icon: 'grid_view', activeIcon: 'grid_view', badge: 0, ariaLabel: null },
      {
        labelKey: 'wishlist',
        link: authed ? '/wishlist' : '/auth/login',
        exact: false,
        icon: 'favorite_border',
        activeIcon: 'favorite',
        badge: this.wishlist.count(),
        ariaLabel: this.wishlistAriaLabel(),
      },
      {
        labelKey: 'cart',
        link: '/cart',
        exact: false,
        icon: 'shopping_cart',
        activeIcon: 'shopping_cart',
        badge: this.cart.itemCount(),
        ariaLabel: this.cartAriaLabel(),
      },
      {
        labelKey: 'account',
        link: authed ? '/account' : '/auth/login',
        exact: false,
        icon: 'person_outline',
        activeIcon: 'person',
        badge: 0,
        ariaLabel: null,
      },
    ];
  });

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  private cartAriaLabel(): string {
    const count = this.cart.itemCount();
    return count > 0 ? this.t('cartWithCount').replace('{count}', String(count)) : this.t('cart');
  }

  private wishlistAriaLabel(): string {
    const count = this.wishlist.count();
    return count > 0 ? this.t('wishlistWithCount').replace('{count}', String(count)) : this.t('wishlist');
  }
}
