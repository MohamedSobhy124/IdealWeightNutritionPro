import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'ui-section-header',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-6 flex items-end justify-between gap-4">
      <div class="min-w-0">
        @if (eyebrow()) {
          <p class="eyebrow mb-2">{{ eyebrow() }}</p>
        }
        <h2 class="section-title display-title m-0 text-xl text-ink-900 sm:text-2xl lg:text-[1.75rem]">{{ title() }}</h2>
        @if (subtitle()) {
          <p class="mt-1.5 max-w-xl text-sm leading-relaxed text-ink-600">{{ subtitle() }}</p>
        }
      </div>
      @if (viewAllLink()) {
        <a
          [routerLink]="viewAllLink()!"
          [queryParams]="viewAllQueryParams()"
          class="group inline-flex shrink-0 items-center gap-1 text-sm font-semibold text-brand-700 hover:text-brand-800"
        >
          {{ viewAllLabel() }}
          <span class="material-icons !text-[18px] transition-transform group-hover:translate-x-0.5 rtl:rotate-180 rtl:group-hover:-translate-x-0.5" aria-hidden="true">arrow_forward</span>
        </a>
      }
    </div>
  `,
  styles: `
    .section-title::before {
      content: '';
      display: block;
      width: 2.5rem;
      height: 0.25rem;
      margin-bottom: 0.55rem;
      border-radius: 999px;
      background: rgb(var(--rgb-lime));
    }
  `,
})
export class UiSectionHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
  readonly eyebrow = input<string | null>(null);
  readonly viewAllLink = input<string | null>(null);
  readonly viewAllQueryParams = input<Record<string, string> | null>(null);
  readonly viewAllLabel = input('View all');
}
