import { Component, inject, OnInit, signal } from '@angular/core';

import { CatalogueService } from '../../core/services/catalogue.service';

import { LocaleService } from '../../core/services/locale.service';

import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';

import { UI, UiKey } from '../../core/i18n/ui-text';

import { ServiceSubscriptionSummary } from '../../core/models/service.models';

import {

  UiEmptyStateComponent,

  UiPageHeaderComponent,

  UiServiceCardComponent,

  UiSkeletonComponent,

} from '../../shared/ui';



@Component({

  standalone: true,

  imports: [UiPageHeaderComponent, UiServiceCardComponent, UiSkeletonComponent, UiEmptyStateComponent],

  templateUrl: './service-list.component.html',

  styleUrl: './service-list.component.css',

})

export class ServiceListComponent implements OnInit {

  private readonly servicesApi = inject(ServiceSubscriptionService);

  readonly catalogue = inject(CatalogueService);

  readonly locale = inject(LocaleService);



  readonly services = signal<ServiceSubscriptionSummary[]>([]);

  readonly loading = signal(true);

  readonly error = signal<string | null>(null);



  t(key: UiKey): string {

    return this.locale.pick(UI[key].en, UI[key].ar);

  }



  displayTitle(service: ServiceSubscriptionSummary): string {

    return this.locale.pick(service.title, service.titleAr);

  }



  displayDescription(service: ServiceSubscriptionSummary): string {

    return this.locale.pick(service.description ?? '', service.descriptionAr);

  }



  ngOnInit(): void {

    this.servicesApi.listServices().subscribe({

      next: (services) => {

        this.services.set(services);

        this.loading.set(false);

      },

      error: () => {

        this.error.set(this.t('servicesLoadError'));

        this.loading.set(false);

      },

    });

  }

}

