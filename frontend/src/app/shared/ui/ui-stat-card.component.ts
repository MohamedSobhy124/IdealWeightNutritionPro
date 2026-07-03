import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { UiSkeletonComponent } from './ui-skeleton.component';

export type UiTrendDirection = 'up' | 'down' | 'flat';

@Component({
  selector: 'ui-stat-card',
  standalone: true,
  imports: [UiSkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ui-card ui-card-pad">
      <div class="flex items-start justify-between gap-3">
        <div class="min-w-0">
          <p class="truncate text-sm font-medium text-ink-500">{{ label() }}</p>
          @if (loading()) {
            <div class="mt-2"><ui-skeleton width="6rem" height="1.75rem" /></div>
          } @else {
            <p class="mt-1.5 text-2xl font-bold tracking-tight text-ink-900">{{ value() }}</p>
          }
          @if (hint() && !loading()) {
            <p class="mt-1 text-xs text-ink-400">{{ hint() }}</p>
          }
        </div>
        @if (icon()) {
          <div
            class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-brand-50 text-brand-600"
          >
            <span class="material-icons !text-[22px]">{{ icon() }}</span>
          </div>
        }
      </div>
      @if (trend() !== null && !loading()) {
        <div class="mt-3 flex items-center gap-1.5 text-xs font-medium" [class]="trendClass()">
          <span class="material-icons !text-[16px]">{{ trendIcon() }}</span>
          <span>{{ trend() }}</span>
          @if (trendLabel()) {
            <span class="font-normal text-ink-400">{{ trendLabel() }}</span>
          }
        </div>
      }
    </div>
  `,
})
export class UiStatCardComponent {
  readonly label = input.required<string>();
  readonly value = input<string | number>('');
  readonly icon = input<string | null>(null);
  readonly hint = input<string | null>(null);
  readonly loading = input(false);
  readonly trend = input<string | null>(null);
  readonly trendLabel = input<string | null>(null);
  readonly trendDirection = input<UiTrendDirection>('flat');

  protected readonly trendIcon = computed(() => {
    switch (this.trendDirection()) {
      case 'up':
        return 'trending_up';
      case 'down':
        return 'trending_down';
      default:
        return 'trending_flat';
    }
  });

  protected readonly trendClass = computed(() => {
    switch (this.trendDirection()) {
      case 'up':
        return 'text-success-fg';
      case 'down':
        return 'text-danger';
      default:
        return 'text-ink-500';
    }
  });
}
