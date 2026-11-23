import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs/operators';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { UiCardComponent, UiSkeletonComponent } from '../../shared/ui';

interface AccountNavItem {
  labelKey: UiKey;
  link: string;
  exact: boolean;
  icon: string;
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-account-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UiCardComponent, UiSkeletonComponent],
  templateUrl: './account-shell.component.html',
  styleUrl: './account-shell.component.css',
})
export class AccountShellComponent {
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);
  private readonly router = inject(Router);
  private readonly path = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.router.url.split(/[?#]/)[0]),
    ),
    { initialValue: this.router.url.split(/[?#]/)[0] },
  );

  /** Wishlist and order tracking sit under the switcher only on the overview, so sub-pages stay short. */
  readonly showMobileShortcuts = computed(() => this.path() === '/account');

  /** Pages rendered inside this shell. */
  readonly sections: AccountNavItem[] = [
    { labelKey: 'myAccount', link: '/account', exact: true, icon: 'account_circle' },
    { labelKey: 'myOrders', link: '/account/orders', exact: false, icon: 'receipt_long' },
    { labelKey: 'myReturns', link: '/account/returns', exact: false, icon: 'assignment_return' },
    { labelKey: 'mySubscriptions', link: '/account/subscriptions', exact: false, icon: 'workspace_premium' },
  ];

  /** Destinations that leave the account area, kept from the old hub. */
  readonly shortcuts: AccountNavItem[] = [
    { labelKey: 'wishlistTitle', link: '/wishlist', exact: false, icon: 'favorite_border' },
    { labelKey: 'trackOrder', link: '/track', exact: false, icon: 'local_shipping' },
  ];

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }
}
