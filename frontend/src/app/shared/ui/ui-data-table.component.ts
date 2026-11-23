import { isPlatformBrowser, NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  effect,
  ElementRef,
  HostListener,
  inject,
  input,
  linkedSignal,
  output,
  PLATFORM_ID,
  signal,
  untracked,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LocaleService } from '../../core/services/locale.service';
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
  /** Role on the mobile card: card title, subtitle under the title, or footer actions. */
  mobile?: 'primary' | 'secondary' | 'actions';
  /** Hidden until the user enables it from the column menu. */
  hiddenByDefault?: boolean;
  /** Set to `false` to keep the column out of the column menu (always visible). */
  hideable?: boolean;
  /** Exclude from CSV export. */
  noExport?: boolean;
  /** Value used for sort and export when it is not `row[key]`. */
  value?: (row: Record<string, unknown>) => unknown;
  /** Extra text matched by the toolbar search. Falls back to `value`, then `row[key]`. */
  searchText?: (row: Record<string, unknown>) => unknown;
}

type SortDir = 'asc' | 'desc' | null;

const INTERACTIVE = 'a, button, input, select, textarea, label, [role="button"]';

const LABELS = {
  searchPlaceholder: ['Search…', 'بحث…'],
  searchAriaLabel: ['Search table', 'بحث في الجدول'],
  selectedLabel: ['selected', 'محدد'],
  exportLabel: ['Export', 'تصدير'],
  ofLabel: ['of', 'من'],
  selectAllLabel: ['Select all rows on this page', 'تحديد كل صفوف الصفحة'],
  selectRowLabel: ['Select row', 'تحديد الصف'],
  previousPageLabel: ['Previous page', 'الصفحة السابقة'],
  nextPageLabel: ['Next page', 'الصفحة التالية'],
  emptyTitle: ['No records found', 'لا توجد سجلات'],
  columnsLabel: ['Columns', 'الأعمدة'],
  resetColumnsLabel: ['Show all columns', 'إظهار كل الأعمدة'],
  rowsPerPageLabel: ['Rows per page', 'صفوف لكل صفحة'],
  clearSelectionLabel: ['Clear', 'إلغاء التحديد'],
} as const satisfies Record<string, readonly [string, string]>;

type LabelKey = keyof typeof LABELS;

