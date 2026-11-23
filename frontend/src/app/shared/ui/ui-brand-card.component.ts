import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'ui-brand-card',
  standalone: true,
  imports: [NgOptimizedImage, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a
      class="ui-card group flex min-h-[44px] flex-col items-center gap-2 p-3 text-center transition-[border-color,box-shadow,transform] duration-200 hover:-translate-y-0.5 hover:border-brand-100 hover:bg-brand-50/40 hover:shadow-sm"
      [routerLink]="link()"
      [queryParams]="queryParams()"
    >
      @if (imageUrl()) {
        <span class="relative flex h-16 w-full items-center justify-center overflow-hidden rounded-lg bg-ink-50 px-2 ring-1 ring-transparent transition-all group-hover:ring-brand-100">
          <img
            [ngSrc]="imageUrl()!"
            [alt]="name()"
            width="120"
            height="56"
            class="max-h-14 w-auto object-contain transition-transform duration-300 group-hover:scale-105"
          />
        </span>
        <span class="line-clamp-2 text-xs font-medium text-ink-800 group-hover:text-brand-700">{{ name() }}</span>
      } @else {
        <span class="flex min-h-16 w-full items-center justify-center rounded-lg bg-stage px-3 text-sm font-semibold text-ink-800">
          {{ name() }}
        </span>
      }
    </a>
  `,
})
export class UiBrandCardComponent {
  readonly name = input.required<string>();
  readonly imageUrl = input<string | null>(null);
  readonly link = input<string | string[]>('/shop');
  readonly queryParams = input<Record<string, string | number> | null>(null);
}
