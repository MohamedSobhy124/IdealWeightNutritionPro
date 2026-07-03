import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { LocaleService } from '../../core/services/locale.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { UiFormFieldComponent, UiPageHeaderComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiFormFieldComponent, UiPageHeaderComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly locale = inject(LocaleService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('error') === 'google') {
      this.error.set(this.t('oauthFailed'));
    }
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.cart.load().subscribe();
        this.auth.loadProfile().subscribe({
          next: () => this.router.navigate(['/account']),
          error: () => this.router.navigate(['/account']),
        });
      },
      error: (err) => {
        this.error.set(err?.error?.errors?.[0] ?? err?.error?.error ?? this.t('loginFailed'));
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }
}