@Component({
  selector: 'ui-data-table',
  standalone: true,
  imports: [FormsModule, NgTemplateOutlet, UiSkeletonComponent, UiEmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ui-card overflow-hidden">
      <!-- Toolbar -->
      <div class="flex flex-col gap-3 border-b border-ink-200 p-3 sm:p-4 lg:flex-row lg:items-center lg:justify-between">
        <div class="flex min-w-0 flex-1 flex-wrap items-center gap-2">
          @if (searchable()) {
            <div class="relative w-full sm:max-w-xs">
              <span
                class="material-icons pointer-events-none absolute start-3 top-1/2 -translate-y-1/2 !text-[20px] text-ink-400"
                aria-hidden="true"
                >search</span
              >
              <input
                type="search"
                class="ui-input ps-10"
                [placeholder]="label('searchPlaceholder')"
                [attr.aria-label]="label('searchAriaLabel')"
                [ngModel]="search()"
                (ngModelChange)="search.set($event)"
              />
            </div>
          }
          <ng-content select="[table-toolbar]"></ng-content>
        </div>
        <div class="flex flex-wrap items-center gap-2">
          @if (selectedCount() > 0) {
            <span class="text-sm font-medium text-ink-600" role="status">{{ selectedCount() }} {{ label('selectedLabel') }}</span>
            <ng-content select="[table-bulk-actions]"></ng-content>
            <button type="button" class="ui-btn-ghost ui-btn-sm" (click)="clearSelection()">{{ label('clearSelectionLabel') }}</button>
          }
          @if (columnMenu() && hideableColumns().length > 1) {
            <div class="relative hidden md:block">
              <button
                type="button"
                class="ui-btn-secondary ui-btn-sm"
                [attr.aria-expanded]="columnMenuOpen()"
                aria-haspopup="true"
                (click)="columnMenuOpen.set(!columnMenuOpen())"
              >
                <span class="material-icons !text-[18px]" aria-hidden="true">view_column</span>
                {{ label('columnsLabel') }}
              </button>
              @if (columnMenuOpen()) {
                <div
                  class="absolute end-0 top-full z-20 mt-2 w-60 overflow-hidden rounded-xl border border-ink-200 bg-surface py-1.5 shadow-elevated animate-slide-up"
                  role="group"
                  [attr.aria-label]="label('columnsLabel')"
                >
                  @for (col of hideableColumns(); track col.key) {
                    <label class="flex min-h-10 cursor-pointer items-center gap-3 px-3 text-sm text-ink-700 hover:bg-ink-50">
                      <input
                        type="checkbox"
                        class="h-4 w-4 accent-brand-600"
                        [checked]="!hiddenKeys().has(col.key)"
                        [disabled]="!hiddenKeys().has(col.key) && columns().length - hiddenKeys().size <= 1"
                        (change)="toggleColumn(col.key)"
                      />
                      <span class="truncate">{{ col.header || col.key }}</span>
                    </label>
                  }
                  @if (hiddenKeys().size > 0) {
                    <div class="mt-1 border-t border-ink-100 px-3 pt-1.5">
                      <button type="button" class="ui-btn-ghost ui-btn-sm w-full" (click)="resetColumns()">{{ label('resetColumnsLabel') }}</button>
                    </div>
                  }
                </div>
              }
            </div>
          }
          @if (exportable()) {
            <button type="button" class="ui-btn-secondary ui-btn-sm" (click)="exportCsv()">
              <span class="material-icons !text-[18px]" aria-hidden="true">download</span>
              {{ label('exportLabel') }}
            </button>
          }
        </div>
      </div>
      <ng-content select="[table-filters]"></ng-content>

      <!-- Desktop table -->
      <div class="hidden overflow-x-auto md:block" [class.ui-table-scroll]="stickyHeader()">
        <table class="w-full border-collapse text-sm">
          @if (caption()) {
            <caption class="sr-only">{{ caption() }}</caption>
          }
          <thead class="sticky top-0 z-10 bg-ink-50">
            <tr class="border-b border-ink-200 text-start">
              @if (selectable()) {
                <th class="ui-table-pad w-12 px-4 py-3" scope="col">
                  <input
                    type="checkbox"
                    class="h-4 w-4 accent-brand-600"
                    [checked]="allOnPageSelected()"
                    [indeterminate]="someOnPageSelected()"
                    (change)="toggleAllOnPage($event)"
                    [attr.aria-label]="label('selectAllLabel')"
                  />
                </th>
              }
              @for (col of visibleColumns(); track col.key) {
                <th
                  class="ui-table-pad whitespace-nowrap px-4 py-3 text-start text-xs font-semibold uppercase tracking-wide text-ink-500"
                  scope="col"
                  [class.text-center]="col.align === 'center'"
                  [class.text-end]="col.align === 'end'"
                  [style.width]="col.width || null"
                  [attr.aria-sort]="ariaSort(col.key)"
                >
                  @if (col.sortable) {
                    <button
                      type="button"
                      class="inline-flex items-center gap-1 rounded uppercase hover:text-ink-900"
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
                    <td class="ui-table-pad px-4 py-3"><ui-skeleton width="1rem" height="1rem" /></td>
                  }
                  @for (col of visibleColumns(); track col.key) {
                    <td class="ui-table-pad px-4 py-3"><ui-skeleton height="0.9rem" /></td>
                  }
                </tr>
              }
            } @else {
              @for (row of pagedRows(); track rowKey(row)) {
                <tr
                  class="border-b border-ink-100 transition-colors hover:bg-ink-50/70"
                  [class.cursor-pointer]="rowsClickable()"
                  [class.bg-brand-50]="isSelected(row)"
                  (click)="onRowClick(row, $event)"
                >
                  @if (selectable()) {
                    <td class="ui-table-pad px-4 py-3">
                      <input
                        type="checkbox"
                        class="h-4 w-4 accent-brand-600"
                        [checked]="isSelected(row)"
                        (change)="toggleRow(row)"
                        [attr.aria-label]="label('selectRowLabel')"
                      />
                    </td>
                  }
                  @for (col of visibleColumns(); track col.key) {
                    <td
                      class="ui-table-pad px-4 py-3 align-middle text-ink-700"
                      [class.text-center]="col.align === 'center'"
                      [class.text-end]="col.align === 'end'"
                    >
                      <ng-container *ngTemplateOutlet="cell; context: { row: row, col: col }"></ng-container>
                    </td>
                  }
                </tr>
              }
            }
          </tbody>
        </table>
      </div>

      <!-- Mobile cards -->
      <ul class="m-0 list-none divide-y divide-ink-100 p-0 md:hidden">
        @if (loading()) {
          @for (r of skeletonRows; track r) {
            <li class="space-y-2 p-4">
              <ui-skeleton width="60%" height="1rem" />
              <ui-skeleton width="40%" height="0.8rem" />
            </li>
          }
        } @else {
          @for (row of pagedRows(); track rowKey(row)) {
            <li
              class="flex gap-3 p-4"
              [class.cursor-pointer]="rowsClickable()"
              [class.bg-brand-50]="isSelected(row)"
              (click)="onRowClick(row, $event)"
            >
              @if (selectable()) {
                <input
                  type="checkbox"
                  class="mt-1 h-5 w-5 shrink-0 accent-brand-600"
                  [checked]="isSelected(row)"
                  (change)="toggleRow(row)"
                  [attr.aria-label]="label('selectRowLabel')"
                />
              }
              <div class="min-w-0 flex-1">
                @if (mobileLayout().primary; as col) {
                  <div class="text-[15px] font-semibold leading-snug text-ink-900">
                    <ng-container *ngTemplateOutlet="cell; context: { row: row, col: col }"></ng-container>
                  </div>
                }
                @if (mobileLayout().secondary; as col) {
                  <div class="mt-0.5 text-sm text-ink-500">
                    <ng-container *ngTemplateOutlet="cell; context: { row: row, col: col }"></ng-container>
                  </div>
                }
                @if (mobileLayout().details.length) {
                  <dl class="m-0 mt-3 grid grid-cols-2 gap-x-4 gap-y-2.5">
                    @for (col of mobileLayout().details; track col.key) {
                      <div class="min-w-0">
                        <dt class="text-[11px] font-medium uppercase tracking-wide text-ink-400">{{ col.header }}</dt>
                        <dd class="m-0 mt-0.5 break-words text-sm text-ink-800">
                          <ng-container *ngTemplateOutlet="cell; context: { row: row, col: col }"></ng-container>
                        </dd>
                      </div>
                    }
                  </dl>
                }
                @if (mobileLayout().actions.length) {
                  <div class="mt-3 flex flex-wrap items-center justify-end gap-2 border-t border-ink-100 pt-3">
                    @for (col of mobileLayout().actions; track col.key) {
                      <ng-container *ngTemplateOutlet="cell; context: { row: row, col: col }"></ng-container>
                    }
                  </div>
                }
              </div>
            </li>
          }
        }
      </ul>

      <!-- Empty state -->
      @if (!loading() && pagedRows().length === 0) {
        <ui-empty-state [icon]="emptyIcon()" [title]="label('emptyTitle')" [message]="emptyMessage()" />
      }

      <!-- Pagination -->
      @if (!loading() && showPager()) {
        <div class="flex flex-col items-center justify-between gap-3 border-t border-ink-200 px-4 py-3 sm:flex-row">
          <div class="flex items-center gap-3 text-sm text-ink-500">
            <span class="tabular-nums">{{ rangeStart() }}–{{ rangeEnd() }} {{ label('ofLabel') }} {{ total() }}</span>
            @if (pageSizeSelector()) {
              <label class="hidden items-center gap-2 sm:flex">
                <span>{{ label('rowsPerPageLabel') }}</span>
                <select
                  class="ui-select !min-h-0 w-auto !py-1.5 !ps-2.5 text-sm"
                  [ngModel]="size()"
                  (ngModelChange)="setPageSize(+$event)"
                >
                  @for (n of sizeOptions(); track n) {
                    <option [value]="n">{{ n }}</option>
                  }
                </select>
              </label>
            }
          </div>
          <div class="flex items-center gap-1">
            <button
              type="button"
              class="ui-btn-icon min-h-11 min-w-11 disabled:opacity-40"
              [disabled]="page() === 1"
              (click)="setPage(page() - 1)"
              [attr.aria-label]="label('previousPageLabel')"
            >
              <span class="material-icons !text-[20px] rtl:rotate-180" aria-hidden="true">chevron_left</span>
            </button>
            <span class="px-2 text-sm font-medium tabular-nums text-ink-700" aria-live="polite">{{ page() }} / {{ totalPages() }}</span>
            <button
              type="button"
              class="ui-btn-icon min-h-11 min-w-11 disabled:opacity-40"
              [disabled]="page() === totalPages()"
              (click)="setPage(page() + 1)"
              [attr.aria-label]="label('nextPageLabel')"
            >
              <span class="material-icons !text-[20px] rtl:rotate-180" aria-hidden="true">chevron_right</span>
            </button>
          </div>
        </div>
      }
    </div>

    <ng-template #cell let-row="row" let-col="col">
      @if (cellTemplate(col.key); as tpl) {
        <ng-container *ngTemplateOutlet="tpl; context: { $implicit: row, row: row }"></ng-container>
      } @else {
        {{ display(row, col.key) }}
      }
    </ng-template>
  `,
})
export class UiDataTableComponent<T extends Record<string, unknown> = Record<string, unknown>> {
  private readonly locale = inject(LocaleService);
  private readonly host = inject(ElementRef<HTMLElement>);
  private readonly router = inject(Router);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly columns = input.required<UiTableColumn[]>();
  readonly rows = input.required<readonly T[]>();
  readonly loading = input(false);
  readonly rowId = input<(row: T) => string | number>((row) => row['id'] as string | number);

  readonly searchable = input(true);
  readonly selectable = input(false);
  readonly exportable = input(false);
  readonly exportFileName = input('export.csv');
  readonly pageSize = input(10);
  readonly pageSizeOptions = input<number[]>([10, 20, 50, 100]);
  readonly pageSizeSelector = input(true);
  readonly columnMenu = input(true);
  /** Persists hidden columns in localStorage under this key. */
  readonly storageKey = input<string | null>(null);
  readonly stickyHeader = input(true);
  readonly clickableRows = input(false);
  /** Makes rows open `${rowLinkBase}/${rowId}`; Ctrl/⌘-click opens a new tab. */
  readonly rowLinkBase = input<string | null>(null);
  protected readonly rowsClickable = computed(() => this.clickableRows() || !!this.rowLinkBase());

  /** Server paging: total record count on the server. Rows are then treated as the current page. */
  readonly serverTotal = input<number | null>(null);
  /** Server paging: current 1-based page. */
  readonly serverPage = input(1);

  // Labels. Defaults follow the active locale; pass a value to override.
  readonly emptyIcon = input('inbox');
  readonly emptyTitle = input<string | null>(null);
  readonly emptyMessage = input<string | null>(null);
  readonly caption = input<string | null>(null);
  readonly searchPlaceholder = input<string | null>(null);
  readonly searchAriaLabel = input<string | null>(null);
  readonly selectedLabel = input<string | null>(null);
  readonly exportLabel = input<string | null>(null);
  readonly ofLabel = input<string | null>(null);
  readonly selectAllLabel = input<string | null>(null);
  readonly selectRowLabel = input<string | null>(null);
  readonly previousPageLabel = input<string | null>(null);
  readonly nextPageLabel = input<string | null>(null);

  readonly selectionChange = output<T[]>();
  readonly rowClick = output<T>();
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  private readonly cellDirectives = contentChildren(UiCellDirective);

  protected readonly search = signal('');
  protected readonly sortKey = signal<string | null>(null);
  protected readonly sortDir = signal<SortDir>(null);
  protected readonly clientPage = signal(1);
  protected readonly size = linkedSignal(() => this.pageSize());
  protected readonly columnMenuOpen = signal(false);
  /** Changes only when the set of column keys (or storage key) changes, not on every new array. */
  private readonly columnIdentity = computed(
    () => `${this.storageKey() ?? ''}::${this.columns().map((c) => c.key).join('|')}`
  );
  protected readonly hiddenKeys = linkedSignal({
    source: this.columnIdentity,
    computation: () => untracked(() => this.initialHiddenKeys()),
  });
  private readonly selectedIds = signal<Set<string | number>>(new Set());

  protected readonly skeletonRows = Array.from({ length: 6 });

  private readonly labelOverrides: Partial<Record<LabelKey, () => string | null>> = {
    searchPlaceholder: this.searchPlaceholder,
    searchAriaLabel: this.searchAriaLabel,
    selectedLabel: this.selectedLabel,
    exportLabel: this.exportLabel,
    ofLabel: this.ofLabel,
    selectAllLabel: this.selectAllLabel,
    selectRowLabel: this.selectRowLabel,
    previousPageLabel: this.previousPageLabel,
    nextPageLabel: this.nextPageLabel,
    emptyTitle: this.emptyTitle,
  };

  constructor() {
    // Reset to the first page whenever the dataset or client-side filters change.
    effect(() => {
      this.rows();
      this.search();
      this.sortKey();
      this.sortDir();
      untracked(() => this.clientPage.set(1));
    });
  }

  protected label(key: LabelKey): string {
    const override = this.labelOverrides[key]?.();
    if (override) return override;
    const [en, ar] = LABELS[key];
    return this.locale.pick(en, ar);
  }

  protected cellTemplate(key: string) {
    return this.cellDirectives().find((d) => d.uiCell() === key)?.template ?? null;
  }

  // ---- Columns ------------------------------------------------------------
  protected readonly visibleColumns = computed(() =>
    this.columns().filter((c) => !this.hiddenKeys().has(c.key))
  );

  protected readonly hideableColumns = computed(() =>
    this.columns().filter((c) => c.hideable !== false && c.mobile !== 'primary')
  );

  protected readonly mobileLayout = computed(() => {
    const cols = this.columns().filter((c) => !c.hideOnMobile);
    const actions = cols.filter((c) => c.mobile === 'actions' || c.key === 'actions');
    const rest = cols.filter((c) => !actions.includes(c));
    const primary = rest.find((c) => c.mobile === 'primary') ?? rest[0] ?? null;
    const secondary = rest.find((c) => c.mobile === 'secondary') ?? null;
    const details = rest.filter((c) => c !== primary && c !== secondary);
    return { primary, secondary, details, actions };
  });

  protected toggleColumn(key: string): void {
    const next = new Set(this.hiddenKeys());
    if (next.has(key)) next.delete(key);
    else next.add(key);
    this.hiddenKeys.set(next);
    this.persistHidden(next);
  }

  protected resetColumns(): void {
    this.hiddenKeys.set(new Set());
    this.persistHidden(new Set());
  }

  private initialHiddenKeys(): Set<string> {
    const key = this.storageKey();
    if (key && this.isBrowser) {
      try {
        const stored = localStorage.getItem(`iwn-table-cols:${key}`);
        if (stored) return new Set(JSON.parse(stored) as string[]);
      } catch {
        /* ignore corrupt storage */
      }
    }
    return new Set(this.columns().filter((c) => c.hiddenByDefault).map((c) => c.key));
  }

  private persistHidden(keys: Set<string>): void {
    const key = this.storageKey();
    if (!key || !this.isBrowser) return;
    localStorage.setItem(`iwn-table-cols:${key}`, JSON.stringify([...keys]));
  }

  // ---- Rows ---------------------------------------------------------------
  protected display(row: T, key: string): string {
    const value = this.cellValue(row, key);
    return value === null || value === undefined || value === '' ? '—' : String(value);
  }

  private cellValue(row: T, key: string): unknown {
    const column = this.columns().find((col) => col.key === key);
    return column?.value ? column.value(row as Record<string, unknown>) : row[key];
  }

  protected rowKey(row: T): string | number {
    return this.rowId()(row);
  }

  protected onRowClick(row: T, event: MouseEvent): void {
    if (!this.rowsClickable()) return;
    const target = event.target as HTMLElement | null;
    if (target?.closest(INTERACTIVE)) return;
    this.rowClick.emit(row);
    const base = this.rowLinkBase();
    if (!base) return;
    const commands = [base, this.rowKey(row)];
    if (event.ctrlKey || event.metaKey) {
      window.open(this.router.serializeUrl(this.router.createUrlTree(commands)), '_blank', 'noopener');
    } else {
      void this.router.navigate(commands);
    }
  }

  private readonly isServer = computed(() => this.serverTotal() !== null);

  private readonly filteredRows = computed(() => {
    const term = this.search().trim().toLowerCase();
    let data = [...this.rows()];
    if (term) {
      const columns = this.columns().filter((col) => !col.noExport);
      data = data.filter((row) =>
        columns.some((col) => {
          const haystack = col.searchText
            ? col.searchText(row as Record<string, unknown>)
            : this.cellValue(row, col.key);
          return String(haystack ?? '').toLowerCase().includes(term);
        }),
      );
    }
    const key = this.sortKey();
    const dir = this.sortDir();
    if (key && dir) {
      data.sort((a, b) => {
        const av = this.cellValue(a, key);
        const bv = this.cellValue(b, key);
        if (av == null) return 1;
        if (bv == null) return -1;
        const cmp =
          typeof av === 'number' && typeof bv === 'number'
            ? av - bv
            : String(av).localeCompare(String(bv), undefined, { numeric: true });
        return dir === 'asc' ? cmp : -cmp;
      });
    }
    return data;
  });

  protected readonly page = computed(() => (this.isServer() ? this.serverPage() : this.clientPage()));
  protected readonly total = computed(() => this.serverTotal() ?? this.filteredRows().length);
  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.size())));
  protected readonly sizeOptions = computed(() =>
    [...new Set([...this.pageSizeOptions(), this.size()])].sort((a, b) => a - b)
  );
  protected readonly showPager = computed(
    () => this.total() > Math.min(this.size(), ...this.pageSizeOptions()) || this.page() > 1
  );

  protected readonly pagedRows = computed(() => {
    if (this.isServer()) return this.filteredRows();
    const start = (this.page() - 1) * this.size();
    return this.filteredRows().slice(start, start + this.size());
  });

  protected readonly rangeStart = computed(() => (this.total() === 0 ? 0 : (this.page() - 1) * this.size() + 1));
  protected readonly rangeEnd = computed(() => Math.min(this.page() * this.size(), this.total()));

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

  protected ariaSort(key: string): 'ascending' | 'descending' | null {
    if (this.sortKey() !== key || !this.sortDir()) return null;
    return this.sortDir() === 'asc' ? 'ascending' : 'descending';
  }

  protected setPage(page: number): void {
    const next = Math.min(Math.max(1, page), this.totalPages());
    if (this.isServer()) {
      this.pageChange.emit(next);
    } else {
      this.clientPage.set(next);
    }
  }

  protected setPageSize(size: number): void {
    this.size.set(size);
    this.clientPage.set(1);
    this.pageSizeChange.emit(size);
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

  clearSelection(): void {
    this.selectedIds.set(new Set());
    this.emitSelection();
  }

  private emitSelection(): void {
    const ids = this.selectedIds();
    this.selectionChange.emit(this.rows().filter((r) => ids.has(this.rowKey(r))));
  }

  @HostListener('document:click', ['$event'])
  protected onDocumentClick(event: MouseEvent): void {
    if (this.columnMenuOpen() && !this.host.nativeElement.contains(event.target as Node)) {
      this.columnMenuOpen.set(false);
    }
  }

  @HostListener('keydown.escape')
  protected onEscape(): void {
    this.columnMenuOpen.set(false);
  }

  // ---- Export -------------------------------------------------------------
  protected exportCsv(): void {
    const cols = this.visibleColumns().filter((c) => !c.noExport && c.key !== 'actions');
    const header = cols.map((c) => this.csvCell(c.header)).join(',');
    const lines = this.filteredRows().map((row) => cols.map((c) => this.csvCell(this.display(row, c.key))).join(','));
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
