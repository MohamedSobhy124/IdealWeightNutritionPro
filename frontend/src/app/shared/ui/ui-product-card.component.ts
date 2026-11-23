import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UiBadgeComponent } from './ui-badge.component';

export type ProductCardDensity = 'compact' | 'comfortable';

@Component({
  selector: 'ui-product-card',
  standalone: true,
  imports: [NgOptimizedImage, RouterLink, IwnCurrencyPipe, UiBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      class="product-card group flex h-full flex-col overflow-hidden"
      [class.product-card--compact]="density() === 'compact'"
    >
      <div class="product-card__media relative aspect-[4/5] sm:aspect-square">
        <div class="product-card__badges absolute start-2.5 top-2.5 z-10 flex flex-col items-start gap-1">
          @if (flashSale()) {
            <ui-badge variant="citrus">{{ flashLabel() }}</ui-badge>
          }
          @if (listPrice() > price()) {
            <ui-badge variant="citrus"><span dir="ltr">-{{ discountPercent() }}%</span></ui-badge>
          }
        </div>

        @if (!inStock()) {
          <span class="product-card__stock absolute start-2.5 bottom-2.5 z-10">
            {{ outOfStockLabel() }}
          </span>
        }

        <button
          type="button"
          class="product-card__wish absolute end-2.5 top-2.5 z-10"
          [class.product-card__wish--on]="inWishlist()"
          [disabled]="busy()"
          (click)="onToggleWishlist($event)"
          [attr.aria-label]="wishlistLabel()"
        >
          <span class="material-icons !text-[18px]" aria-hidden="true">{{ inWishlist() ? 'favorite' : 'favorite_border' }}</span>
        </button>

        <a [routerLink]="['/product', slug()]" class="product-card__image relative block h-full">
          @if (imageUrl()) {
            <img
              [ngSrc]="imageUrl()"
              [alt]="title()"
              fill
              sizes="(max-width: 640px) 50vw, (max-width: 1024px) 33vw, 25vw"
              class="object-contain p-5 motion-reduce:transform-none"
              (load)="markSharpImage($event)"
            />
          }
        </a>
      </div>

      <div class="product-card__body flex flex-1 flex-col gap-1.5 p-3.5 sm:p-4">
        <a [routerLink]="['/product', slug()]" class="block min-w-0 flex-1 no-underline">
          @if (brand()) {
            <p class="product-card__brand">{{ brand() }}</p>
          }
          <h3 class="product-card__title">{{ title() }}</h3>
          @if (rating() > 0) {
            <div class="mt-1 flex items-center gap-1" [attr.aria-label]="rating() + ' / 5'">
              <span class="material-icons !text-[14px] text-warning-fg" aria-hidden="true">star</span>
              <span class="text-xs font-semibold tabular-nums text-ink-800">{{ rating().toFixed(1) }}</span>
              @if (reviewCount() > 0) {
                <span class="text-xs text-ink-600 tabular-nums">({{ reviewCount() }})</span>
              }
            </div>
          }
          <p class="product-card__price mt-2 flex flex-wrap items-baseline gap-1.5 tabular-nums">
            <span
              class="text-[1.05rem] font-extrabold tracking-tight"
              [class.text-citrus]="listPrice() > price()"
              [class.text-ink-900]="listPrice() <= price()"
            >{{ price() | iwnCurrency }}</span>
            @if (listPrice() > price()) {
              <span class="text-xs font-medium text-ink-600 line-through">{{ listPrice() | iwnCurrency }}</span>
            }
          </p>
        </a>

        <div class="product-card__actions mt-auto flex gap-2 pt-2">
          @if (showQuickView()) {
            <button
              type="button"
              class="ui-btn-secondary hidden flex-1 sm:inline-flex"
              [disabled]="busy()"
              (click)="onQuickView($event)"
              [attr.aria-label]="quickViewLabel()"
            >
              {{ quickViewLabel() }}
            </button>
          }

          <button
            type="button"
            class="ui-btn-primary w-full flex-1"
            [disabled]="busy() || !inStock()"
            [attr.aria-busy]="busy()"
            [attr.aria-label]="quickAddLabel()"
            (click)="quickAdd.emit()"
          >
            <span class="material-icons !text-[18px]" aria-hidden="true">add_shopping_cart</span>
            <span class="truncate text-xs font-semibold sm:text-sm">{{ quickAddLabel() }}</span>
          </button>
        </div>
      </div>
    </article>
  `,
  styles: `
    .product-card__stock {
      border-radius: 999px;
      background: rgb(var(--rgb-ink-900) / 0.88);
      color: #fff;
      padding: 0.2rem 0.55rem;
      font-size: 0.7rem;
      font-weight: 700;
      line-height: 1.3;
    }
    .product-card__img--zoom {
      transition: transform 0.5s ease-out;
    }
    .group:hover .product-card__img--zoom {
      transform: scale(1.04);
    }
    @media (prefers-reduced-motion: reduce) {
      .group:hover .product-card__img--zoom {
        transform: none;
      }
    }
    .product-card {
      border-radius: 1.25rem;
      border: 1px solid rgb(var(--rgb-ink-200) / 0.85);
      background: #fff;
      box-shadow: 0 1px 2px rgb(28 25 23 / 0.03);
      transition: box-shadow 0.25s ease, border-color 0.25s ease, transform 0.25s ease;
    }
    .product-card:hover {
      border-color: rgb(var(--rgb-ink-300));
      box-shadow: 0 12px 28px -12px rgb(28 25 23 / 0.14);
    }
    .product-card__media {
      background:
        radial-gradient(circle at 50% 20%, rgb(255 255 255), transparent 55%),
        rgb(var(--rgb-stage));
    }
    .product-card__wish {
      display: inline-flex;
      height: 2.25rem;
      width: 2.25rem;
      align-items: center;
      justify-content: center;
      border-radius: 999px;
      border: 1px solid rgb(var(--rgb-ink-200));
      background: rgb(255 255 255 / 0.95);
      color: rgb(var(--rgb-ink-600));
      cursor: pointer;
      transition: color 0.15s ease, background 0.15s ease, transform 0.15s ease;
    }
    .product-card__wish:hover {
      color: rgb(var(--rgb-brand-solid));
      transform: scale(1.04);
    }
    .product-card__wish--on {
      color: rgb(var(--rgb-danger));
      border-color: rgb(var(--rgb-danger) / 0.25);
      background: rgb(var(--rgb-danger-soft));
    }
    .product-card__brand {
      margin: 0;
      font-size: 0.65rem;
      font-weight: 700;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: rgb(var(--rgb-brand-solid));
    }
    .product-card__title {
      margin: 0;
      display: -webkit-box;
      -webkit-box-orient: vertical;
      -webkit-line-clamp: 2;
      overflow: hidden;
      min-height: 2.5rem;
      font-family: Outfit, Manrope, system-ui, sans-serif;
      font-size: 0.9rem;
      font-weight: 600;
      line-height: 1.35;
      letter-spacing: -0.02em;
      color: rgb(var(--rgb-ink-900));
    }
    .product-card__title:hover {
      color: rgb(var(--rgb-brand-solid));
    }
    :host-context([dir='rtl']) .product-card__title {
      font-family: 'IBM Plex Sans Arabic', Tajawal, system-ui, sans-serif;
      letter-spacing: 0;
    }
    :host-context(.dark) .product-card {
      background: var(--surface-raised);
      border-color: var(--border);
    }
  `,
})
export class UiProductCardComponent {
  readonly title = input.required<string>();
  readonly slug = input.required<string>();
  readonly imageUrl = input.required<string>();
  readonly price = input.required<number>();
  readonly listPrice = input<number>(0);
  readonly brand = input<string | null>(null);
  readonly flashSale = input(false);
  readonly inWishlist = input(false);
  readonly busy = input(false);
  readonly rating = input(0);
  readonly reviewCount = input(0);
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

  /** Skip the hover zoom when the source is too small to scale cleanly. */
  protected markSharpImage(event: Event): void {
    const img = event.target as HTMLImageElement | null;
    if (img && img.naturalWidth >= 200) img.classList.add('product-card__img--zoom');
  }
}
