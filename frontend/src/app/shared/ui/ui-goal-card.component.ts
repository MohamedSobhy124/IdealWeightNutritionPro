import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'ui-goal-card',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (image()) {
      <!-- Photo tile: brand lifestyle image under a forest scrim. Falls back to an
           elegant deep-forest tile when the image file is not present. -->
      <a
        class="ui-card group relative flex min-h-[11.5rem] flex-col justify-end overflow-hidden !border-transparent bg-[rgb(var(--rgb-forest-deep))] p-5 transition-[box-shadow,transform] duration-200 hover:-translate-y-0.5 hover:shadow-elevated"
        [routerLink]="link()"
        [queryParams]="queryParams()"
      >
        <span
          class="absolute inset-0 bg-cover bg-center transition-transform duration-300 ease-out group-hover:scale-[1.04]"
          [style.background-image]="'url(' + image() + ')'"
          aria-hidden="true"
        ></span>
        <span
          class="absolute inset-0 bg-gradient-to-t from-[rgb(var(--rgb-forest-deep)/0.88)] via-[rgb(var(--rgb-forest-deep)/0.28)] to-transparent"
          aria-hidden="true"
        ></span>
        <span
          class="absolute start-4 top-4 flex h-10 w-10 items-center justify-center rounded-xl bg-white/15 text-white ring-1 ring-white/25 backdrop-blur-sm"
        >
          <span class="material-icons !text-[20px]" aria-hidden="true">{{ icon() }}</span>
        </span>
        <span class="relative z-10">
          <span class="display-title block text-lg text-white">{{ title() }}</span>
          @if (subtitle()) {
            <span class="mt-1 block text-sm leading-relaxed text-white/75">{{ subtitle() }}</span>
          }
        </span>
      </a>
    } @else {
      <!-- Quiet fallback tile when no imagery is available. -->
      <a
        class="ui-card group flex min-h-[6.5rem] items-center gap-4 bg-surface-raised p-5 transition-[border-color,box-shadow,transform] duration-200 hover:border-brand-100 hover:shadow-sm sm:min-h-[7rem] sm:gap-5 sm:p-5"
        [routerLink]="link()"
        [queryParams]="queryParams()"
      >
        <span class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-brand-50 text-brand-700 ring-1 ring-brand-100/80 transition-colors duration-200 group-hover:bg-brand-100/70">
          <span class="material-icons !text-[26px]">{{ icon() }}</span>
        </span>
        <div class="min-w-0">
          <p class="display-title m-0 text-[1.05rem] text-ink-900 group-hover:text-brand-700 sm:text-lg">{{ title() }}</p>
          @if (subtitle()) {
            <p class="mt-1 text-sm leading-relaxed text-ink-500">{{ subtitle() }}</p>
          }
        </div>
      </a>
    }
  `,
})
export class UiGoalCardComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
  readonly icon = input('fitness_center');
  readonly image = input<string | null>(null);
  readonly link = input<string | string[]>('/shop');
  readonly queryParams = input<Record<string, string> | null>(null);
}
