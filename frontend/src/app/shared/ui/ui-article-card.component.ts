import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'ui-article-card',
  standalone: true,
  imports: [NgOptimizedImage, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="ui-card ui-hover-lift group overflow-hidden border-ink-200 bg-white shadow-none">
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
        @if (category()) {
          <p class="mb-1 text-[10px] font-semibold uppercase tracking-wide text-brand-700 sm:text-xs">{{ category() }}</p>
        }
        <h3 class="line-clamp-2 text-sm font-semibold text-ink-900 sm:text-base">
          <a [routerLink]="link()" class="transition-colors hover:text-brand-700">{{ title() }}</a>
        </h3>
        @if (excerpt()) {
          <p class="mt-1.5 line-clamp-2 text-xs text-ink-500 sm:text-sm">{{ excerpt() }}</p>
        }
        @if (date()) {
          <p class="mt-2 text-[10px] text-ink-600 sm:text-xs">{{ date() }}</p>
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
