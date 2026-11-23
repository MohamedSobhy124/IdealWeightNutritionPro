import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center px-6 py-12 text-center">
      <div class="relative mb-5 flex h-20 w-20 items-center justify-center" aria-hidden="true">
        <span
          class="absolute inset-0 rounded-full border border-dashed"
          [class.border-brand-200]="!danger()"
          [class.border-danger]="danger()"
        ></span>
        <span
          class="flex h-14 w-14 items-center justify-center rounded-full shadow-sm ring-1"
          [class.bg-brand-50]="!danger()"
          [class.text-brand-600]="!danger()"
          [class.ring-brand-100]="!danger()"
          [class.bg-danger-soft]="danger()"
          [class.text-danger]="danger()"
          [class.ring-danger-soft]="danger()"
        >
          <span class="material-icons !text-[28px]">{{ icon() }}</span>
        </span>
        @if (!danger()) {
          <span class="absolute -end-1 -top-1 h-3 w-3 rounded-full bg-lime-deep"></span>
          <span class="absolute -bottom-1 -start-1.5 h-2 w-2 rounded-full bg-citrus"></span>
        }
      </div>
      <h3 class="text-base font-semibold text-ink-900">{{ title() }}</h3>
      @if (message()) {
        <p class="mt-1 max-w-sm text-sm text-ink-500">{{ message() }}</p>
      }
      <div class="mt-5 empty:hidden">
        <ng-content></ng-content>
      </div>
    </div>
  `,
})
export class UiEmptyStateComponent {
  readonly icon = input('inbox');
  readonly title = input.required<string>();
  readonly message = input<string | null>(null);
  readonly danger = input(false);
}
