import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UiBadgeComponent } from './ui-badge.component';

export type ProductCardDensity = 'compact' | 'comfortable';

@Component({
  selector: 'ui-product-card',
  standalone: true,
  imports: [RouterLink, IwnCurrencyPipe, UiBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="ui-card ui-hover-lift group flex h-full flex-col overflow-hidden border border-ink-200/80 bg-white shadow-[0_10px_24px_rgb(15_23_42_/_0.06)]"
      [class.ui-product-card--compact]="density() === 'compact'"
    >
      <div class="relative aspect-[5/6] bg-gradient-to-b from-ink-100 to-ink-50 sm:aspect-square">
        <div
          class="ui-product-card__badge-stack absolute start-2 top-2 z-10 flex max-w-[calc(100%-2.75rem)] flex-col items-start gap-0.5"
          [class.start-1.5]="density() === 'compact'"
          [class.top-1.5]="density() === 'compact'"
          [class.max-w-[calc(100%-2.25rem)]]="density() === 'compact'"
          [class.flex-row]="density() === 'compact'"
          [class.flex-wrap]="density() === 'compact'"
        >
          @if (flashSale()) {
            <ui-badge variant="danger">{{ flashLabel() }}</ui-badge>
          }
          @if (listPrice() > price()) {
            <ui-badge variant="success">-{{ discountPercent() }}%</ui-badge>
          }
        </div>

        @if (!inStock()) {
          <span class="absolute bottom-2 start-2 z-10">
            <ui-badge variant="neutral">{{ outOfStockLabel() }}</ui-badge>
          </span>
        }

        <button
          type="button"
          class="ui-btn-icon ui-product-card__wishlist absolute end-2 top-2 z-10 bg-white/95 shadow-sm"
          [class.text-danger]="inWishlist()"
          [disabled]="busy()"
          (click)="onToggleWishlist($event)"
          [attr.aria-label]="wishlistLabel()"
        >
          <span class="material-icons text-[18px]" aria-hidden="true">{{ inWishlist() ? 'favorite' : 'favorite_border' }}</span>
        </button>

        <a [routerLink]="['/product', slug()]" class="block h-full">
          <img
            [src]="imageUrl()"
            [alt]="title()"
            width="400"
            height="400"
            loading="lazy"
            class="h-full w-full object-contain p-3 transition-transform duration-300 group-hover:scale-105 motion-reduce:transform-none sm:object-cover sm:p-0"
          />
        </a>
      </div>

      <div class="ui-product-card__body flex flex-1 flex-col p-3 sm:p-3.5">
        <a [routerLink]="['/product', slug()]" class="block min-w-0 flex-1 no-underline">
          <h3 class="ui-product-card__title line-clamp-2 min-h-[2.5rem] break-words text-sm font-semibold leading-5 text-ink-900 text-pretty hover:text-brand-700 sm:min-h-0">
            {{ title() }}
          </h3>
          <p class="ui-product-card__price mt-1.5 flex flex-wrap items-baseline gap-1.5 tabular-nums">
            <span class="text-base font-extrabold tracking-tight text-ink-900" [class.text-danger]="listPrice() > price()">{{ price() | iwnCurrency }}</span>
            @if (listPrice() > price()) {
              <span class="text-[11px] font-medium text-ink-400 line-through sm:text-xs">{{ listPrice() | iwnCurrency }}</span>
            }
          </p>
        </a>

        @if (rating() > 0) {
          <div class="mt-1 hidden items-center gap-1 text-warning-fg sm:flex" [attr.aria-label]="rating() + ' / 5'">
            @for (star of stars; track star) {
              <span class="material-icons text-[14px]" aria-hidden="true">{{ star <= rating() ? 'star' : 'star_border' }}</span>
            }
            <span class="text-xs text-ink-400 tabular-nums">{{ rating().toFixed(1) }}</span>
          </div>
        }

        <div
          class="ui-product-card__actions mt-auto flex gap-2 pt-2.5"
          [class.pt-1.5]="density() === 'compact'"
        >
          @if (showQuickView()) {
            <button
              type="button"
              class="ui-btn-secondary hidden flex-1 sm:inline-flex md:opacity-0 md:transition-opacity md:duration-150 md:group-hover:opacity-100 md:group-focus-within:opacity-100"
              [disabled]="busy()"
              (click)="onQuickView($event)"
              [attr.aria-label]="quickViewLabel()"
            >
              @if (density() === 'compact') {
                <span class="hidden sm:inline">{{ quickViewLabel() }}</span>
                <span class="material-icons text-[16px] sm:hidden" aria-hidden="true">visibility</span>
              } @else {
                {{ quickViewLabel() }}
              }
            </button>
          }

          <button
            type="button"
            class="ui-btn-primary w-full flex-1 rounded-xl shadow-sm sm:w-auto"
            [class.md:opacity-0]="!alwaysShowActions()"
            [class.md:transition-opacity]="!alwaysShowActions()"
            [class.md:duration-150]="!alwaysShowActions()"
            [class.md:group-hover:opacity-100]="!alwaysShowActions()"
            [class.md:group-focus-within:opacity-100]="!alwaysShowActions()"
            [disabled]="busy() || !inStock()"
            [attr.aria-busy]="busy()"
            [attr.aria-label]="quickAddLabel()"
            (click)="quickAdd.emit()"
          >
            @if (density() === 'compact') {
              <span class="material-icons text-[16px]" aria-hidden="true">add_shopping_cart</span>
              <span class="truncate text-[11px] font-semibold sm:text-xs">{{ quickAddLabel() }}</span>
            } @else {
              {{ quickAddLabel() }}
            }
          </button>
        </div>
      </div>
    </article>
  `,
})
export class UiProductCardComponent {
  readonly title = input.required<string>();
  readonly slug = input.required<string>();
  readonly imageUrl = input.required<string>();
  readonly price = input.required<number>();
  readonly listPrice = input<number>(0);
  readonly flashSale = input(false);
  readonly inWishlist = input(false);
  readonly busy = input(false);
  readonly rating = input(0);
  readonly inStock = input(true);
  readonly showQuickView = input(false);
  readonly density = input<ProductCardDensity>('compact');
  readonly alwaysShowActions = input(false);
  readonly quickAddLabel = input('View');
  readonly quickViewLabel = input('Quick view');
  readonly flashLabel = input('Flash');
  readonly wishlistLabel = input('Wishlist');
  readonly outOfStockLabel = input('Out of stock');

  readonly quickAdd = output<void>();
  readonly quickView = output<void>();
  readonly toggleWishlist = output<Event>();

  protected readonly stars = [1, 2, 3, 4, 5];

  protected readonly discountPercent = computed(() => {
    const list = this.listPrice();
    const price = this.price();
    if (!list || list <= price) return 0;
    return Math.round(((list - price) / list) * 100);
  });

  protected onToggleWishlist(event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    this.toggleWishlist.emit(event);
  }

  protected onQuickView(event: Event): void {
    event.stopPropagation();
    event.preventDefault();
    this.quickView.emit();
  }
}
