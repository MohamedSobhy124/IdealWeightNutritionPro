import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { LocaleService } from '../../core/services/locale.service';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { UiFormFieldComponent, UiPasswordFieldComponent } from '../../shared/ui';

/** Shared, view-only copy for the auth screens (login + register). */
export const AUTH_COPY = {
  heroTitle: { en: 'Welcome back to your wellness journey', ar: 'أهلاً بعودتك إلى رحلتك الصحية' },
  heroTitleJoin: { en: 'Become a member, get more from every order', ar: 'كن عضواً واحصل على المزيد مع كل طلب' },
  heroLeadJoin: {
    en: 'Exclusive member prices, faster checkout and personal advice from our nutrition experts.',
    ar: 'أسعار حصرية للأعضاء، ودفع أسرع، ونصائح شخصية من خبراء التغذية لدينا.',
  },
  formTitle: { en: 'Welcome back', ar: 'مرحباً بعودتك' },
  formSubtitle: {
    en: 'Sign in to track orders, reorder favourites and unlock member prices.',
    ar: 'سجّل الدخول لتتبع طلباتك وإعادة طلب منتجاتك المفضلة والاستفادة من أسعار الأعضاء.',
  },
  joinTitle: { en: 'Create your account', ar: 'أنشئ حسابك' },
  joinSubtitle: {
    en: 'Join in under a minute and start saving on every order.',
    ar: 'انضم خلال أقل من دقيقة وابدأ التوفير مع كل طلب.',
  },
  originalsDesc: { en: 'Sourced from authorised distributors', ar: 'من موزعين معتمدين' },
  deliveryDesc: { en: 'Fast delivery across the UAE', ar: 'توصيل سريع في جميع أنحاء الإمارات' },
  expertTitle: { en: 'Expert nutrition advice', ar: 'استشارات خبراء التغذية' },
  expertDesc: { en: 'Guidance tailored to your goals', ar: 'إرشادات تناسب أهدافك' },
  memberTitle: { en: 'Exclusive member offers', ar: 'عروض حصرية للأعضاء' },
  memberDesc: { en: 'Members-only prices and early access', ar: 'أسعار خاصة للأعضاء ووصول مبكر' },
  trustLine: { en: 'Trusted by thousands of customers across the UAE', ar: 'موثوق من آلاف العملاء في الإمارات' },
  newHere: { en: 'New to Ideal Weight?', ar: 'جديد في ايديال ويت؟' },
  secureNote: { en: 'Your data is protected and never shared', ar: 'بياناتك محمية ولن تتم مشاركتها' },
  stepDetails: { en: 'Your details', ar: 'بياناتك' },
  stepVerify: { en: 'Verify your email', ar: 'تأكيد البريد الإلكتروني' },
  stepSecure: { en: 'Secure your account', ar: 'تأمين حسابك' },
} as const;

export type AuthCopyKey = keyof typeof AUTH_COPY;

export interface AuthBenefit {
  icon: string;
  title: () => string;
  desc: () => string;
}

/** Benefit list for the auth brand panel (view-only). */
export function authBenefits(t: (k: UiKey) => string, c: (k: AuthCopyKey) => string): AuthBenefit[] {
  return [
    { icon: 'verified', title: () => t('trustedOriginals'), desc: () => c('originalsDesc') },
    { icon: 'local_shipping', title: () => t('fastDelivery'), desc: () => c('deliveryDesc') },
    { icon: 'support_agent', title: () => c('expertTitle'), desc: () => c('expertDesc') },
    { icon: 'loyalty', title: () => c('memberTitle'), desc: () => c('memberDesc') },
  ];
}

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, UiFormFieldComponent, UiPasswordFieldComponent],
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

  /* View-only helpers for the redesigned layout. */
  readonly stars = [1, 2, 3, 4, 5];
  readonly benefits = authBenefits(
    (k) => this.t(k),
    (k) => this.c(k),
  );

  c(key: AuthCopyKey): string {
    return this.locale.pick(AUTH_COPY[key].en, AUTH_COPY[key].ar);
  }

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
