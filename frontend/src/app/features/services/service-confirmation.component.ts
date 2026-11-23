import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { ServiceCheckoutService } from '../../core/services/service-checkout.service';
import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ServicePurchaseDetail } from '../../core/models/service-purchase.models';
import { buildServicePurchaseTimeline } from '../../core/utils/service-timeline';
import { serviceTypeLabel, statusLabel } from '../../core/utils/status-label';
import { UiBadgeComponent, UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent, UiTimelineComponent, statusVariant } from '../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [RouterLink, IwnCurrencyPipe, UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent, UiBadgeComponent, UiTimelineComponent],
  templateUrl: './service-confirmation.component.html',
  styleUrl: './service-confirmation.component.css',
})
export class ServiceConfirmationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly checkout = inject(ServiceCheckoutService);
  private readonly servicesApi = inject(ServiceSubscriptionService);
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly purchase = signal<ServicePurchaseDetail | null>(null);
  purchaseId = 0;

  readonly statusVariant = statusVariant;

  readonly serviceTitle = () => {
    const p = this.purchase();
    return p ? this.locale.pick(p.serviceTitle, p.serviceTitleAr) : '';
  };

  labelStatus(status: string | null | undefined): string {
    return statusLabel(status, (key) => this.t(key));
  }

  typeLabel(type: string | null | undefined): string {
    return serviceTypeLabel(type, (key) => this.t(key));
  }

  timelineItems(purchase: ServicePurchaseDetail) {
    return buildServicePurchaseTimeline(purchase, (key) => this.t(key));
  }

  /** Guests do not receive the purchase record. Payment already succeeded to reach this page. */
  paidTimeline() {
    return [{ title: this.t('purchaseStepPaid'), completed: true }];
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.purchaseId = Number(params.get('purchaseId'));
      if (!this.purchaseId) {
        this.error.set(this.t('invalidPurchase'));
        this.loading.set(false);
        return;
      }

      this.checkout.completePayment(this.purchaseId).subscribe({
        next: (res) => {
          if (!res.isPaid) {
            this.error.set(res.message ?? this.t('paymentNotCompleted'));
            this.loading.set(false);
            return;
          }

          if (this.auth.isAuthenticated()) {
            this.servicesApi.getMyPurchase(this.purchaseId).subscribe({
              next: (detail) => {
                this.purchase.set(detail);
                this.loading.set(false);
              },
              error: () => this.loading.set(false),
            });
          } else {
            this.loading.set(false);
          }
        },
        error: () => {
          this.error.set(this.t('paymentVerificationFailed'));
          this.loading.set(false);
        },
      });
    });
  }
}
