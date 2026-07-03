import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { CatalogueService } from '../../core/services/catalogue.service';
import { ComboOfferService } from '../../core/services/combo-offer.service';
import { LocaleService } from '../../core/services/locale.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ComboOfferDetail, ComboOfferLineItem } from '../../core/models/combo-offer.models';

import { UiBadgeComponent, UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, IwnCurrencyPipe, UiSkeletonComponent, UiBadgeComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiCardComponent],
  templateUrl: './combo-offer-detail.component.html',
  styleUrl: './combo-offer-detail.component.css',
})
export class ComboOfferDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly combosApi = inject(ComboOfferService);
  private readonly cart = inject(CartService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly combo = signal<ComboOfferDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly adding = signal(false);
  readonly cartMessage = signal<string | null>(null);

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  comboName(combo: ComboOfferDetail): string {
    return this.locale.pick(combo.name, combo.nameAr ?? '');
  }

  itemTitle(item: ComboOfferLineItem): string {
    return item.title;
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set(this.t('comboNotFound'));
      this.loading.set(false);
      return;
    }

    this.combosApi.getActive(id).subscribe({
      next: (combo) => {
        this.combo.set(combo);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('comboNotFound'));
        this.loading.set(false);
      },
    });
  }

  addToCart(): void {
    const c = this.combo();
    if (!c || c.items.length === 0) return;

    const first = c.items[0];
    this.adding.set(true);
    this.cartMessage.set(null);
    this.cart
      .addItem({
        productId: first.productId,
        quantity: 1,
        productVariantId: first.productVariantId,
        comboOfferId: c.id,
      })
      .subscribe({
        next: () => {
          this.cartMessage.set(this.t('comboAddedToCart'));
          this.adding.set(false);
        },
        error: (err) => {
          this.cartMessage.set(err.error?.errors?.[0] ?? this.t('addToCartFailed'));
          this.adding.set(false);
        },
      });
  }
}
