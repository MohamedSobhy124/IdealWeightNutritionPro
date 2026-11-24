import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal, viewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminProductFilter, AdminProductListItem, AdminProductStatistics } from '../../../core/models/admin-product.models';
import { AdminProductService } from '../../../core/services/admin-product.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  UiActionMenuComponent,
  UiAlertComponent,
  UiBadgeComponent,
  UiCellDirective,
  UiDataTableComponent,
  UiPageHeaderComponent,
  type UiMenuItem,
  type UiTableColumn,
  type UiTableTab,
} from '../../../shared/ui';

/** Tab order, left to right. Counts come from `statistics()` where available. */
const PRODUCT_FILTERS: AdminProductFilter[] = [
  'all',
  'active',
  'instock',
  'lowstock',
  'outofstock',
  'expiring',
  'new',
  'trending',
  'deleted',
];

const FILTER_LABELS: Record<AdminProductFilter, AdminUiKey> = {
  active: 'productFilterActive',
  all: 'productFilterAll',
  deleted: 'productFilterDeleted',
  lowstock: 'productFilterLowStock',
  outofstock: 'productFilterOutOfStock',
  instock: 'productFilterInStock',
  new: 'productFilterNew',
  trending: 'productFilterTrending',
  expiring: 'productFilterExpiring',
};

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterLink,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
    UiActionMenuComponent,
    UiAlertComponent,
  ],
  templateUrl: './admin-products.component.html',
  styleUrl: './admin-products.component.css',
})
export class AdminProductsComponent implements OnInit {
  readonly catalogue = inject(CatalogueService);
  private readonly productsApi = inject(AdminProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);

  private readonly table = viewChild(UiDataTableComponent);

