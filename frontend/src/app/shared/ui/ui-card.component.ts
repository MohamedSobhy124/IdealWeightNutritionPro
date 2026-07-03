import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="ui-card" [class.ui-card-pad]="padded()">
      @if (heading()) {
        <header
          class="flex items-center justify-between gap-3 border-b border-ink-200 px-5 py-4 sm:px-6"
        >
          <div class="min-w-0">
            <h3 class="truncate text-base font-semibold text-ink-900">{{ heading() }}</h3>
            @if (subheading()) {
              <p class="mt-0.5 truncate text-sm text-ink-500">{{ subheading() }}</p>
            }
          </div>
          <ng-content select="[card-actions]"></ng-content>
        </header>
      }
      <div [class.ui-card-pad]="padded()">
        <ng-content></ng-content>
      </div>
    </section>
  `,
})
export class UiCardComponent {
  readonly heading = input<string | null>(null);
  readonly subheading = input<string | null>(null);
  readonly padded = input(true);
}
