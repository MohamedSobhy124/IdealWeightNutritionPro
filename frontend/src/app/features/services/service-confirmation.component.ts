import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { ServiceCheckoutService } from '../../core/services/service-checkout.service';
import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ServicePurchaseDetail } from '../../core/models/service-purchase.models';
import { UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [RouterLink, IwnCurrencyPipe, UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent],
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

  readonly serviceTitle = () => {
    const p = this.purchase();
    return p ? this.locale.pick(p.serviceTitle, p.serviceTitleAr) : '';
  };

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.purchaseId = Number(params.get('purchaseId'));
      if (!this.purchaseId) {
        this.error.set('Invalid purchase.');
        this.loading.set(false);
        return;
      }

      this.checkout.completePayment(this.purchaseId).subscribe({
        next: (res) => {
          if (!res.isPaid) {
            this.error.set(res.message ?? 'Payment was not completed.');
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
          this.error.set('Payment verification failed.');
          this.loading.set(false);
        },
      });
    });
  }
}
