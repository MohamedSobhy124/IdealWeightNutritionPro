import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UiBadgeComponent } from './ui-badge.component';
import { UiModalComponent } from './ui-modal.component';

@Component({
  selector: 'ui-quick-view',
  standalone: true,
  imports: [RouterLink, IwnCurrencyPipe, UiBadgeComponent, UiModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ui-modal [open]="open()" [heading]="title()" size="lg" (closed)="closed.emit()">
      <div class="grid gap-4 sm:grid-cols-2">
        <div class="overflow-hidden rounded-xl bg-ink-100">
          <img [src]="imageUrl()" [alt]="title()" class="aspect-square w-full object-cover" />
        </div>
        <div class="space-y-3">
          @if (brand()) {
            <p class="text-sm text-ink-500">{{ brand() }}</p>
          }
          <p class="text-xl font-bold text-ink-900">{{ price() | iwnCurrency }}</p>
          @if (listPrice() > price()) {
            <p class="text-sm text-ink-400 line-through">{{ listPrice() | iwnCurrency }}</p>
          }
          <ui-badge [variant]="inStock() ? 'success' : 'danger'" [dot]="true">
            {{ inStock() ? inStockLabel() : outOfStockLabel() }}
          </ui-badge>
          @if (description()) {
            <p class="line-clamp-4 text-sm text-ink-600">{{ description() }}</p>
          }
          <div class="flex flex-wrap gap-2 pt-2">
            <button type="button" class="ui-btn-primary flex-1" [disabled]="!inStock() || busy()" (click)="addToCart.emit()">
              {{ addToCartLabel() }}
            </button>
            <a [routerLink]="['/product', slug()]" class="ui-btn-secondary flex-1 text-center" (click)="closed.emit()">
              {{ viewDetailsLabel() }}
            </a>
          </div>
        </div>
      </div>
    </ui-modal>
  `,
})
export class UiQuickViewComponent {
  readonly open = input(false);
  readonly title = input.required<string>();
  readonly slug = input.required<string>();
  readonly imageUrl = input.required<string>();
  readonly price = input.required<number>();
  readonly listPrice = input(0);
  readonly brand = input<string | null>(null);
  readonly description = input<string | null>(null);
  readonly inStock = input(true);
  readonly busy = input(false);
  readonly inStockLabel = input('In stock');
  readonly outOfStockLabel = input('Out of stock');
  readonly addToCartLabel = input('Add to cart');
  readonly viewDetailsLabel = input('View details');

  readonly closed = output<void>();
  readonly addToCart = output<void>();
}
