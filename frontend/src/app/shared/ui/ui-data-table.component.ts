import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  effect,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UiCellDirective } from './ui-cell.directive';
import { UiEmptyStateComponent } from './ui-empty-state.component';
import { UiSkeletonComponent } from './ui-skeleton.component';

export interface UiTableColumn {
  key: string;
  header: string;
  sortable?: boolean;
  align?: 'start' | 'center' | 'end';
  width?: string;
  /** Hide this column on the mobile card layout. */
  hideOnMobile?: boolean;
  /** Exclude from CSV export. */
  noExport?: boolean;
}

type SortDir = 'asc' | 'desc' | null;

@Component({
  selector: 'ui-data-table',
  standalone: true,
  imports: [FormsModule, NgTemplateOutlet, UiSkeletonComponent, UiEmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ui-card overflow-hidden">
      <!-- Toolbar -->
      <div class="flex flex-col gap-3 border-b border-ink-200 p-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="flex flex-1 items-center gap-2">
          @if (searchable()) {
            <div class="relative w-full max-w-xs">
              <span
                class="material-icons pointer-events-none absolute start-3 top-1/2 -translate-y-1/2 !text-[20px] text-ink-400"
                aria-hidden="true"
                >search</span
              >
              <input
                type="search"
                class="ui-input ps-10"
                [placeholder]="searchPlaceholder()"
                [attr.aria-label]="searchAriaLabel()"
                [ngModel]="search()"
                (ngModelChange)="onSearch($event)"
              />
            </div>
          }
          <ng-content select="[table-toolbar]"></ng-content>
        </div>
        <div class="flex items-center gap-2">
          @if (selectedCount() > 0) {
            <span class="text-sm text-ink-500">{{ selectedCount() }} {{ selectedLabel() }}</span>
            <ng-content select="[table-bulk-actions]"></ng-content>
          }
          @if (exportable()) {
            <button type="button" class="ui-btn-secondary ui-btn-sm" (click)="exportCsv()">
              <span class="material-icons !text-[18px]" aria-hidden="true">download</span>
              {{ exportLabel() }}
            </button>
          }
        </div>
      </div>

      <!-- Desktop table -->
      <div class="hidden overflow-x-auto md:block">
        <table class="w-full border-collapse text-sm">
          @if (caption()) {
            <caption class="sr-only">{{ caption() }}</caption>
          }
          <thead class="sticky top-0 z-10 bg-ink-50">
            <tr class="border-b border-ink-200 text-start">
              @if (selectable()) {
                <th class="w-12 px-4 py-3" scope="col">
                  <input
                    type="checkbox"
                    class="h-4 w-4 accent-brand-600"
                    [checked]="allOnPageSelected()"
                    [indeterminate]="someOnPageSelected()"
                    (change)="toggleAllOnPage($event)"
                    [attr.aria-label]="selectAllLabel()"
                  />
                </th>
              }
              @for (col of columns(); track col.key) {
                <th
                  class="px-4 py-3 font-semibold text-ink-600 whitespace-nowrap"
                  scope="col"
                  [class.text-center]="col.align === 'center'"
                  [class.text-end]="col.align === 'end'"
                  [style.width]="col.width || null"
                >
                  @if (col.sortable) {
                    <button
                      type="button"
                      class="inline-flex items-center gap-1 hover:text-ink-900"
                      (click)="toggleSort(col.key)"
                    >
                      {{ col.header }}
                      <span class="material-icons !text-[16px]" aria-hidden="true" [class.opacity-30]="sortKey() !== col.key">
                        {{ sortKey() === col.key ? (sortDir() === 'asc' ? 'arrow_upward' : 'arrow_downward') : 'unfold_more' }}
                      </span>
                    </button>
                  } @else {
                    {{ col.header }}
                  }
                </th>
              }
            </tr>
          </thead>
          <tbody>
            @if (loading()) {
              @for (r of skeletonRows; track r) {
                <tr class="border-b border-ink-100">
                  @if (selectable()) {
                    <td class="px-4 py-3"><ui-skeleton width="1rem" height="1rem" /></td>
                  }
                  @for (col of columns(); track col.key) {
                    <td class="px-4 py-3"><ui-skeleton height="0.9rem" /></td>
                  }
                </tr>
              }
            } @else {
              @for (row of pagedRows(); track rowKey(row)) {
                <tr class="border-b border-ink-100 transition-colors hover:bg-ink-50/60">
                  @if (selectable()) {
                    <td class="px-4 py-3">
                      <input
                        type="checkbox"
                        class="h-4 w-4 accent-brand-600"
                        [checked]="isSelected(row)"
                        (change)="toggleRow(row)"
                        [attr.aria-label]="selectRowLabel()"
                      />
                    </td>
                  }
                  @for (col of columns(); track col.key) {
                    <td
                      class="px-4 py-3 text-ink-700 align-middle"
                      [class.text-center]="col.align === 'center'"
                      [class.text-end]="col.align === 'end'"
                    >
                      @if (cellTemplate(col.key); as tpl) {
                        <ng-container
                          *ngTemplateOutlet="tpl; context: { $implicit: row, row: row }"
                        ></ng-container>
                      } @else {
                        {{ display(row, col.key) }}
                      }
                    </td>
                  }
                </tr>
              }
            }
          </tbody>
        </table>
      </div>

      <!-- Mobile cards -->
      <div class="divide-y divide-ink-100 md:hidden">
        @if (loading()) {
          @for (r of skeletonRows; track r) {
            <div class="p-4"><ui-skeleton height="3rem" /></div>
          }
        } @else {
          @for (row of pagedRows(); track rowKey(row)) {
            <div class="flex gap-3 p-4">
              @if (selectable()) {
                <input
                  type="checkbox"
                  class="mt-1 h-4 w-4 shrink-0 accent-brand-600"
                  [checked]="isSelected(row)"
                  (change)="toggleRow(row)"
                  [attr.aria-label]="selectRowLabel()"
                />
              }
              <div class="min-w-0 flex-1 space-y-1.5">
                @for (col of mobileColumns(); track col.key) {
                  <div class="flex items-start justify-between gap-3">
                    <span class="text-xs font-medium text-ink-400">{{ col.header }}</span>
                    <span class="min-w-0 text-end text-sm text-ink-800">
                      @if (cellTemplate(col.key); as tpl) {
                        <ng-container
                          *ngTemplateOutlet="tpl; context: { $implicit: row, row: row }"
                        ></ng-container>
                      } @else {
                        {{ display(row, col.key) }}
                      }
                    </span>
                  </div>
                }
              </div>
            </div>
          }
        }
      </div>

      <!-- Empty state -->
      @if (!loading() && pagedRows().length === 0) {
        <ui-empty-state [icon]="emptyIcon()" [title]="emptyTitle()" [message]="emptyMessage()" />
      }

      <!-- Pagination -->
      @if (!loading() && totalFiltered() > pageSize()) {
        <div class="flex flex-col items-center justify-between gap-3 border-t border-ink-200 p-4 sm:flex-row">
          <p class="text-sm text-ink-500">
            {{ rangeStart() }}–{{ rangeEnd() }} {{ ofLabel() }} {{ totalFiltered() }}
          </p>
          <div class="flex items-center gap-1">
            <button
              type="button"
              class="ui-btn-icon min-h-11 min-w-11 disabled:opacity-40"
              [disabled]="page() === 1"
              (click)="setPage(page() - 1)"
              [attr.aria-label]="previousPageLabel()"
            >
              <span class="material-icons !text-[20px] rtl:rotate-180" aria-hidden="true">chevron_left</span>
            </button>
            <span class="px-2 text-sm font-medium text-ink-700 tabular-nums">{{ page() }} / {{ totalPages() }}</span>
            <button
              type="button"
              class="ui-btn-icon min-h-11 min-w-11 disabled:opacity-40"
              [disabled]="page() === totalPages()"
              (click)="setPage(page() + 1)"
              [attr.aria-label]="nextPageLabel()"
            >
              <span class="material-icons !text-[20px] rtl:rotate-180" aria-hidden="true">chevron_right</span>
            </button>
          </div>
        </div>
      }
    </div>
  `,
})
export class UiDataTableComponent<T extends Record<string, unknown> = Record<string, unknown>> {
  readonly columns = input.required<UiTableColumn[]>();
  readonly rows = input.required<readonly T[]>();
  readonly loading = input(false);
  readonly rowId = input<(row: T) => string | number>((row) => row['id'] as string | number);

  readonly searchable = input(true);
  readonly searchPlaceholder = input('Search…');
  readonly searchAriaLabel = input('Search table');
  readonly selectable = input(false);
  readonly exportable = input(false);
  readonly exportFileName = input('export.csv');
  readonly pageSize = input(10);

  // Labels (so the table stays i18n-agnostic and works in EN/AR).
  readonly emptyIcon = input('inbox');
  readonly emptyTitle = input('No records found');
  readonly emptyMessage = input<string | null>(null);
  readonly selectedLabel = input('selected');
  readonly exportLabel = input('Export');
  readonly ofLabel = input('of');
  readonly caption = input<string | null>(null);
  readonly selectAllLabel = input('Select all rows on this page');
  readonly selectRowLabel = input('Select row');
  readonly previousPageLabel = input('Previous page');
  readonly nextPageLabel = input('Next page');

  readonly selectionChange = output<T[]>();

  private readonly cellDirectives = contentChildren(UiCellDirective);

  protected readonly search = signal('');
  protected readonly sortKey = signal<string | null>(null);
  protected readonly sortDir = signal<SortDir>(null);
  protected readonly page = signal(1);
  private readonly selectedIds = signal<Set<string | number>>(new Set());

  protected readonly skeletonRows = Array.from({ length: 6 });

  constructor() {
    // Reset to first page whenever the dataset or filters change.
    effect(() => {
      this.rows();
      this.search();
      this.sortKey();
      this.sortDir();
      this.page.set(1);
    });
  }

  protected cellTemplate(key: string) {
    return this.cellDirectives().find((d) => d.uiCell() === key)?.template ?? null;
  }

  protected readonly mobileColumns = computed(() =>
    this.columns().filter((c) => !c.hideOnMobile)
  );

  protected display(row: T, key: string): string {
    const value = row[key];
    return value === null || value === undefined || value === '' ? '—' : String(value);
  }

  protected rowKey(row: T): string | number {
    return this.rowId()(row);
  }

  private readonly filteredRows = computed(() => {
    const term = this.search().trim().toLowerCase();
    let data = [...this.rows()];
    if (term) {
      const keys = this.columns().map((c) => c.key);
      data = data.filter((row) =>
        keys.some((k) => String(row[k] ?? '').toLowerCase().includes(term))
      );
    }
    const key = this.sortKey();
    const dir = this.sortDir();
    if (key && dir) {
      data.sort((a, b) => {
        const av = a[key];
        const bv = b[key];
        if (av == null) return 1;
        if (bv == null) return -1;
        let cmp: number;
        if (typeof av === 'number' && typeof bv === 'number') {
          cmp = av - bv;
        } else {
          cmp = String(av).localeCompare(String(bv), undefined, { numeric: true });
        }
        return dir === 'asc' ? cmp : -cmp;
      });
    }
    return data;
  });

  protected readonly totalFiltered = computed(() => this.filteredRows().length);
  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalFiltered() / this.pageSize()))
  );

  protected readonly pagedRows = computed(() => {
    const start = (this.page() - 1) * this.pageSize();
    return this.filteredRows().slice(start, start + this.pageSize());
  });

  protected readonly rangeStart = computed(() =>
    this.totalFiltered() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1
  );
  protected readonly rangeEnd = computed(() =>
    Math.min(this.page() * this.pageSize(), this.totalFiltered())
  );

  protected onSearch(value: string): void {
    this.search.set(value);
  }

  protected toggleSort(key: string): void {
    if (this.sortKey() !== key) {
      this.sortKey.set(key);
      this.sortDir.set('asc');
      return;
    }
    const next: SortDir = this.sortDir() === 'asc' ? 'desc' : this.sortDir() === 'desc' ? null : 'asc';
    this.sortDir.set(next);
    if (next === null) this.sortKey.set(null);
  }

  protected setPage(page: number): void {
    this.page.set(Math.min(Math.max(1, page), this.totalPages()));
  }

  // ---- Selection ----------------------------------------------------------
  protected isSelected(row: T): boolean {
    return this.selectedIds().has(this.rowKey(row));
  }

  protected readonly selectedCount = computed(() => this.selectedIds().size);

  protected readonly allOnPageSelected = computed(() => {
    const rows = this.pagedRows();
    return rows.length > 0 && rows.every((r) => this.selectedIds().has(this.rowKey(r)));
  });

  protected readonly someOnPageSelected = computed(() => {
    const rows = this.pagedRows();
    const sel = this.selectedIds();
    const count = rows.filter((r) => sel.has(this.rowKey(r))).length;
    return count > 0 && count < rows.length;
  });

  protected toggleRow(row: T): void {
    const set = new Set(this.selectedIds());
    const id = this.rowKey(row);
    if (set.has(id)) set.delete(id);
    else set.add(id);
    this.selectedIds.set(set);
    this.emitSelection();
  }

  protected toggleAllOnPage(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const set = new Set(this.selectedIds());
    for (const row of this.pagedRows()) {
      const id = this.rowKey(row);
      if (checked) set.add(id);
      else set.delete(id);
    }
    this.selectedIds.set(set);
    this.emitSelection();
  }

  private emitSelection(): void {
    const ids = this.selectedIds();
    this.selectionChange.emit(this.rows().filter((r) => ids.has(this.rowKey(r))));
  }

  // ---- Export -------------------------------------------------------------
  protected exportCsv(): void {
    const cols = this.columns().filter((c) => !c.noExport);
    const header = cols.map((c) => this.csvCell(c.header)).join(',');
    const lines = this.filteredRows().map((row) =>
      cols.map((c) => this.csvCell(this.display(row, c.key))).join(',')
    );
    const csv = [header, ...lines].join('\r\n');
    const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = this.exportFileName();
    a.click();
    URL.revokeObjectURL(url);
  }

  private csvCell(value: string): string {
    const v = value ?? '';
    return /[",\n]/.test(v) ? `"${v.replace(/"/g, '""')}"` : v;
  }
}
