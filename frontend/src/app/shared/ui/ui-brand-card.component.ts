import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'ui-brand-card',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a
      class="ui-card group flex min-h-[44px] flex-col items-center gap-2 p-3 text-center transition-all hover:-translate-y-0.5 hover:shadow-elevated hover:ring-2 hover:ring-indigo-200"
      [routerLink]="link()"
      [queryParams]="queryParams()"
    >
      @if (imageUrl()) {
        <span class="flex h-16 w-full items-center justify-center overflow-hidden rounded-lg bg-ink-50 px-2 ring-1 ring-transparent transition-all group-hover:ring-indigo-200">
          <img [src]="imageUrl()!" [alt]="name()" loading="lazy" class="max-h-14 w-auto object-contain transition-transform duration-300 group-hover:scale-105" />
        </span>
      } @else {
        <span class="flex h-16 w-16 items-center justify-center rounded-full bg-brand-50 text-brand-600 transition-colors group-hover:bg-indigo-100 group-hover:text-indigo-600">
          <span class="material-icons">storefront</span>
        </span>
      }
      <span class="line-clamp-2 text-xs font-medium text-ink-800 group-hover:text-indigo-700">{{ name() }}</span>
    </a>
  `,
})
export class UiBrandCardComponent {
  readonly name = input.required<string>();
  readonly imageUrl = input<string | null>(null);
  readonly link = input<string | string[]>('/shop');
  readonly queryParams = input<Record<string, string | number> | null>(null);
}
