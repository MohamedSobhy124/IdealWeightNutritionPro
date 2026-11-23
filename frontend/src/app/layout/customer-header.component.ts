import { A11yModule } from '@angular/cdk/a11y';
import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, ElementRef, HostListener, inject, PLATFORM_ID, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { UI, UiKey } from '../core/i18n/ui-text';
import { AuthService } from '../core/services/auth.service';
import { CartService } from '../core/services/cart.service';
import { LocaleService } from '../core/services/locale.service';
import { WishlistService } from '../core/services/wishlist.service';
import { NotificationBellComponent } from '../shared/notification-bell/notification-bell.component';
import { UiThemeToggleComponent } from '../shared/ui/ui-theme-toggle.component';
import { CUSTOMER_PRIMARY_NAV } from './customer-nav';
import { CustomerShellState } from './customer-shell.state';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-customer-header',
  standalone: true,
  imports: [A11yModule, RouterLink, RouterLinkActive, FormsModule, NotificationBellComponent, UiThemeToggleComponent],
  templateUrl: './customer-header.component.html',
  styleUrl: './customer-header.component.css',
})
export class CustomerHeaderComponent {
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);
  readonly wishlist = inject(WishlistService);
  readonly locale = inject(LocaleService);
  readonly shell = inject(CustomerShellState);
  private readonly platformId = inject(PLATFORM_ID);

  readonly primaryNav = CUSTOMER_PRIMARY_NAV;
  readonly headerScrolled = signal(false);
  private readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      this.headerScrolled.set(window.scrollY > 6);
    }
    effect(() => {
      if (!isPlatformBrowser(this.platformId) || !this.shell.searchOpen()) return;
      const input = this.searchInput()?.nativeElement;
      if (!input) return;
      queueMicrotask(() => input.focus());
    });
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  cartAriaLabel(): string {
    const count = this.cart.itemCount();
    return count > 0 ? this.t('cartWithCount').replace('{count}', String(count)) : this.t('cart');
  }

  wishlistAriaLabel(): string {
    const count = this.wishlist.count();
    return count > 0 ? this.t('wishlistWithCount').replace('{count}', String(count)) : this.t('wishlist');
  }

  @HostListener('document:keydown.escape')
  onSearchEscape(): void {
    if (this.shell.searchOpen()) this.shell.searchOpen.set(false);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    if (!target?.closest('.user-menu')) this.shell.userMenuOpen.set(false);
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.headerScrolled.set(window.scrollY > 6);
  }
}
