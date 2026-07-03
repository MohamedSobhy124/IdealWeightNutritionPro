import { Component, inject, OnInit, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';

import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
import {
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
} from '../../../shared/ui';

import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';

import {

  AdminFlashSaleItem,

  UpsertAdminFlashSaleRequest,

} from '../../../core/models/admin-flash-sale.models';

import { AdminProductListItem } from '../../../core/models/admin-product.models';

import { AdminFlashSaleService } from '../../../core/services/admin-flash-sale.service';
import { AdminMediaService } from '../../../core/services/admin-service.service';
import { AdminProductService } from '../../../core/services/admin-product.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    IwnCurrencyPipe,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiEmptyStateComponent,
    UiSkeletonComponent,
  ],
  templateUrl: './admin-flash-sale-form.component.html',
  styleUrl: './admin-flash-sale-form.component.css',
})
export class AdminFlashSaleFormComponent implements OnInit {

  private readonly flashApi = inject(AdminFlashSaleService);
  private readonly mediaApi = inject(AdminMediaService);
  private readonly productsApi = inject(AdminProductService);
  readonly catalogue = inject(CatalogueService);

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  readonly locale = inject(LocaleService);



  readonly loading = signal(true);

  readonly submitting = signal(false);

  readonly error = signal<string | null>(null);

  readonly saveError = signal<string | null>(null);

  readonly itemMessage = signal<string | null>(null);

  readonly itemError = signal(false);

  readonly isEdit = signal(false);

  readonly items = signal<AdminFlashSaleItem[]>([]);

  readonly productResults = signal<AdminProductListItem[]>([]);

  readonly busyItemId = signal<number | null>(null);
  readonly uploadingImage = signal(false);



  flashSaleId: number | null = null;

  productSearch = '';

  itemPrice = 0;

  itemQty = 1;



  name = '';

  nameAr = '';

  description = '';

  descriptionAr = '';

  imageUrl = '';

  startDate = '';

  endDate = '';

  isActive = true;
  notifySubscribers = false;



  t(key: AdminUiKey): string {

    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);

  }



  ngOnInit(): void {

    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam && idParam !== 'new') {

      const id = Number(idParam);

      if (!id) {

        this.error.set(this.t('invalidFlashSale'));

        this.loading.set(false);

        return;

      }

      this.isEdit.set(true);

      this.flashSaleId = id;

      this.flashApi.get(id).subscribe({

        next: (sale) => {

          this.applySale(sale);

          this.items.set(sale.items);

          this.loading.set(false);

        },

        error: () => {

          this.error.set(this.t('flashSaleNotFound'));

          this.loading.set(false);

        },

      });

    } else {

      const today = new Date();

      const tomorrow = new Date(today);

      tomorrow.setDate(tomorrow.getDate() + 1);

      this.startDate = this.toDateInput(today.toISOString());

      this.endDate = this.toDateInput(tomorrow.toISOString());

      this.loading.set(false);

    }

  }



  private applySale(sale: {

    name: string;

    nameAr?: string;

    description?: string;

    descriptionAr?: string;

    imageUrl: string;

    startDate: string;

    endDate: string;

    isActive: boolean;

  }): void {

    this.name = sale.name;

    this.nameAr = sale.nameAr ?? '';

    this.description = sale.description ?? '';

    this.descriptionAr = sale.descriptionAr ?? '';

    this.imageUrl = sale.imageUrl;

    this.startDate = this.toDateInput(sale.startDate);

    this.endDate = this.toDateInput(sale.endDate);

    this.isActive = sale.isActive;

  }



  searchProducts(): void {

    const term = this.productSearch.trim();

    if (!term) {

      this.productResults.set([]);

      return;

    }

    const existing = new Set(this.items().map((i) => i.productId));

    this.productsApi.list(1, 20, term).subscribe({

      next: (res) => this.productResults.set(res.items.filter((p) => !existing.has(p.id))),

    });

  }



  addItem(productId: number): void {

    if (!this.flashSaleId || this.itemPrice <= 0 || this.itemQty < 1) {

      this.itemMessage.set(this.t('flashSaleItemRequired'));

      this.itemError.set(true);

      return;

    }

    this.flashApi

      .addItem(this.flashSaleId, {

        productId,

        flashSalePrice: this.itemPrice,

        flashSaleQuantity: this.itemQty,

      })

      .subscribe({

        next: (sale) => {

          this.items.set(sale.items);

          this.itemMessage.set(this.t('flashSaleItemAdded'));

          this.itemError.set(false);

          this.productSearch = '';

          this.productResults.set([]);

          this.itemPrice = 0;

          this.itemQty = 1;

        },

        error: (err) => {

          this.itemMessage.set(err?.error?.errors?.[0] ?? this.t('actionFailed'));

          this.itemError.set(true);

        },

      });

  }



  removeItem(item: AdminFlashSaleItem): void {

    this.busyItemId.set(item.id);

    this.flashApi.removeItem(item.id).subscribe({

      next: () => {

        this.items.update((list) => list.filter((i) => i.id !== item.id));

        this.busyItemId.set(null);

      },

      error: () => {

        this.itemMessage.set(this.t('actionFailed'));

        this.itemError.set(true);

        this.busyItemId.set(null);

      },

    });

  }



  submit(): void {

    if (!this.name.trim() || !this.nameAr.trim() || !this.startDate || !this.endDate) {

      this.saveError.set(this.t('flashSaleRequiredFields'));

      return;

    }



    const request: UpsertAdminFlashSaleRequest = {

      name: this.name.trim(),

      nameAr: this.nameAr.trim(),

      description: this.description.trim() || undefined,

      descriptionAr: this.descriptionAr.trim() || undefined,

      imageUrl: this.imageUrl.trim(),

      startDate: new Date(this.startDate).toISOString(),

      endDate: new Date(this.endDate).toISOString(),

      isActive: this.isActive,
      notifySubscribers: this.notifySubscribers,

    };



    this.submitting.set(true);

    this.saveError.set(null);



    const op =

      this.isEdit() && this.flashSaleId != null

        ? this.flashApi.update(this.flashSaleId, request)

        : this.flashApi.create(request);



    op.subscribe({

      next: (sale) => {

        this.submitting.set(false);

        if (!this.isEdit()) {

          void this.router.navigate(['/admin/flash-sales', sale.id]);

        }

      },

      error: (err) => {

        this.saveError.set(err?.error?.errors?.[0] ?? this.t('saveFailed'));

        this.submitting.set(false);

      },

    });

  }



  private toDateInput(iso: string): string {
    return iso.slice(0, 10);
  }

  onImageSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || !this.flashSaleId) return;

    this.uploadingImage.set(true);
    this.mediaApi.uploadFlashSaleImage(this.flashSaleId, file).subscribe({
      next: (res) => {
        this.imageUrl = res.imageUrl;
        this.uploadingImage.set(false);
      },
      error: (err) => {
        this.saveError.set(err?.error?.errors?.[0] ?? this.t('uploadFailed'));
        this.uploadingImage.set(false);
      },
    });
    (event.target as HTMLInputElement).value = '';
  }
}


