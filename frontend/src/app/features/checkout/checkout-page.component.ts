import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { CheckoutService } from '../../core/services/checkout.service';
import { LocaleService } from '../../core/services/locale.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { paymentMethodLabelKey, paymentUnavailableReasonKey } from '../../core/i18n/payment-labels';
import {
  City,
  RemoteArea,
  ShippingQuoteResponse,
  PaymentMethodOption,
  CreateOrderResponse,
} from '../../core/models/checkout.models';
import { startGeideaPayment } from '../../core/utils/geidea-checkout';
import { apiErrorMessage } from '../../core/utils/api-error';

import { UiCardComponent, UiEmptyStateComponent, UiFormFieldComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [RouterLink, FormsModule, IwnCurrencyPipe, UiCardComponent, UiPageHeaderComponent, UiSkeletonComponent, UiEmptyStateComponent, UiFormFieldComponent],
  templateUrl: './checkout-page.component.html',
  styleUrl: './checkout-page.component.css',
})
export class CheckoutPageComponent implements OnInit {
  private readonly checkout = inject(CheckoutService);
  private readonly cartService = inject(CartService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly otpBusy = signal(false);
  readonly error = signal<string | null>(null);
  readonly otpMessage = signal<string | null>(null);
  readonly emailVerified = signal(false);
  readonly cities = signal<City[]>([]);
  readonly remoteAreas = signal<RemoteArea[]>([]);
  readonly quote = signal<ShippingQuoteResponse | null>(null);
  readonly paymentMethods = signal<PaymentMethodOption[]>([]);
  readonly cartSubtotal = signal(0);
  readonly cartDiscount = signal(0);
  readonly cartTotal = signal(0);
  readonly cartEmpty = signal(false);

  name = '';
  email = '';
  phone = '';
  street = '';
  cityId: number | null = null;
  remoteAreaId: number | null = null;
  otp = '';
  paymentMethod = 'COD';
  createAccountForGuest = false;
  readonly checkoutStep = signal(0);

  readonly checkoutSteps: { icon: string; labelKey: UiKey }[] = [
    { icon: 'person', labelKey: 'checkoutStepContact' },
    { icon: 'local_shipping', labelKey: 'checkoutStepDelivery' },
    { icon: 'credit_card', labelKey: 'checkoutStepPayment' },
    { icon: 'fact_check', labelKey: 'checkoutStepReview' },
  ];

  readonly isAuthenticated = computed(() => this.auth.isAuthenticated());

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  cityLabel(city: City): string {
    const name = this.locale.pick(city.name, city.nameAr);
    return `${name} (${city.emirate})`;
  }

  areaLabel(area: RemoteArea): string {
    return this.locale.pick(area.name, area.nameAr);
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

  stepProgress(): number {
    return Math.round((this.checkoutStep() / 3) * 100);
  }

  orderTotal(): number {
    return this.quote()?.total ?? this.cartTotal();
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

  selectedPaymentLabel(): string {
    const method = this.paymentMethods().find((m) => m.id === this.paymentMethod);
    if (method) return this.paymentLabel(method);
    if (this.paymentMethod === 'COD') return this.t('cashOnDelivery');
    return this.paymentMethod;
  }

  reviewCityName(): string {
    if (!this.cityId) return '';
    const city = this.cities().find((c) => c.id === this.cityId);
    return city ? this.cityLabel(city) : '';
  }

  reviewAreaName(): string {
    if (!this.remoteAreaId) return '';
    const area = this.remoteAreas().find((a) => a.id === this.remoteAreaId);
    return area ? this.areaLabel(area) : '';
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

    this.cartService.load().subscribe({
      next: (cart) => {
        this.cartSubtotal.set(cart.subtotal);
        this.cartDiscount.set(cart.discount ?? 0);
        this.cartTotal.set(cart.total ?? cart.subtotal);
        this.cartEmpty.set(cart.items.length === 0);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('cartLoadError'));
        this.loading.set(false);
      },
    });

    this.checkout.listCities().subscribe({
      next: (cities) => this.cities.set(cities),
      error: () => this.error.set(this.t('couldNotLoadCities')),
    });
  }

  onCityChange(cityId: number | null): void {
    this.remoteAreaId = null;
    this.remoteAreas.set([]);
    this.quote.set(null);

    if (!cityId) return;

    this.checkout.listRemoteAreas(cityId).subscribe({
      next: (areas) => {
        this.remoteAreas.set(areas);
        if (areas.length === 0) this.refreshQuote();
      },
    });
  }

  refreshQuote(): void {
    if (!this.cityId) return;
    if (this.remoteAreas().length > 0 && !this.remoteAreaId) return;

    this.checkout
      .getShippingQuote({
        cityId: this.cityId,
        remoteAreaId: this.remoteAreaId ?? undefined,
      })
      .subscribe({
        next: (quote) => {
          this.quote.set(quote);
          this.loadPaymentMethods(quote.total);
        },
        error: () => this.quote.set(null),
      });
  }

  private loadPaymentMethods(total: number): void {
    this.checkout.getPaymentMethods(total).subscribe({
      next: (res) => {
        this.paymentMethods.set(res.methods);
        const selected = res.methods.find((m) => m.id === this.paymentMethod && m.available);
        if (!selected) {
          const firstAvailable = res.methods.find((m) => m.available);
          if (firstAvailable) this.paymentMethod = firstAvailable.id;
        }
      },
      error: () => this.paymentMethods.set([]),
    });
  }

  sendOtp(): void {
    this.otpBusy.set(true);
    this.otpMessage.set(null);
    this.checkout.sendOtp(this.email).subscribe({
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
    this.checkout.verifyOtp(this.email, this.otp).subscribe({
      next: (res) => {
        this.emailVerified.set(true);
        this.otpMessage.set(res.message);
        this.otpBusy.set(false);
      },
      error: (err) => {
        this.emailVerified.set(false);
        this.otpMessage.set(apiErrorMessage(err, this.t('invalidCode')));
        this.otpBusy.set(false);
      },
    });
  }

  canPlaceOrder(): boolean {
    if (!this.name || !this.email || !this.phone || !this.street || !this.cityId) return false;
    if (this.remoteAreas().length > 0 && !this.remoteAreaId) return false;
    if (!this.isAuthenticated() && !this.emailVerified() && !this.otp) return false;
    return true;
  }

  canProceedFromStep(step: number): boolean {
    switch (step) {
      case 0:
        if (!this.name || !this.email || !this.phone) return false;
        if (!this.isAuthenticated() && !this.emailVerified() && !this.otp) return false;
        return true;
      case 1:
        if (!this.street || !this.cityId) return false;
        if (this.remoteAreas().length > 0 && !this.remoteAreaId) return false;
        return true;
      case 2:
        return !!this.paymentMethod;
      default:
        return this.canPlaceOrder();
    }
  }

  nextStep(): void {
    if (!this.canProceedFromStep(this.checkoutStep())) return;
    if (this.checkoutStep() === 1) {
      this.refreshQuote();
    }
    this.checkoutStep.update((s) => Math.min(s + 1, 3));
  }

  prevStep(): void {
    this.checkoutStep.update((s) => Math.max(s - 1, 0));
  }

  submit(): void {
    if (!this.canPlaceOrder() || !this.cityId) return;

    this.submitting.set(true);
    this.error.set(null);

    this.cartService.load().subscribe({
      next: (cart) => {
        if (cart.items.length === 0) {
          this.error.set(this.t('cartEmpty'));
          this.submitting.set(false);
          return;
        }

        this.checkout
          .placeOrder({
            name: this.name,
            email: this.email,
            phoneNumber: this.phone,
            streetAddress: this.street,
            cityId: this.cityId!,
            remoteAreaId: this.remoteAreaId ?? undefined,
            paymentMethod: this.paymentMethod,
            otp: this.isAuthenticated() ? undefined : this.otp || undefined,
            createAccountForGuest: !this.isAuthenticated() && this.createAccountForGuest,
          })
          .subscribe({
            next: (order) => this.handleOrderPlaced(order),
            error: (err) => {
              this.error.set(apiErrorMessage(err, this.t('couldNotPlaceOrder')));
              this.submitting.set(false);
            },
          });
      },
      error: () => {
        this.error.set(this.t('cartLoadError'));
        this.submitting.set(false);
      },
    });
  }

  private handleOrderPlaced(order: CreateOrderResponse): void {
    if (order.requiresPaymentAction && order.paymentMethod === 'Geidea' && order.paymentSessionId) {
      startGeideaPayment(order.paymentSessionId)
        .then(() => this.finalizeOnlineOrder(order.orderId))
        .catch((err: Error) => {
          this.error.set(err.message ?? this.t('paymentNotCompleted'));
          this.submitting.set(false);
        });
      return;
    }

    if (order.requiresPaymentAction && order.paymentRedirectUrl) {
      window.location.href = order.paymentRedirectUrl;
      return;
    }

    this.cartService.load().subscribe();
    this.router.navigate(['/order/confirmation', order.orderId], {
      state: {
        order,
        email: this.email,
        accountCreated: order.accountCreated,
        accountLinked: order.accountLinked,
      },
    });
  }

  private finalizeOnlineOrder(orderId: number): void {
    this.checkout.completePayment(orderId).subscribe({
      next: () => {
        this.cartService.load().subscribe();
        this.router.navigate(['/order/confirmation', orderId], {
          state: { email: this.email },
        });
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err, this.t('paymentVerifyFailed')));
        this.submitting.set(false);
      },
    });
  }
}
