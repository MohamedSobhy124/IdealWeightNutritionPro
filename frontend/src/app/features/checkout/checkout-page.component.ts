import { NgTemplateOutlet, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { CatalogueService } from '../../core/services/catalogue.service';
import { CheckoutService } from '../../core/services/checkout.service';
import { LocaleService } from '../../core/services/locale.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { formatIwnCurrency } from '../../core/utils/iwn-currency';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { paymentMethodLabelKey, paymentUnavailableReasonKey } from '../../core/i18n/payment-labels';
import {
  City,
  RemoteArea,
  ShippingQuoteResponse,
  PaymentMethodOption,
  CreateOrderResponse,
} from '../../core/models/checkout.models';
import { GeideaCancelledError, GeideaUnavailableError, startGeideaPayment } from '../../core/utils/geidea-checkout';
import { apiErrorMessage } from '../../core/utils/api-error';

import { CartItem } from '../../core/models/cart.models';
import { UiEmptyStateComponent, UiFormFieldComponent, UiOtpInputComponent, UiPageHeaderComponent, UiSheetSelectComponent, UiSkeletonComponent } from '../../shared/ui';

const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

/** Checkout-only copy that has no shared key in ui-text.ts. */
const COPY = {
  secureCheckout: { en: 'Secure checkout', ar: 'دفع آمن ومشفّر' },
  stepOf: { en: 'Step {n} of {total}', ar: 'الخطوة {n} من {total}' },
  edit: { en: 'Edit', ar: 'تعديل' },
  showSummary: { en: 'Show order summary', ar: 'عرض ملخص الطلب' },
  hideSummary: { en: 'Hide order summary', ar: 'إخفاء ملخص الطلب' },
  weAccept: { en: 'We accept', ar: 'طرق الدفع المتاحة' },
  payDescCOD: { en: 'Pay in cash when your order arrives', ar: 'ادفع نقداً عند استلام طلبك' },
  payDescCard: { en: 'Visa or Mastercard, processed securely', ar: 'فيزا أو ماستركارد بمعالجة آمنة' },
  payDescLater: { en: 'Split your total into interest-free payments', ar: 'قسّم المبلغ على دفعات بدون فوائد' },
  payDescOther: { en: 'Secure online payment', ar: 'دفع إلكتروني آمن' },
} as const;
type CopyKey = keyof typeof COPY;

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [RouterLink, FormsModule, NgTemplateOutlet, IwnCurrencyPipe, UiPageHeaderComponent, UiSkeletonComponent, UiEmptyStateComponent, UiFormFieldComponent, UiOtpInputComponent, UiSheetSelectComponent],
  templateUrl: './checkout-page.component.html',
  styleUrl: './checkout-page.component.css',
})
export class CheckoutPageComponent implements OnInit {
  private readonly checkout = inject(CheckoutService);
  private readonly cartService = inject(CartService);
  readonly catalogue = inject(CatalogueService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);
  private readonly platformId = inject(PLATFORM_ID);

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
  /** Set when Continue is pressed on a step that is still incomplete. */
  private readonly attemptedStep = signal<number | null>(null);

  readonly checkoutSteps: { icon: string; labelKey: UiKey }[] = [
    { icon: 'person', labelKey: 'checkoutStepContact' },
    { icon: 'local_shipping', labelKey: 'checkoutStepDelivery' },
    { icon: 'credit_card', labelKey: 'checkoutStepPayment' },
    { icon: 'fact_check', labelKey: 'checkoutStepReview' },
  ];

  /* ---- View-only helpers (no checkout logic) ---------------------------- */
  /** Mobile order-summary disclosure. */
  readonly summaryOpen = signal(false);
  readonly cartItems = computed<CartItem[]>(() => this.cartService.cart()?.items ?? []);
  readonly appliedPromoCode = computed(() => this.cartService.cart()?.appliedPromo?.code ?? null);
  /** Fill of the stepper line, 0-100. */
  readonly stepProgress = computed(() => (this.checkoutStep() / (this.checkoutSteps.length - 1)) * 100);
  readonly stepCounter = computed(() =>
    this.c('stepOf')
      .replace('{n}', String(this.checkoutStep() + 1))
      .replace('{total}', String(this.checkoutSteps.length)),
  );
  /** Payment names shown as pills in the summary. */
  readonly paymentPills = computed(() => {
    this.locale.locale();
    const available = this.paymentMethods().filter((m) => m.available);
    if (available.length) return available.map((m) => this.paymentLabel(m));
    // Before methods load (no city yet), only claim what the API always offers.
    return [this.t('cashOnDelivery')];
  });

  c(key: CopyKey): string {
    return this.locale.pick(COPY[key].en, COPY[key].ar);
  }

  itemTitle(item: CartItem): string {
    return this.locale.pick(item.title, item.titleAr);
  }

  paymentDescription(methodId: string): string {
    switch (methodId) {
      case 'COD':
        return this.c('payDescCOD');
      case 'Geidea':
        return this.c('payDescCard');
      case 'Tamara':
      case 'Tabby':
        return this.c('payDescLater');
      default:
        return this.c('payDescOther');
    }
  }

