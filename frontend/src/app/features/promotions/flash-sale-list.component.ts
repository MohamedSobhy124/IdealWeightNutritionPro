import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CatalogueService } from '../../core/services/catalogue.service';
import { FlashSaleService } from '../../core/services/flash-sale.service';
import { LocaleService } from '../../core/services/locale.service';
import { FlashSaleCountdownComponent } from './flash-sale-countdown.component';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { FlashSaleSummary } from '../../core/models/flash-sale.models';

import { UiBadgeComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [RouterLink, FlashSaleCountdownComponent, UiPageHeaderComponent, UiSkeletonComponent, UiEmptyStateComponent, UiBadgeComponent],
  templateUrl: './flash-sale-list.component.html',
  styleUrl: './flash-sale-list.component.css',
})
export class FlashSaleListComponent implements OnInit {
  readonly catalogue = inject(CatalogueService);
  private readonly flashSales = inject(FlashSaleService);
  readonly locale = inject(LocaleService);
  private readonly platformId = inject(PLATFORM_ID);

  readonly sales = signal<FlashSaleSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  saleName(sale: FlashSaleSummary): string {
    return this.locale.pick(sale.name, sale.nameAr ?? '');
  }

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.flashSales.listActive().subscribe({
      next: (sales) => {
        this.sales.set(sales);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('flashSalesLoadError'));
        this.loading.set(false);
      },
    });
  }
}
