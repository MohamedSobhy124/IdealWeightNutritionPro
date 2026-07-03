import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { LocaleService } from '../../core/services/locale.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { UiFormFieldComponent, UiPageHeaderComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule, RouterLink, UiFormFieldComponent, UiPageHeaderComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly otpBusy = signal(false);
  readonly otpMessage = signal<string | null>(null);
  readonly emailVerified = signal(false);

  otp = '';

  readonly form = this.fb.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  sendOtp(): void {
    const email = this.form.controls.email.value.trim();
    if (!email) return;

    this.otpBusy.set(true);
    this.otpMessage.set(null);
    this.emailVerified.set(false);

    this.auth.sendRegistrationOtp(email).subscribe({
      next: (res) => {
        this.otpMessage.set(res.message);
        this.otpBusy.set(false);
      },
      error: (err) => {
        this.otpMessage.set(err?.error?.errors?.[0] ?? err?.error?.error ?? this.t('registrationFailed'));
        this.otpBusy.set(false);
      },
    });
  }

  verifyOtpCode(): void {
    const email = this.form.controls.email.value.trim();
    if (!email || !this.otp.trim()) return;

    this.otpBusy.set(true);
    this.otpMessage.set(null);

    this.auth.verifyRegistrationOtp(email, this.otp.trim()).subscribe({
      next: (res) => {
        this.emailVerified.set(true);
        this.otpMessage.set(res.message || this.t('emailVerified'));
        this.otpBusy.set(false);
      },
      error: (err) => {
        this.emailVerified.set(false);
        this.otpMessage.set(err?.error?.errors?.[0] ?? err?.error?.error ?? this.t('registrationFailed'));
        this.otpBusy.set(false);
      },
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    if (!this.emailVerified()) {
      this.error.set(this.t('verifyEmailFirst'));
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const { fullName, email, phone, password } = this.form.getRawValue();
    this.auth
      .register({ fullName, email, password, phone: phone || undefined })
      .subscribe({
        next: () => {
          this.cart.load().subscribe();
          this.router.navigate(['/account']);
        },
        error: (err) => {
          this.error.set(err?.error?.errors?.[0] ?? err?.error?.error ?? this.t('registrationFailed'));
          this.loading.set(false);
        },
        complete: () => this.loading.set(false),
      });
  }
}
