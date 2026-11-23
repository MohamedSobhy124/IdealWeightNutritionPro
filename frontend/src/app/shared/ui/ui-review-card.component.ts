import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'ui-review-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="ui-card h-full overflow-hidden bg-surface-raised p-5 sm:p-6">
      <div class="mb-3 flex items-center gap-1 text-warning-fg">
        @for (star of stars; track star) {
          <span class="material-icons !text-[18px]">{{ star <= rating() ? 'star' : 'star_border' }}</span>
        }
      </div>
      <p class="text-sm leading-relaxed text-ink-700">"{{ quote() }}"</p>
      <div class="mt-4 flex items-center gap-3 border-t border-ink-100 pt-3">
        <span
          class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-brand-50 text-sm font-bold text-brand-700"
          aria-hidden="true"
        >{{ authorInitial() }}</span>
        <div class="min-w-0">
          <p class="truncate text-sm font-semibold text-ink-900">{{ author() }}</p>
          @if (location()) {
            <p class="truncate text-xs text-ink-600">{{ location() }}</p>
          }
        </div>
      </div>
    </article>
  `,
})
export class UiReviewCardComponent {
  readonly quote = input.required<string>();
  readonly author = input.required<string>();
  readonly location = input<string | null>(null);
  readonly rating = input(5);

  protected readonly stars = [1, 2, 3, 4, 5];

  protected authorInitial(): string {
    const name = this.author()?.trim();
    return name ? name.charAt(0).toUpperCase() : '?';
  }
}
