import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-review-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="ui-card ui-card-pad h-full">
      <div class="mb-2 flex items-center gap-1 text-warning-fg">
        @for (star of stars; track star) {
          <span class="material-icons !text-[18px]">{{ star <= rating() ? 'star' : 'star_border' }}</span>
        }
      </div>
      <p class="text-sm leading-relaxed text-ink-700">"{{ quote() }}"</p>
      <p class="mt-3 text-sm font-semibold text-ink-900">{{ author() }}</p>
      @if (location()) {
        <p class="text-xs text-ink-400">{{ location() }}</p>
      }
    </article>
  `,
})
export class UiReviewCardComponent {
  readonly quote = input.required<string>();
  readonly author = input.required<string>();
  readonly location = input<string | null>(null);
  readonly rating = input(5);

  protected readonly stars = [1, 2, 3, 4, 5];
}
