import { ChangeDetectionStrategy, Component, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

import { CatalogueService } from '../../core/services/catalogue.service';

import { LocaleService } from '../../core/services/locale.service';

import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';

import { UI, UiKey } from '../../core/i18n/ui-text';

import { ServiceSubscriptionSummary } from '../../core/models/service.models';

import { serviceTypeLabel } from '../../core/utils/status-label';

import {

  UiEmptyStateComponent,

  UiPageHeaderComponent,

  UiServiceCardComponent,

  UiSkeletonComponent,

} from '../../shared/ui';



@Component({

  changeDetection: ChangeDetectionStrategy.OnPush,

  standalone: true,

  imports: [UiPageHeaderComponent, UiServiceCardComponent, UiSkeletonComponent, UiEmptyStateComponent],

  templateUrl: './service-list.component.html',

  styleUrl: './service-list.component.css',

})

export class ServiceListComponent implements OnInit {
  private readonly platformId = inject(PLATFORM_ID);

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



  typeLabel(type: string | null | undefined): string {

    return serviceTypeLabel(type, (key) => this.t(key));

  }



  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

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

