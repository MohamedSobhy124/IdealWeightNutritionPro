import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';

@Component({
  selector: 'ui-service-card',
  standalone: true,
  imports: [NgOptimizedImage, RouterLink, IwnCurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="ui-card ui-hover-lift group overflow-hidden">
      @if (imageUrl()) {
        <a [routerLink]="link()" class="relative block aspect-[16/10] overflow-hidden bg-stage">
          <img
            [ngSrc]="imageUrl()!"
            [alt]="title()"
            fill
            sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, 33vw"
            class="object-cover transition-transform duration-300 group-hover:scale-105 motion-reduce:transform-none"
          />
        </a>
      }
      <div class="p-3 sm:p-4">
        <h3 class="line-clamp-2 text-sm font-semibold text-ink-900 sm:text-base">
          <a [routerLink]="link()" class="hover:text-brand-700">{{ title() }}</a>
        </h3>
        @if (subtitle()) {
        <p class="mt-1 line-clamp-3 text-xs text-ink-600 sm:text-sm">{{ subtitle() }}</p>
        }
        @if (meta()) {
          <p class="mt-1 text-xs font-semibold uppercase tracking-wide text-brand-700">{{ meta() }}</p>
        }
        <p class="mt-2 flex flex-wrap items-center gap-2 text-sm font-bold tabular-nums sm:text-base">
          <span
            [class.text-citrus]="comparePrice() != null && comparePrice()! > price()"
            [class.text-ink-900]="!(comparePrice() != null && comparePrice()! > price())"
          >{{ price() | iwnCurrency }}</span>
          @if (comparePrice() != null && comparePrice()! > price()) {
            <span class="text-xs font-medium text-ink-600 line-through">{{ comparePrice() | iwnCurrency }}</span>
          }
          @if (badge()) {
            <span class="ui-badge-citrus">{{ badge() }}</span>
          }
        </p>
        <a [routerLink]="link()" class="ui-btn-primary mt-3 inline-flex w-full justify-center sm:w-auto">{{ ctaLabel() }}</a>
      </div>
    </article>
  `,
})
export class UiServiceCardComponent {
  readonly title = input.required<string>();
  readonly link = input.required<string | string[]>();
  readonly price = input.required<number>();
  readonly imageUrl = input<string | null>(null);
  readonly subtitle = input<string | null>(null);
  readonly meta = input<string | null>(null);
  readonly comparePrice = input<number | null>(null);
  readonly badge = input<string | null>(null);
  readonly ctaLabel = input('View details');
}
