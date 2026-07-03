import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'ui-article-card',
  standalone: true,
  imports: [RouterLink],
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
        @if (category()) {
          <p class="mb-1 text-[10px] font-semibold uppercase tracking-wide text-brand-600 sm:text-xs">{{ category() }}</p>
        }
        <h3 class="line-clamp-2 text-sm font-semibold text-ink-900 sm:text-base">
          <a [routerLink]="link()" class="hover:text-brand-700">{{ title() }}</a>
        </h3>
        @if (excerpt()) {
          <p class="mt-1.5 line-clamp-2 text-xs text-ink-500 sm:text-sm">{{ excerpt() }}</p>
        }
        @if (date()) {
          <p class="mt-2 text-[10px] text-ink-400 sm:text-xs">{{ date() }}</p>
        }
      </div>
    </article>
  `,
})
export class UiArticleCardComponent {
  readonly title = input.required<string>();
  readonly link = input.required<string | string[]>();
  readonly imageUrl = input<string | null>(null);
  readonly excerpt = input<string | null>(null);
  readonly category = input<string | null>(null);
  readonly date = input<string | null>(null);
}
