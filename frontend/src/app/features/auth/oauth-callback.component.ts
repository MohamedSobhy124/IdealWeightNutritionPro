import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { LocaleService } from '../../core/services/locale.service';
import { UI, UiKey } from '../../core/i18n/ui-text';

@Component({
  standalone: true,
  template: `
    <section class="auth-card">
      <p>{{ t('loading') }}</p>
    </section>
  `,
  styles: [
    `
      .auth-card {
        max-width: 28rem;
        margin: 2rem auto;
        text-align: center;
      }
    `,
  ],
})
export class OauthCallbackComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);
  private readonly locale = inject(LocaleService);

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const accessToken = params.get('accessToken');
    const refreshToken = params.get('refreshToken');
    const expiresAt = params.get('expiresAt');

    if (!accessToken || !refreshToken || !expiresAt) {
      this.router.navigate(['/auth/login'], { queryParams: { error: 'google' } });
      return;
    }

    this.auth.persistTokens({ accessToken, refreshToken, expiresAt, userId: '' });
    this.cart.load().subscribe();
    this.auth.loadProfile().subscribe({
      next: () => this.router.navigate(['/account']),
      error: () => this.router.navigate(['/account']),
    });
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }
}
