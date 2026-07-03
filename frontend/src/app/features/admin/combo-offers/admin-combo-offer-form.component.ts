import { Component, inject, OnInit, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';

import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';

import {

  AdminComboOfferItem,

  UpsertAdminComboOfferRequest,

} from '../../../core/models/admin-combo-offer.models';

import { AdminProductListItem } from '../../../core/models/admin-product.models';

import { AdminComboOfferService } from '../../../core/services/admin-combo-offer.service';
import { AdminMediaService } from '../../../core/services/admin-service.service';
import { AdminProductService } from '../../../core/services/admin-product.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import {
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
} from '../../../shared/ui';



@Component({

  standalone: true,

  imports: [
    RouterLink,
    FormsModule,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiEmptyStateComponent,
    UiSkeletonComponent,
  ],

  templateUrl: './admin-combo-offer-form.component.html',

  styleUrl: './admin-combo-offer-form.component.css',

})

export class AdminComboOfferFormComponent implements OnInit {

  private readonly comboApi = inject(AdminComboOfferService);
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

  readonly items = signal<AdminComboOfferItem[]>([]);

  readonly productResults = signal<AdminProductListItem[]>([]);

  readonly busyItemId = signal<number | null>(null);
  readonly uploadingImage = signal(false);



  comboOfferId: number | null = null;

  productSearch = '';

  itemQty = 1;

  itemRequired = true;



  name = '';

  nameAr = '';

  description = '';

  descriptionAr = '';

  imageUrl = '';

  comboPrice = 0;

  minimumQuantity = 1;
  maximumQuantity: number | null = null;
  displayOrder = 0;

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

        this.error.set(this.t('invalidComboOffer'));

        this.loading.set(false);

        return;

      }

      this.isEdit.set(true);

      this.comboOfferId = id;

      this.comboApi.get(id).subscribe({

        next: (combo) => {

          this.applyCombo(combo);

          this.items.set(combo.items);

          this.loading.set(false);

        },

        error: () => {

          this.error.set(this.t('comboOfferNotFound'));

          this.loading.set(false);

        },

      });

    } else {

      const today = new Date();

      const nextMonth = new Date(today);

      nextMonth.setMonth(nextMonth.getMonth() + 1);

      this.startDate = this.toDateInput(today.toISOString());

      this.endDate = this.toDateInput(nextMonth.toISOString());

      this.loading.set(false);

    }

  }



  private applyCombo(combo: {

    name: string;

    nameAr?: string;

    description?: string;

    descriptionAr?: string;

    imageUrl: string;

    comboPrice: number;

    minimumQuantity: number;
    maximumQuantity?: number;
    displayOrder: number;

    startDate: string;

    endDate: string;

    isActive: boolean;

  }): void {

    this.name = combo.name;

    this.nameAr = combo.nameAr ?? '';

    this.description = combo.description ?? '';

    this.descriptionAr = combo.descriptionAr ?? '';

    this.imageUrl = combo.imageUrl;

    this.comboPrice = combo.comboPrice;

    this.minimumQuantity = combo.minimumQuantity || 1;
    this.maximumQuantity = combo.maximumQuantity || null;
    this.displayOrder = combo.displayOrder ?? 0;

    this.startDate = this.toDateInput(combo.startDate);

    this.endDate = this.toDateInput(combo.endDate);

    this.isActive = combo.isActive;

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

    if (!this.comboOfferId || this.itemQty < 1) {

      this.itemMessage.set(this.t('comboItemFieldsRequired'));

      this.itemError.set(true);

      return;

    }

    this.comboApi

      .addItem(this.comboOfferId, {

        productId,

        quantity: this.itemQty,

        isRequired: this.itemRequired,

      })

      .subscribe({

        next: (combo) => {

          this.items.set(combo.items);

          this.itemMessage.set(this.t('comboItemAdded'));

          this.itemError.set(false);

          this.productSearch = '';

          this.productResults.set([]);

          this.itemQty = 1;

          this.itemRequired = true;

        },

        error: (err) => {

          this.itemMessage.set(err?.error?.errors?.[0] ?? this.t('actionFailed'));

          this.itemError.set(true);

        },

      });

  }



  removeItem(item: AdminComboOfferItem): void {

    if (item.id == null) return;

    this.busyItemId.set(item.id);

    this.comboApi.removeItem(item.id).subscribe({

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

    if (!this.name.trim() || !this.nameAr.trim() || !this.startDate || !this.endDate || this.comboPrice <= 0) {

      this.saveError.set(this.t('comboOfferRequiredFields'));

      return;

    }



    const request: UpsertAdminComboOfferRequest = {

      name: this.name.trim(),

      nameAr: this.nameAr.trim(),

      description: this.description.trim() || undefined,

      descriptionAr: this.descriptionAr.trim() || undefined,

      imageUrl: this.imageUrl.trim(),

      comboPrice: this.comboPrice,

      startDate: new Date(this.startDate).toISOString(),

      endDate: new Date(this.endDate).toISOString(),

      minimumQuantity: this.minimumQuantity || 1,
      maximumQuantity: this.maximumQuantity ?? undefined,
      displayOrder: this.displayOrder || 0,
      notifySubscribers: this.notifySubscribers,

      isActive: this.isActive,

    };



    this.submitting.set(true);

    this.saveError.set(null);



    const op =

      this.isEdit() && this.comboOfferId != null

        ? this.comboApi.update(this.comboOfferId, request)

        : this.comboApi.create(request);



    op.subscribe({

      next: (combo) => {

        this.submitting.set(false);

        if (!this.isEdit()) {

          void this.router.navigate(['/admin/combo-offers', combo.id]);

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
    if (!file || !this.comboOfferId) return;

    this.uploadingImage.set(true);
    this.mediaApi.uploadComboOfferImage(this.comboOfferId, file).subscribe({
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