  /** Jump back to an earlier step (stepper dots / review "Edit" links). Same effect as repeated Back. */
  goToStep(step: number): void {
    if (step >= this.checkoutStep() || step < 0) return;
    this.attemptedStep.set(null);
    this.checkoutStep.set(step);
  }

  readonly isAuthenticated = computed(() => this.auth.isAuthenticated());

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  /** 0–100: how close the subtotal is to the free-delivery threshold. */
  readonly freeDeliveryProgress = computed(() => {
    const q = this.quote();
    const threshold = q?.freeDeliveryThreshold ?? 0;
    if (!q?.freeDeliveryAvailable || threshold <= 0) return 100;
    return Math.min(100, Math.max(0, Math.round((q.subtotal / threshold) * 100)));
  });

  readonly freeDeliveryRemainingText = computed(() =>
    this.t('freeDeliveryRemaining').replace(
      '{amount}',
      formatIwnCurrency(this.quote()?.amountRemainingForFreeDelivery ?? 0, this.locale),
    ),
  );

  readonly cityOptions = computed(() => {
    this.locale.locale();
    return [
      { value: null, label: this.t('selectCity') },
      ...this.cities().map((city) => ({ value: city.id, label: this.cityLabel(city) })),
    ];
  });

  readonly areaOptions = computed(() => {
    this.locale.locale();
    return [
      { value: null, label: this.t('selectArea') },
      ...this.remoteAreas().map((area) => ({ value: area.id, label: this.areaLabel(area) })),
    ];
  });

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
    return this.canProceedFromStep(0) && this.canProceedFromStep(1) && this.canProceedFromStep(2);
  }

  canProceedFromStep(step: number): boolean {
    switch (step) {
      case 0:
        if (!this.name.trim() || !this.emailValid() || !this.phone.trim()) return false;
        if (!this.isAuthenticated() && !this.emailVerified() && !this.otp.trim()) return false;
        return true;
      case 1:
        if (!this.street.trim() || !this.cityId) return false;
        if (this.remoteAreas().length > 0 && !this.remoteAreaId) return false;
        return true;
      case 2:
        return !!this.paymentMethod;
      default:
        return true;
    }
  }

  nameError(): string | null {
    return this.attemptedStep() === 0 && !this.name.trim() ? this.t('fieldRequired') : null;
  }

  emailError(): string | null {
    if (this.attemptedStep() !== 0) return null;
    if (!this.email.trim()) return this.t('fieldRequired');
    return this.emailValid() ? null : this.t('invalidEmail');
  }

  phoneError(): string | null {
    return this.attemptedStep() === 0 && !this.phone.trim() ? this.t('fieldRequired') : null;
  }

  otpError(): string | null {
    if (this.attemptedStep() !== 0 || this.isAuthenticated() || this.emailVerified() || this.otp.trim()) return null;
    return this.t('verifyEmailFirst');
  }

  streetError(): string | null {
    return this.attemptedStep() === 1 && !this.street.trim() ? this.t('fieldRequired') : null;
  }

  cityError(): string | null {
    return this.attemptedStep() === 1 && !this.cityId ? this.t('fieldRequired') : null;
  }

  areaError(): string | null {
    if (this.attemptedStep() !== 1 || this.remoteAreas().length === 0 || this.remoteAreaId) return null;
    return this.t('fieldRequired');
  }

  nextStep(): void {
    if (!this.canProceedFromStep(this.checkoutStep())) {
      this.attemptedStep.set(this.checkoutStep());
      return;
    }
    this.attemptedStep.set(null);
    if (this.checkoutStep() === 1) {
      this.refreshQuote();
    }
    this.checkoutStep.update((s) => Math.min(s + 1, 3));
  }

  prevStep(): void {
    this.attemptedStep.set(null);
    this.checkoutStep.update((s) => Math.max(s - 1, 0));
  }

  private emailValid(): boolean {
    return EMAIL_PATTERN.test(this.email.trim());
  }

  submit(): void {
    if (!this.canPlaceOrder() || !this.cityId) {
      const step = !this.canProceedFromStep(0) ? 0 : !this.canProceedFromStep(1) ? 1 : 2;
      this.checkoutStep.set(step);
      this.attemptedStep.set(step);
      return;
    }

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
            name: this.name.trim(),
            email: this.email.trim(),
            phoneNumber: this.phone.trim(),
            streetAddress: this.street.trim(),
            cityId: this.cityId!,
            remoteAreaId: this.remoteAreaId ?? undefined,
            paymentMethod: this.paymentMethod,
            otp: this.isAuthenticated() ? undefined : this.otp.trim() || undefined,
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
          // Popup could not open: use Geidea's hosted payment page instead.
          if (err instanceof GeideaUnavailableError && order.paymentRedirectUrl) {
            window.location.href = order.paymentRedirectUrl;
            return;
          }
          this.error.set(
            err instanceof GeideaCancelledError ? this.t('paymentNotCompleted') : err.message || this.t('paymentNotCompleted'),
          );
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
