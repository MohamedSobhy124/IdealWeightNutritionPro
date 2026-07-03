import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'ui-pagination',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalPages() > 1) {
      <div class="mt-6 flex flex-col items-center justify-between gap-3 sm:flex-row">
        <p class="text-sm tabular-nums text-ink-500">
          {{ rangeStart() }}–{{ rangeEnd() }} {{ ofLabel() }} {{ totalCount() }}
        </p>
        <div class="flex items-center gap-1">
          <button
            type="button"
            class="ui-btn-icon min-h-11 min-w-11 disabled:opacity-40"
            [disabled]="page() <= 1"
            (click)="pageChange.emit(page() - 1)"
            [attr.aria-label]="prevLabel()"
          >
            <span class="material-icons !text-[20px] rtl:rotate-180" aria-hidden="true">chevron_left</span>
          </button>
          <span class="px-2 text-sm font-medium tabular-nums text-ink-700">{{ page() }} / {{ totalPages() }}</span>
          <button
            type="button"
            class="ui-btn-icon min-h-11 min-w-11 disabled:opacity-40"
            [disabled]="page() >= totalPages()"
            (click)="pageChange.emit(page() + 1)"
            [attr.aria-label]="nextLabel()"
          >
            <span class="material-icons !text-[20px] rtl:rotate-180" aria-hidden="true">chevron_right</span>
          </button>
        </div>
      </div>
    }
  `,
})
export class UiPaginationComponent {
  readonly page = input.required<number>();
  readonly pageSize = input(24);
  readonly totalCount = input.required<number>();
  readonly ofLabel = input('of');
  readonly prevLabel = input('Previous page');
  readonly nextLabel = input('Next page');

  readonly pageChange = output<number>();

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize()))
  );

  protected readonly rangeStart = computed(() =>
    this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1
  );

  protected readonly rangeEnd = computed(() =>
    Math.min(this.page() * this.pageSize(), this.totalCount())
  );
}
