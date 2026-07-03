import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { UiEmptyStateComponent, UiFormFieldComponent, UiPageHeaderComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiFormFieldComponent, UiPageHeaderComponent, UiEmptyStateComponent],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css',
})
export class ResetPasswordComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);

  email = '';
  token = '';

  readonly form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      this.email = params.get('email') ?? '';
      this.token = params.get('token') ?? '';
    });
  }

  submit(): void {
    if (this.form.invalid || !this.email || !this.token) return;

    this.loading.set(true);
    this.error.set(null);
    this.message.set(null);

    this.auth
      .resetPassword({
        email: this.email,
        token: this.token,
        password: this.form.getRawValue().password,
      })
      .subscribe({
        next: (res) => {
          this.message.set(res.message || this.t('passwordUpdated'));
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.error?.errors?.[0] ?? this.t('resetLinkInvalid'));
          this.loading.set(false);
        },
      });
  }
}
