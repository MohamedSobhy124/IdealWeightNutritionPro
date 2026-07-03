import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { UiFormFieldComponent, UiPageHeaderComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiFormFieldComponent, UiPageHeaderComponent],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css',
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  submit(): void {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set(null);
    this.message.set(null);

    this.auth.forgotPassword(this.form.getRawValue()).subscribe({
      next: (res) => {
        this.message.set(res.message || this.t('resetEmailSent'));
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.errors?.[0] ?? this.t('resetEmailSent'));
        this.loading.set(false);
      },
    });
  }
}
