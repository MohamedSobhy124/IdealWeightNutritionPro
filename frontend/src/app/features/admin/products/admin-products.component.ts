import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { AdminProductFilter, AdminProductListItem } from '../../../core/models/admin-product.models';
import { AdminProductService } from '../../../core/services/admin-product.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiPageHeaderComponent,
  UiDataTableComponent,
  UiCellDirective,
  UiBadgeComponent,
  type UiTableColumn,
} from '../../../shared/ui';

const PRODUCT_FILTERS: AdminProductFilter[] = [
  'active',
  'all',
  'deleted',
  'lowstock',
  'outofstock',
  'instock',
  'new',
  'trending',
];

@Component({
  standalone: true,
  imports: [
    RouterLink,
    IwnCurrencyPipe,
    FormsModule,
    UiPageHeaderComponent,
    UiDataTableComponent,
    UiCellDirective,
    UiBadgeComponent,
  ],
  templateUrl: './admin-products.component.html',
  styleUrl: './admin-products.component.css',
})
export class AdminProductsComponent implements OnInit {
  readonly catalogue = inject(CatalogueService);
  private readonly productsApi = inject(AdminProductService);
  private readonly route = inject(ActivatedRoute);
  readonly locale = inject(LocaleService);

  readonly products = signal<AdminProductListItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly exporting = signal(false);
  readonly regeneratingSlugs = signal(false);
  readonly message = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly filters = PRODUCT_FILTERS;

  search = '';
  filter: AdminProductFilter = 'active';

  t(key: AdminUiKey): string {
    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  }

  get cols(): UiTableColumn[] {
    return [
      { key: 'title', header: this.t('title'), sortable: true },
      { key: 'categoryName', header: this.t('category'), hideOnMobile: true },
      { key: 'productType', header: this.t('type'), hideOnMobile: true },
      { key: 'price', header: this.t('price'), sortable: true, align: 'end' },
      { key: 'stockQuantity', header: this.t('stock'), sortable: true, align: 'end' },
      { key: 'status', header: this.t('productStatus') },
    ];
  }

  filterLabel(value: AdminProductFilter): string {
    const keyMap: Record<AdminProductFilter, AdminUiKey> = {
      active: 'productFilterActive',
      all: 'productFilterAll',
      deleted: 'productFilterDeleted',
      lowstock: 'productFilterLowStock',
      outofstock: 'productFilterOutOfStock',
      instock: 'productFilterInStock',
      new: 'productFilterNew',
      trending: 'productFilterTrending',
    };
    return this.t(keyMap[value]);
  }

  ngOnInit(): void {
    const filterParam = this.route.snapshot.queryParamMap.get('filter');
    if (filterParam && this.filters.includes(filterParam as AdminProductFilter)) {
      this.filter = filterParam as AdminProductFilter;
    }
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.productsApi.list(1, 100, this.search, this.filter).subscribe({
      next: (res) => {
        this.products.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(this.t('loadProductsError'));
        this.loading.set(false);
      },
    });
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.error.set(null);
    this.message.set(null);
    this.productsApi.exportCsv(this.filter, this.search).subscribe({
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
        this.error.set(this.t('exportFailed'));
        this.exporting.set(false);
      },
    });
  }

  regenerateSlugs(): void {
    if (!confirm(this.t('confirmRegenerateSlugs'))) return;

    this.regeneratingSlugs.set(true);
    this.error.set(null);
    this.message.set(null);
    this.productsApi.regenerateSlugs().subscribe({
      next: (res) => {
        if (res.success) {
          this.message.set(res.message);
          this.load();
        } else {
          this.error.set(res.message);
        }
        this.regeneratingSlugs.set(false);
      },
      error: () => {
        this.error.set(this.t('actionFailed'));
        this.regeneratingSlugs.set(false);
      },
    });
  }
}
