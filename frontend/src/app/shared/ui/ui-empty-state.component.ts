import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center px-6 py-12 text-center">
      <div
        class="mb-4 flex h-14 w-14 items-center justify-center rounded-2xl"
        [class.bg-ink-100]="!danger()"
        [class.text-ink-400]="!danger()"
        [class.bg-danger-soft]="danger()"
        [class.text-danger]="danger()"
      >
        <span class="material-icons !text-[28px]">{{ icon() }}</span>
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
