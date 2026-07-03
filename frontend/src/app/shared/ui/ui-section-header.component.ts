import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'ui-section-header',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mb-4 flex items-end justify-between gap-3">
      <div class="min-w-0">
        <h2 class="text-xl font-bold text-ink-900 sm:text-2xl">{{ title() }}</h2>
        @if (subtitle()) {
          <p class="mt-1 text-sm text-ink-500">{{ subtitle() }}</p>
        }
      </div>
      @if (viewAllLink()) {
        <a [routerLink]="viewAllLink()!" [queryParams]="viewAllQueryParams()" class="shrink-0 text-sm font-semibold text-brand-600 hover:text-brand-700">
          {{ viewAllLabel() }}
        </a>
      }
    </div>
  `,
})
export class UiSectionHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
  readonly viewAllLink = input<string | null>(null);
  readonly viewAllQueryParams = input<Record<string, string> | null>(null);
  readonly viewAllLabel = input('View all');
}
