import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'ui-goal-card',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a
      class="ui-card group flex min-h-[88px] items-center gap-4 p-4 transition-shadow hover:shadow-elevated"
      [routerLink]="link()"
      [queryParams]="queryParams()"
    >
      <span class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-brand-600">
        <span class="material-icons">{{ icon() }}</span>
      </span>
      <div class="min-w-0">
        <p class="font-semibold text-ink-900 group-hover:text-brand-700">{{ title() }}</p>
        @if (subtitle()) {
          <p class="mt-0.5 text-sm text-ink-500">{{ subtitle() }}</p>
        }
      </div>
    </a>
  `,
})
export class UiGoalCardComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
  readonly icon = input('fitness_center');
  readonly link = input<string | string[]>('/shop');
  readonly queryParams = input<Record<string, string> | null>(null);
}
