import { Location } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../core/services/auth.service';
import { isTopLevelPath } from './customer-nav';

/**
 * Storefront chrome state shared by the header and the drawer.
 * Lives for the whole app session, so the mobile back button still knows
 * the navigation history while the chrome is unmounted on /admin.
 */
@Injectable({ providedIn: 'root' })
export class CustomerShellState {
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly auth = inject(AuthService);
  private navigationCount = 0;

  readonly isStaff = computed(() => {
    const roles = this.auth.profile()?.roles ?? [];
    return roles.includes('Admin') || roles.includes('Employee');
  });

  readonly mobileMenuOpen = signal(false);
  readonly searchOpen = signal(false);
  readonly userMenuOpen = signal(false);
  readonly headerSearch = signal('');
  readonly showBack = signal(!isTopLevelPath(this.router.url));

  constructor() {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.closeOverlays();
        const url = (event as NavigationEnd).urlAfterRedirects;
        this.showBack.set(!isTopLevelPath(url));
        this.navigationCount++;
      });
  }

  closeOverlays(): void {
    this.mobileMenuOpen.set(false);
    this.searchOpen.set(false);
    this.userMenuOpen.set(false);
  }

  openSearch(): void {
    this.searchOpen.set(true);
    this.mobileMenuOpen.set(false);
  }

  submitSearch(): void {
    const q = this.headerSearch().trim();
    this.closeOverlays();
    if (!q) return;
    void this.router.navigate(['/shop'], { queryParams: { search: q } });
  }

  /** Steps back through in-app history. A deep link lands on the home page. */
  goBack(): void {
    if (this.navigationCount > 1) {
      this.location.back();
    } else {
      void this.router.navigateByUrl('/');
    }
  }
}
