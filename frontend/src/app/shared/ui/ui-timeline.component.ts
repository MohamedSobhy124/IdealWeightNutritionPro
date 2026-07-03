import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';

export interface UiTimelineItem {
  title: string;
  subtitle?: string | null;
  date?: string | null;
  active?: boolean;
  completed?: boolean;
}

@Component({
  selector: 'ui-timeline',
  standalone: true,
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ol class="space-y-0" [attr.aria-label]="ariaLabel()">
      @for (item of items(); track $index; let last = $last) {
        <li class="relative flex gap-4 pb-6" [class.pb-0]="last">
          @if (!last) {
            <span class="absolute start-[15px] top-8 h-[calc(100%-1rem)] w-0.5 bg-ink-200" aria-hidden="true"></span>
          }
          <span
            class="relative z-10 flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm font-bold transition-transform"
            [class.bg-brand-600]="item.active || item.completed"
            [class.text-white]="item.active || item.completed"
            [class.bg-ink-100]="!item.active && !item.completed"
            [class.text-ink-500]="!item.active && !item.completed"
            [class.ui-timeline-pulse]="item.active"
          >
            @if (item.completed && !item.active) {
              <span class="material-icons !text-[18px]">check</span>
            } @else {
              {{ $index + 1 }}
            }
          </span>
          <div class="min-w-0 flex-1 pt-0.5">
            <p class="font-semibold text-ink-900" [class.text-brand-600]="item.active">{{ item.title }}</p>
            @if (item.subtitle) {
              <p class="mt-0.5 text-sm text-ink-500">{{ item.subtitle }}</p>
            }
            @if (item.date) {
              <p class="mt-0.5 text-xs text-ink-400">{{ item.date | date: 'medium' }}</p>
            }
          </div>
        </li>
      }
    </ol>
  `,
})
export class UiTimelineComponent {
  readonly items = input.required<UiTimelineItem[]>();
  readonly ariaLabel = input('Timeline');
}