  readonly products = signal<AdminProductListItem[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly stats = signal<AdminProductStatistics | null>(null);
  readonly busy = signal(false);
  readonly loading = signal(true);
  readonly exporting = signal(false);
  readonly regeneratingSlugs = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly selected = signal<AdminProductListItem[]>([]);
  readonly filter = signal<AdminProductFilter>('active');

  search = '';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'title', header: this.t('title'), sortable: true, mobile: 'primary', hideable: false },
      { key: 'categoryName', header: this.t('category'), hideOnMobile: true },
      { key: 'productType', header: this.t('type'), hideOnMobile: true, hiddenByDefault: true },
      { key: 'price', header: this.t('price'), sortable: true, align: 'end' },
      { key: 'stockQuantity', header: this.t('stock'), sortable: true, align: 'end' },
      { key: 'status', header: this.t('productStatus') },
      { key: 'actions', header: '', noExport: true, hideable: false, width: '3.5rem', align: 'end', mobile: 'actions' },
    ];
  }

  readonly tabs = computed<UiTableTab[]>(() => {
    const s = this.stats();
    const counts: Partial<Record<AdminProductFilter, number>> = s
      ? { all: s.total, active: s.active, lowstock: s.lowStock, outofstock: s.outOfStock, expiring: s.expiring, deleted: s.deleted }
      : {};
    // Re-read the locale so labels follow AR/EN switches.
    this.locale.isArabic();
    return PRODUCT_FILTERS.map((value) => ({ value, label: this.t(FILTER_LABELS[value]), count: counts[value] ?? null }));
  });

  readonly subtitle = computed(() => {
    const s = this.stats();
    const total = s?.total ?? this.totalCount();
    const base = `${total} ${this.t('productCount')}`;
    return s?.outOfStock ? `${base} · ${s.outOfStock} ${this.t('outOfStock')}` : base;
  });

  readonly headerMenu = computed<UiMenuItem[]>(() => [
    {
      id: 'slugs',
      label: this.regeneratingSlugs() ? this.t('regeneratingSlugs') : this.t('regenerateSlugs'),
      icon: 'autorenew',
      disabled: this.regeneratingSlugs(),
    },
  ]);

  readonly allSelectedDeleted = computed(() => this.selected().length > 0 && this.selected().every((p) => p.isDeleted));

  rowMenu(p: AdminProductListItem): UiMenuItem[] {
    return [
      { id: 'edit', label: this.t('edit'), icon: 'edit', link: ['/admin/products', p.id] },
      { id: 'store', label: this.t('viewStorefront'), icon: 'open_in_new', hidden: p.isDeleted },
      p.isDeleted
        ? { id: 'restore', label: this.t('restoreProduct'), icon: 'restore_from_trash', dividerBefore: true }
        : { id: 'delete', label: this.t('deleteProduct'), icon: 'delete_outline', danger: true, dividerBefore: true },
    ];
  }

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const filterParam = params.get('filter') as AdminProductFilter | null;
    if (filterParam && PRODUCT_FILTERS.includes(filterParam)) this.filter.set(filterParam);
    this.search = params.get('q') ?? '';
    this.loadStats();
    this.load();
  }

  loadStats(): void {
    this.productsApi.statistics().subscribe({ next: (s) => this.stats.set(s), error: () => this.stats.set(null) });
  }

  onTab(value: string): void {
    this.filter.set(value as AdminProductFilter);
    this.syncUrl();
    this.reload();
  }

  onSearch(term: string): void {
    if (term === this.search) return;
    this.search = term;
    this.syncUrl();
    this.reload();
  }

  /** Resets to the first page (search/filter changed). */
  reload(): void {
    this.page.set(1);
    this.load();
  }

  onPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  onPageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }

  onHeaderAction(id: string): void {
    if (id === 'slugs') void this.regenerateSlugs();
  }

  onRowAction(id: string, p: AdminProductListItem): void {
    if (id === 'store') window.open(this.router.serializeUrl(this.router.createUrlTree(['/product', p.slug])), '_blank', 'noopener');
    if (id === 'delete' || id === 'restore') void this.toggleDeleted(p);
  }

  async toggleDeleted(p: AdminProductListItem): Promise<void> {
    const restoring = p.isDeleted;
    const confirmed = await this.confirmDialog.ask({
      title: this.t(restoring ? 'confirmRestoreProduct' : 'confirmDeleteProduct'),
      message: restoring ? p.title : `${p.title}. ${this.t('productDeleteHint')}`,
      confirmLabel: this.t(restoring ? 'restoreProduct' : 'deleteProduct'),
      danger: !restoring,
      icon: restoring ? 'restore_from_trash' : 'delete_outline',
    });
    if (!confirmed) return;
    this.busy.set(true);
    const op = restoring ? this.productsApi.restore(p.id) : this.productsApi.softDelete(p.id);
    op.subscribe({
      next: () => {
        this.toast.success(this.t(restoring ? 'productRestoredMsg' : 'productDeletedMsg'));
        this.busy.set(false);
        this.refresh();
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.busy.set(false);
      },
    });
  }

  async bulkToggle(restore: boolean): Promise<void> {
    const targets = this.selected().filter((p) => p.isDeleted === restore);
    if (!targets.length) return;
    const confirmed = await this.confirmDialog.ask({
      title: this.t(restore ? 'confirmBulkRestoreProducts' : 'confirmBulkDeleteProducts'),
      message: `${targets.length} ${this.t('bulkProductsMessage')}`,
      confirmLabel: this.t(restore ? 'restoreSelected' : 'deleteSelected'),
      danger: !restore,
      icon: restore ? 'restore_from_trash' : 'delete_outline',
    });
    if (!confirmed) return;
    this.busy.set(true);
    forkJoin(
      targets.map((p) =>
        (restore ? this.productsApi.restore(p.id) : this.productsApi.softDelete(p.id)).pipe(
          map(() => true),
          catchError(() => of(false)),
        ),
      ),
    ).subscribe((results) => {
      const ok = results.filter(Boolean).length;
      if (ok) this.toast.success(`${ok} ${this.t('bulkUpdatedMsg')}`);
      if (ok < results.length) this.toast.error(this.t('bulkPartialFailed'));
      this.busy.set(false);
      this.table()?.clearSelection();
      this.refresh();
    });
  }

  private refresh(): void {
    this.loadStats();
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.productsApi.list(this.page(), this.pageSize(), this.search, this.filter()).subscribe({
      next: (res) => {
        this.products.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(this.t('loadProductsError'));
        this.loading.set(false);
      },
    });
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.productsApi.exportCsv(this.filter(), this.search).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `products-export-${new Date().toISOString().slice(0, 10)}.csv`;
        anchor.click();
        URL.revokeObjectURL(url);
        this.exporting.set(false);
      },
      error: () => {
        this.toast.error(this.t('exportFailed'));
        this.exporting.set(false);
      },
    });
  }

  async regenerateSlugs(): Promise<void> {
    const confirmed = await this.confirmDialog.ask({
      title: this.t('confirmTitle'),
      message: this.t('confirmRegenerateSlugs'),
      confirmLabel: this.t('regenerateSlugs'),
      icon: 'autorenew',
    });
    if (!confirmed) return;

    this.regeneratingSlugs.set(true);
    this.productsApi.regenerateSlugs().subscribe({
      next: (res) => {
        if (res.success) {
          this.toast.success(res.message);
          this.refresh();
        } else {
          this.toast.error(res.message);
        }
        this.regeneratingSlugs.set(false);
      },
      error: () => {
        this.toast.error(this.t('actionFailed'));
        this.regeneratingSlugs.set(false);
      },
    });
  }

  private syncUrl(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { filter: this.filter() === 'active' ? null : this.filter(), q: this.search || null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
