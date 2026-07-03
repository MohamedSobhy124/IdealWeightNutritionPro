import { DatePipe } from '@angular/common';

import { Component, inject, OnInit, signal, computed } from '@angular/core';

import { ActivatedRoute, RouterLink } from '@angular/router';

import { CatalogueService } from '../../core/services/catalogue.service';

import { LocaleService } from '../../core/services/locale.service';

import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';

import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';

import { UI, UiKey } from '../../core/i18n/ui-text';

import { ServiceSubscriptionDetail } from '../../core/models/service.models';

import { ProductReviewsComponent } from '../catalogue/product-reviews.component';

import { UiBadgeComponent, UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';



@Component({

  standalone: true,

  imports: [RouterLink, DatePipe, ProductReviewsComponent, IwnCurrencyPipe, UiBadgeComponent, UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent],

  templateUrl: './service-detail.component.html',

  styleUrl: './service-detail.component.css',

})

export class ServiceDetailComponent implements OnInit {

  private readonly route = inject(ActivatedRoute);

  private readonly servicesApi = inject(ServiceSubscriptionService);

  readonly catalogue = inject(CatalogueService);

  readonly locale = inject(LocaleService);



  readonly service = signal<ServiceSubscriptionDetail | null>(null);

  readonly loading = signal(true);

  readonly error = signal<string | null>(null);



  readonly title = computed(() => {

    const s = this.service();

    return s ? this.locale.pick(s.title, s.titleAr) : '';

  });



  readonly description = computed(() => {
    const s = this.service();
    return s ? this.locale.pick(s.description ?? '', s.descriptionAr) : '';
  });

  readonly displayPrice = computed(() => {
    const s = this.service();
    if (!s) return 0;
    return s.salePrice != null && s.hasActiveOffer ? s.salePrice : s.price;
  });



  t(key: UiKey): string {

    return this.locale.pick(UI[key].en, UI[key].ar);

  }



  ngOnInit(): void {

    this.route.paramMap.subscribe((params) => {

      const id = Number(params.get('id'));

      if (!id) {

        this.error.set(this.t('serviceNotFound'));

        this.loading.set(false);

        return;

      }



      this.loading.set(true);

      this.error.set(null);

      this.servicesApi.getService(id).subscribe({

        next: (service) => {

          this.service.set(service);

          this.loading.set(false);

        },

        error: () => {

          this.error.set(this.t('serviceNotFound'));

          this.loading.set(false);

        },

      });

    });

  }

}

