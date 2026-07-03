import { Component, OnInit, computed, inject, signal } from '@angular/core';

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

import { ServiceCheckoutQuoteResponse } from '../../core/models/service-checkout.models';

import { ServiceSubscriptionDetail } from '../../core/models/service.models';

import { startGeideaPayment } from '../../core/utils/geidea-checkout';
import { apiErrorMessage } from '../../core/utils/api-error';

import { UiCardComponent, UiEmptyStateComponent, UiFormFieldComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';



@Component({

  standalone: true,

  imports: [FormsModule, IwnCurrencyPipe, UiCardComponent, UiEmptyStateComponent, UiFormFieldComponent, UiPageHeaderComponent, UiSkeletonComponent],

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



  readonly loading = signal(true);

  readonly submitting = signal(false);

  readonly otpBusy = signal(false);

  readonly error = signal<string | null>(null);

  readonly otpMessage = signal<string | null>(null);

  readonly emailVerified = signal(false);

  readonly quote = signal<ServiceCheckoutQuoteResponse | null>(null);

  readonly paymentMethods = signal<PaymentMethodOption[]>([]);

  readonly service = signal<ServiceSubscriptionDetail | null>(null);



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



  ngOnInit(): void {

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

        this.error.set('Service not found.');

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

        this.error.set('Service not found.');

        this.loading.set(false);

      },

    });

  }



  refreshQuote(): void {

    this.checkout

      .getQuote({

        serviceId: this.serviceId,

        offerId: this.offerId,

        promoCode: this.promoCode || null,

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

          this.error.set(apiErrorMessage(err, 'Could not calculate price.'));

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

    this.checkout.sendOtp(this.email).subscribe({

      next: (res) => {

        this.otpMessage.set(res.message);

        this.otpBusy.set(false);

      },

      error: (err) => {

        this.otpMessage.set(apiErrorMessage(err, 'Could not send code.'));

        this.otpBusy.set(false);

      },

    });

  }



  verifyOtpCode(): void {

    this.otpBusy.set(true);

    this.checkout.verifyOtp(this.email, this.otp).subscribe({

      next: (res) => {

        this.emailVerified.set(res.isValid);

        this.otpMessage.set(res.message ?? '');

        this.otpBusy.set(false);

      },

      error: (err) => {

        this.emailVerified.set(false);

        this.otpMessage.set(apiErrorMessage(err, 'Invalid code.'));

        this.otpBusy.set(false);

      },

    });

  }



  canSubmit(): boolean {

    if (!this.name || !this.email || !this.phone || !this.quote()) return false;

    if (!this.isAuthenticated() && !this.emailVerified() && !this.otp) return false;

    const q = this.quote()!;

    if (!q.isFree && !this.paymentMethod) return false;

    return true;

  }



  submit(): void {

    if (!this.canSubmit()) return;



    this.submitting.set(true);

    this.error.set(null);



    this.checkout

      .purchase(this.serviceId, {

        name: this.name,

        email: this.email,

        phoneNumber: this.phone,

        offerId: this.offerId,

        promoCode: this.promoCode || null,

        customAmount: this.customAmount,

        paymentMethod: this.quote()!.isFree ? 'Geidea' : this.paymentMethod,

        otp: this.isAuthenticated() ? null : this.otp || null,

        createAccountForGuest: !this.isAuthenticated() && this.createAccountForGuest,

      })

      .subscribe({

        next: (result) => this.handlePurchase(result),

        error: (err) => {

          this.error.set(apiErrorMessage(err, 'Could not complete purchase.'));

          this.submitting.set(false);

        },

      });

  }



  private handlePurchase(result: import('../../core/models/service-checkout.models').CreateServicePurchaseResponse): void {

    if (result.isPaid) {

      this.router.navigate(['/services/confirmation', result.purchaseId]);

      return;

    }



    if (result.requiresPaymentAction && result.paymentMethod === 'Geidea' && result.paymentSessionId) {

      startGeideaPayment(result.paymentSessionId)

        .then(() => this.finalizePayment(result.purchaseId))

        .catch((err: Error) => {

          this.error.set(err.message ?? 'Payment was not completed.');

          this.submitting.set(false);

        });

      return;

    }



    if (result.requiresPaymentAction && result.paymentRedirectUrl) {

      window.location.href = result.paymentRedirectUrl;

      return;

    }



    this.router.navigate(['/services/confirmation', result.purchaseId]);

  }



  private finalizePayment(purchaseId: number): void {

    this.checkout.completePayment(purchaseId).subscribe({

      next: () => this.router.navigate(['/services/confirmation', purchaseId]),

      error: (err) => {

        this.error.set(apiErrorMessage(err, 'Payment verification failed.'));

        this.submitting.set(false);

      },

    });

  }

}

