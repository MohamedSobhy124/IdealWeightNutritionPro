import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { ServiceCheckoutService } from '../../core/services/service-checkout.service';
import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { paymentMethodLabelKey, paymentUnavailableReasonKey } from '../../core/i18n/payment-labels';
import { PaymentMethodOption } from '../../core/models/checkout.models';
import { CreateServicePurchaseResponse, ServiceCheckoutQuoteResponse } from '../../core/models/service-checkout.models';
import { ServiceSubscriptionDetail } from '../../core/models/service.models';
import { GeideaCancelledError, GeideaUnavailableError, startGeideaPayment } from '../../core/utils/geidea-checkout';
import { apiErrorMessage } from '../../core/utils/api-error';
import { UiCardComponent, UiEmptyStateComponent, UiFormFieldComponent, UiOtpInputComponent, UiPageHeaderComponent, UiSkeletonComponent, UiStepperComponent } from '../../shared/ui';

const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [FormsModule, IwnCurrencyPipe, UiCardComponent, UiEmptyStateComponent, UiFormFieldComponent, UiPageHeaderComponent, UiSkeletonComponent, UiStepperComponent, UiOtpInputComponent],
  templateUrl: './service-checkout-page.component.html',
  styleUrl: './service-checkout-page.component.css',
})
export class ServiceCheckoutPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly servicesApi = inject(ServiceSubscriptionService);
  private readonly checkout = inject(ServiceCheckoutService);
  readonly locale = inject(LocaleService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly otpBusy = signal(false);
  readonly error = signal<string | null>(null);
  readonly otpMessage = signal<string | null>(null);
  readonly emailVerified = signal(false);
  readonly quote = signal<ServiceCheckoutQuoteResponse | null>(null);
  readonly paymentMethods = signal<PaymentMethodOption[]>([]);
  readonly service = signal<ServiceSubscriptionDetail | null>(null);
  readonly step = signal(0);
  /** Set when Continue is pressed on the contact step before it is complete. */
  readonly attempted = signal(false);

  serviceId = 0;
  offerId: number | null = null;
  name = '';
  email = '';
  phone = '';
  otp = '';
  promoCode = '';
  customAmount: number | null = null;
  paymentMethod = 'Geidea';
  createAccountForGuest = false;

  readonly isAuthenticated = computed(() => this.auth.isAuthenticated());

  readonly serviceTitle = computed(() => {
    const s = this.service();
    const q = this.quote();
    if (!s && !q) return '';
    if (q) return this.locale.pick(q.serviceTitle, q.serviceTitleAr);
    return this.locale.pick(s!.title, s!.titleAr);
  });

  readonly checkoutSteps = computed(() => {
    this.locale.locale();
    return [
      { id: 'contact' as const, label: this.t('contact') },
      { id: 'offer' as const, label: this.t('promoCode') },
      { id: 'payment' as const, label: this.quote()?.isFree ? this.t('checkoutStepReview') : this.t('payment') },
    ];
  });

  readonly currentKind = computed(() => this.checkoutSteps()[this.step()]?.id ?? 'contact');
  readonly onLastStep = computed(() => this.step() >= this.checkoutSteps().length - 1);

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  paymentLabel(method: PaymentMethodOption): string {
    const key = paymentMethodLabelKey(method.id);
    return key ? this.t(key) : method.label;
  }

  paymentUnavailableReason(method: PaymentMethodOption): string {
    const code = method.unavailableReasonCode;
    if (code) {
      const key = paymentUnavailableReasonKey(code);
      if (key) {
        let text = this.t(key);
        if (code === 'min_amount' && method.minimumAmount != null) {
          text = text.replace('{amount}', String(Math.round(method.minimumAmount)));
        }
        return text;
      }
    }
    return method.unavailableReason ?? '';
  }

  paymentMethodIcon(methodId: string): string {
    switch (methodId) {
      case 'COD':
        return 'payments';
      case 'Geidea':
        return 'credit_card';
      case 'Tamara':
        return 'account_balance_wallet';
      case 'Tabby':
        return 'calendar_view_week';
      default:
        return 'payment';
    }
  }

  amountDueNow(): number {
    return this.quote()?.amountToPay ?? 0;
  }

  nameError(): string | null {
    return this.showContactErrors() && !this.name.trim() ? this.t('fieldRequired') : null;
  }

  emailError(): string | null {
    if (!this.showContactErrors()) return null;
    if (!this.email.trim()) return this.t('fieldRequired');
    return this.emailValid() ? null : this.t('invalidEmail');
  }

  phoneError(): string | null {
    return this.showContactErrors() && !this.phone.trim() ? this.t('fieldRequired') : null;
  }

  otpError(): string | null {
    if (!this.showContactErrors() || this.isAuthenticated() || this.emailVerified() || this.otp.trim()) return null;
    return this.t('verifyEmailFirst');
  }

  paymentError(): string | null {
    const quote = this.quote();
    if (!this.attempted() || this.currentKind() !== 'payment' || !quote || quote.isFree || this.paymentMethod) return null;
    return this.t('selectPayment');
  }

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    if (this.auth.isAuthenticated()) {
      this.auth.loadProfile().subscribe({
        next: (profile) => {
          this.name = profile.fullName ?? '';
          this.email = profile.email ?? '';
          this.emailVerified.set(true);
        },
      });
    }

    this.route.paramMap.subscribe((params) => {
      this.serviceId = Number(params.get('id'));
      if (!this.serviceId) {
        this.error.set(this.t('serviceNotFound'));
        this.loading.set(false);
        return;
      }

      this.route.queryParamMap.subscribe((query) => {
        const offer = query.get('offerId');
        this.offerId = offer ? Number(offer) : null;
        this.loadService();
      });
    });
  }

  private loadService(): void {
    this.servicesApi.getService(this.serviceId).subscribe({
      next: (service) => {
        this.service.set(service);
        this.refreshQuote();
      },
      error: () => {
        this.error.set(this.t('serviceNotFound'));
        this.loading.set(false);
      },
    });
  }

  refreshQuote(): void {
    this.checkout
      .getQuote({
        serviceId: this.serviceId,
        offerId: this.offerId,
        promoCode: this.promoCode.trim() || null,
        customAmount: this.customAmount,
      })
      .subscribe({
        next: (quote) => {
          this.quote.set(quote);
          this.loading.set(false);
          this.error.set(null);
          if (!quote.isFree) {
            this.loadPaymentMethods(quote.amountToPay);
          } else {
            this.paymentMethods.set([]);
          }
        },
        error: (err) => {
          this.error.set(apiErrorMessage(err, this.t('serviceQuoteFailed')));
          this.loading.set(false);
        },
      });
  }

  private loadPaymentMethods(total: number): void {
    this.checkout.getPaymentMethods(total).subscribe({
      next: (res) => {
        this.paymentMethods.set(res.methods);
        const selected = res.methods.find((m) => m.id === this.paymentMethod && m.available);
        if (!selected) {
          const first = res.methods.find((m) => m.available);
          if (first) this.paymentMethod = first.id;
        }
      },
    });
  }

  sendOtp(): void {
    this.otpBusy.set(true);
    this.checkout.sendOtp(this.email.trim()).subscribe({
      next: (res) => {
        this.otpMessage.set(res.message);
        this.otpBusy.set(false);
      },
      error: (err) => {
        this.otpMessage.set(apiErrorMessage(err, this.t('couldNotSendCode')));
        this.otpBusy.set(false);
      },
    });
  }

  verifyOtpCode(): void {
    this.otpBusy.set(true);
    this.checkout.verifyOtp(this.email.trim(), this.otp.trim()).subscribe({
      next: (res) => {
        this.emailVerified.set(res.isValid);
        this.otpMessage.set(res.message ?? '');
        this.otpBusy.set(false);
      },
      error: (err) => {
        this.emailVerified.set(false);
        this.otpMessage.set(apiErrorMessage(err, this.t('invalidCode')));
        this.otpBusy.set(false);
      },
    });
  }

  canSubmit(): boolean {
    if (!this.contactValid() || !this.quote()) return false;
    const quote = this.quote()!;
    if (!quote.isFree && !this.paymentMethod) return false;
    return true;
  }

  nextStep(): void {
    if (this.currentKind() === 'contact' && !this.contactValid()) {
      this.attempted.set(true);
      return;
    }
    this.attempted.set(false);
    this.step.update((value) => Math.min(value + 1, this.checkoutSteps().length - 1));
  }

  prevStep(): void {
    this.attempted.set(false);
    this.step.update((value) => Math.max(value - 1, 0));
  }

  submit(): void {
    if (!this.onLastStep()) {
      this.nextStep();
      return;
    }
    if (!this.canSubmit()) {
      this.attempted.set(true);
      if (!this.contactValid()) this.step.set(0);
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const quote = this.quote()!;
    this.checkout
      .purchase(this.serviceId, {
        name: this.name.trim(),
        email: this.email.trim(),
        phoneNumber: this.phone.trim(),
        offerId: this.offerId,
        promoCode: this.promoCode.trim() || null,
        customAmount: this.customAmount,
        paymentMethod: quote.isFree ? 'Geidea' : this.paymentMethod,
        otp: this.isAuthenticated() ? null : this.otp.trim() || null,
        createAccountForGuest: !this.isAuthenticated() && this.createAccountForGuest,
      })
      .subscribe({
        next: (result) => this.handlePurchase(result),
        error: (err) => {
          this.error.set(apiErrorMessage(err, this.t('servicePurchaseFailed')));
          this.submitting.set(false);
        },
      });
  }

  private contactValid(): boolean {
    if (!this.name.trim() || !this.emailValid() || !this.phone.trim()) return false;
    if (!this.isAuthenticated() && !this.emailVerified() && !this.otp.trim()) return false;
    return true;
  }

  private emailValid(): boolean {
    return EMAIL_PATTERN.test(this.email.trim());
  }

  private showContactErrors(): boolean {
    return this.attempted() && this.currentKind() === 'contact';
  }

  private handlePurchase(result: CreateServicePurchaseResponse): void {
    if (result.isPaid) {
      void this.router.navigate(['/services/confirmation', result.purchaseId]);
      return;
    }

    if (result.requiresPaymentAction && result.paymentMethod === 'Geidea' && result.paymentSessionId) {
      startGeideaPayment(result.paymentSessionId)
        .then(() => this.finalizePayment(result.purchaseId))
        .catch((err: Error) => {
          // Popup could not open: use Geidea's hosted payment page instead.
          if (err instanceof GeideaUnavailableError && result.paymentRedirectUrl) {
            window.location.href = result.paymentRedirectUrl;
            return;
          }
          this.error.set(
            err instanceof GeideaCancelledError ? this.t('paymentNotCompleted') : err.message || this.t('paymentNotCompleted'),
          );
          this.submitting.set(false);
        });
      return;
    }

    if (result.requiresPaymentAction && result.paymentRedirectUrl) {
      window.location.href = result.paymentRedirectUrl;
      return;
    }

    void this.router.navigate(['/services/confirmation', result.purchaseId]);
  }

  private finalizePayment(purchaseId: number): void {
    this.checkout.completePayment(purchaseId).subscribe({
      next: () => void this.router.navigate(['/services/confirmation', purchaseId]),
      error: (err) => {
        this.error.set(apiErrorMessage(err, this.t('paymentVerificationFailed')));
        this.submitting.set(false);
      },
    });
  }
}
