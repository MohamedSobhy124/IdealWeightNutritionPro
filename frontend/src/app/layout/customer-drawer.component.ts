import { A11yModule } from '@angular/cdk/a11y';
import { ChangeDetectionStrategy, Component, HostListener, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { UI, UiKey } from '../core/i18n/ui-text';
import { AuthService } from '../core/services/auth.service';
import { LocaleService } from '../core/services/locale.service';
import { CUSTOMER_PRIMARY_NAV } from './customer-nav';
import { CustomerShellState } from './customer-shell.state';
import { UiThemeToggleComponent } from '../shared/ui/ui-theme-toggle.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-customer-drawer',
  standalone: true,
  imports: [A11yModule, FormsModule, RouterLink, RouterLinkActive, UiThemeToggleComponent],
  templateUrl: './customer-drawer.component.html',
  styleUrl: './customer-drawer.component.css',
})
export class CustomerDrawerComponent {
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);
  readonly shell = inject(CustomerShellState);
  readonly primaryNav = CUSTOMER_PRIMARY_NAV;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  /** Escape closes the drawer unless a dialog stacked above it handles the key. */
  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.shell.mobileMenuOpen()) return;
    const dialogs = document.querySelectorAll('[role="dialog"]');
    const top = dialogs[dialogs.length - 1];
    if (top && top.id !== 'mobile-nav-drawer') return;
    this.shell.mobileMenuOpen.set(false);
  }
}
