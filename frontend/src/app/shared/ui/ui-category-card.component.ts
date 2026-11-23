import { NgClass, NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Editorial category tile: full-bleed photo (or branded gradient fallback)
 * under a forest scrim, with a deterministic accent bar derived from the
 * category id so each category keeps a stable identity in EN and AR.
 */
@Component({
  selector: 'ui-category-card',
  standalone: true,
  imports: [NgClass, NgOptimizedImage, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a
      class="group relative block aspect-[4/3] overflow-hidden rounded-2xl bg-stage transition-[box-shadow,transform] duration-200 hover:-translate-y-0.5 hover:shadow-elevated"
      [routerLink]="link()"
      [queryParams]="queryParams()"
    >
      @if (imageUrl()) {
        <img
          [ngSrc]="imageUrl()!"
          [alt]="name()"
          fill
          sizes="(min-width: 1024px) 17vw, (min-width: 640px) 31vw, 46vw"
          class="object-cover object-center transition-transform duration-300 ease-out motion-reduce:transform-none group-hover:scale-[1.03]"
        />
      } @else {
        <span class="absolute inset-0" [ngClass]="fallbackBg()"></span>
        <span class="absolute inset-0 flex items-center justify-center" aria-hidden="true">
          <span class="material-icons !text-[44px] text-white/50">category</span>
        </span>
      }
      <span
        class="pointer-events-none absolute inset-x-0 bottom-0 h-2/3 bg-gradient-to-t from-[rgb(var(--rgb-forest-deep)/0.9)] via-[rgb(var(--rgb-forest-deep)/0.28)] to-transparent"
        aria-hidden="true"
      ></span>
      <span class="absolute inset-x-3 top-0 h-1 rounded-b-full" [ngClass]="accentBar()" aria-hidden="true"></span>
      <span class="absolute inset-x-3 bottom-3 z-10 line-clamp-2 text-sm font-bold leading-snug text-white sm:text-[0.95rem]">
        {{ name() }}
      </span>
    </a>
  `,
})
export class UiCategoryCardComponent {
  readonly name = input.required<string>();
  readonly imageUrl = input<string | null>(null);
  readonly link = input<string | string[]>('/shop');
  readonly queryParams = input<Record<string, string | number> | null>(null);

  private static readonly accents = [
    { bar: 'bg-[rgb(var(--rgb-lime))]', fallback: 'bg-[linear-gradient(150deg,rgb(var(--rgb-brand-solid)),rgb(var(--rgb-forest-deep)))]' },
    { bar: 'bg-[#F59E0B]', fallback: 'bg-[linear-gradient(150deg,#7c4a12,#3a2410)]' },
    { bar: 'bg-[rgb(var(--rgb-citrus))]', fallback: 'bg-[linear-gradient(150deg,#7c3a12,#2e1a0c)]' },
    { bar: 'bg-[rgb(var(--rgb-brand-200))]', fallback: 'bg-[linear-gradient(150deg,rgb(var(--rgb-forest-deep)),rgb(var(--rgb-brand-solid)))]' },
  ];

  readonly accent = computed(() => {
    const raw = this.queryParams()?.['categoryId'];
    const id = typeof raw === 'number' ? raw : Number.parseInt(String(raw ?? ''), 10);
    const index = Number.isFinite(id) ? Math.abs(id) % UiCategoryCardComponent.accents.length : 0;
    return UiCategoryCardComponent.accents[index];
  });

  readonly accentBar = computed(() => this.accent().bar);
  readonly fallbackBg = computed(() => this.accent().fallback);
}
