import { DatePipe, DecimalPipe, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CatalogueService } from '../../core/services/catalogue.service';
import { ComboOfferService } from '../../core/services/combo-offer.service';
import { LocaleService } from '../../core/services/locale.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ComboOfferSummary } from '../../core/models/combo-offer.models';
import { FlashSaleCountdownComponent } from './flash-sale-countdown.component';

import { UiBadgeComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, IwnCurrencyPipe, FlashSaleCountdownComponent, UiPageHeaderComponent, UiSkeletonComponent, UiEmptyStateComponent, UiBadgeComponent],
  templateUrl: './combo-offer-list.component.html',
  styleUrl: './combo-offer-list.component.css',
})
export class ComboOfferListComponent implements OnInit {
  readonly catalogue = inject(CatalogueService);
  private readonly combosApi = inject(ComboOfferService);
  readonly locale = inject(LocaleService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly combos = signal<ComboOfferSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  comboName(combo: ComboOfferSummary): string {
    return this.locale.pick(combo.name, combo.nameAr ?? '');
  }

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.combosApi.listActive().subscribe({
      next: (combos) => {
        this.combos.set(combos);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('combosLoadError'));
        this.loading.set(false);
      },
    });
  }
}
