import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';

@Component({
  selector: 'ui-service-card',
  standalone: true,
  imports: [RouterLink, IwnCurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="ui-card ui-hover-lift group overflow-hidden">
      @if (imageUrl()) {
        <a [routerLink]="link()" class="block aspect-[16/10] overflow-hidden bg-ink-100">
          <img
            [src]="imageUrl()!"
            [alt]="title()"
            loading="lazy"
            class="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105 motion-reduce:transform-none"
          />
        </a>
      }
      <div class="p-3 sm:p-4">
        <h3 class="line-clamp-2 text-sm font-semibold text-ink-900 sm:text-base">
          <a [routerLink]="link()" class="hover:text-brand-700">{{ title() }}</a>
        </h3>
        @if (subtitle()) {
          <p class="mt-1 line-clamp-2 text-xs text-ink-500 sm:text-sm">{{ subtitle() }}</p>
        }
        <p class="mt-2 text-sm font-bold tabular-nums text-ink-900 sm:text-base">{{ price() | iwnCurrency }}</p>
        <a [routerLink]="link()" class="ui-btn-primary ui-btn-sm mt-3 inline-flex w-full justify-center sm:w-auto">{{ ctaLabel() }}</a>
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
  readonly ctaLabel = input('View details');
}
