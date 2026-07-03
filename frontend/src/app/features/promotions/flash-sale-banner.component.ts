import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { FlashSaleSummary } from '../../core/models/flash-sale.models';
import { CatalogueService } from '../../core/services/catalogue.service';
import { FlashSaleService } from '../../core/services/flash-sale.service';
import { LocaleService } from '../../core/services/locale.service';
import { FlashSaleCountdownComponent } from './flash-sale-countdown.component';

@Component({
  selector: 'app-flash-sale-banner',
  standalone: true,
  imports: [RouterLink, FlashSaleCountdownComponent],
  templateUrl: './flash-sale-banner.component.html',
  styleUrl: './flash-sale-banner.component.css',
})
export class FlashSaleBannerComponent implements OnInit {
  private readonly flashApi = inject(FlashSaleService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly sale = signal<FlashSaleSummary | null>(null);

  ngOnInit(): void {
    this.flashApi.listActive().subscribe({
      next: (sales) => this.sale.set(sales[0] ?? null),
      error: () => this.sale.set(null),
    });
  }

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  saleName(sale: FlashSaleSummary): string {
    return this.locale.pick(sale.name, sale.nameAr ?? sale.name);
  }
}
