import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CatalogueService } from '../../core/services/catalogue.service';
import { LocaleService } from '../../core/services/locale.service';
import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ServicePurchaseSummary } from '../../core/models/service-purchase.models';
import {
  UiBadgeComponent,
  UiEmptyStateComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  statusVariant,
} from '../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiSkeletonComponent,
    UiEmptyStateComponent,
    UiBadgeComponent,
  ],
  templateUrl: './my-subscriptions.component.html',
  styleUrl: './my-subscriptions.component.css',
})
export class MySubscriptionsComponent implements OnInit {
  private readonly servicesApi = inject(ServiceSubscriptionService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly purchases = signal<ServicePurchaseSummary[]>([]);
  readonly loading = signal(true);
  readonly statusVariant = statusVariant;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  displayTitle(purchase: ServicePurchaseSummary): string {
    return this.locale.pick(purchase.serviceTitle, purchase.serviceTitleAr);
  }

  ngOnInit(): void {
    this.servicesApi.listMyPurchases().subscribe({
      next: (purchases) => {
        this.purchases.set(purchases);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
