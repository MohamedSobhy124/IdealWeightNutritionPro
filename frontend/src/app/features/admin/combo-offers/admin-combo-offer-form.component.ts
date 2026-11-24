import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';

import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';
import { UnsavedChangesForm } from '../../../core/guards/unsaved-changes-form';

import {

  AdminComboOfferImage,
  AdminComboOfferItem,

  UpsertAdminComboOfferRequest,

} from '../../../core/models/admin-combo-offer.models';

import { AdminProductListItem } from '../../../core/models/admin-product.models';

import { AdminComboOfferService } from '../../../core/services/admin-combo-offer.service';
import { AdminMediaService } from '../../../core/services/admin-service.service';
import { AdminProductService } from '../../../core/services/admin-product.service';
import { CatalogueService } from '../../../core/services/catalogue.service';
import { LocaleService } from '../../../core/services/locale.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  UiCardComponent,
  UiEmptyStateComponent,
  UiFormFieldComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  UiAlertComponent,
  UiErrorSummaryComponent,
  UiFormBarComponent,
} from '../../../shared/ui';



@Component({

  changeDetection: ChangeDetectionStrategy.OnPush,

  standalone: true,

  imports: [
    UiAlertComponent,
    UiErrorSummaryComponent,
    UiFormBarComponent,
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

export class AdminComboOfferFormComponent extends UnsavedChangesForm implements OnInit {

  private readonly comboApi = inject(AdminComboOfferService);
  private readonly mediaApi = inject(AdminMediaService);
  private readonly productsApi = inject(AdminProductService);
  readonly catalogue = inject(CatalogueService);

  private readonly route = inject(ActivatedRoute);

  private readonly router = inject(Router);

  readonly locale = inject(LocaleService);
  private readonly toast = inject(UiToastService);



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
  readonly images = signal<AdminComboOfferImage[]>([]);
  readonly uploadingGallery = signal(false);
  galleryCaption = '';
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
  allowFreeDelivery = false;
  freeDeliveryMinimumAmount: number | null = 0;
  freeDeliveryError: string | null = null;

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
          this.images.set(combo.images ?? []);

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
    allowFreeDelivery?: boolean;
    freeDeliveryMinimumAmount?: number;

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
    this.allowFreeDelivery = combo.allowFreeDelivery ?? false;
    this.freeDeliveryMinimumAmount = combo.freeDeliveryMinimumAmount ?? 0;

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

          this.items.update((list) => [...list, combo]);

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



  saveItem(item: AdminComboOfferItem): void {
    if (!this.comboOfferId || item.quantity < 1) {
      this.itemMessage.set(this.t('comboItemFieldsRequired'));
      this.itemError.set(true);
      return;
    }
    this.busyItemId.set(item.id);
    this.comboApi
      .updateItem(this.comboOfferId, item.id, {
        quantity: item.quantity,
        displayOrder: item.displayOrder,
        isRequired: item.isRequired,
      })
      .subscribe({
        next: () => {
          this.items.update((list) => [...list].sort((a, b) => a.displayOrder - b.displayOrder));
          this.itemMessage.set(this.t('comboItemSaved'));
          this.itemError.set(false);
          this.busyItemId.set(null);
        },
        error: (err) => {
          this.itemMessage.set(err?.error?.errors?.[0] ?? this.t('actionFailed'));
          this.itemError.set(true);
          this.busyItemId.set(null);
        },
      });
  }

  onGallerySelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.comboOfferId) return;
    this.uploadingGallery.set(true);
    this.comboApi.addImage(this.comboOfferId, file, this.galleryCaption).subscribe({
      next: (img) => {
        this.images.update((list) => [...list, img]);
        if (!this.imageUrl) this.imageUrl = img.imageUrl;
        this.galleryCaption = '';
        this.uploadingGallery.set(false);
      },
      error: (err) => {
        this.saveError.set(err?.error?.errors?.[0] ?? this.t('uploadFailed'));
        this.uploadingGallery.set(false);
      },
    });
    input.value = '';
  }

  deleteGalleryImage(img: AdminComboOfferImage): void {
    if (!this.comboOfferId) return;
    this.comboApi.deleteImage(this.comboOfferId, img.id).subscribe({
      next: () => {
        this.images.update((list) => list.filter((i) => i.id !== img.id));
        if (this.imageUrl === img.imageUrl) this.imageUrl = this.images()[0]?.imageUrl ?? '';
      },
      error: () => this.saveError.set(this.t('deleteFailed')),
    });
  }

  submit(): void {
    this.freeDeliveryError = null;
    if (this.allowFreeDelivery && !isValidMinimum(this.freeDeliveryMinimumAmount)) {
      this.freeDeliveryError = this.t('freeDeliveryMinimumInvalid');
      this.saveError.set(this.freeDeliveryError);
      return;
    }

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
      allowFreeDelivery: this.allowFreeDelivery,
      freeDeliveryMinimumAmount: this.allowFreeDelivery ? Number(this.freeDeliveryMinimumAmount) : 0,
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
        this.markSaved();
        this.toast.success(this.t('saved'));

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

  cancel(): void {
    void this.router.navigate(['/admin/combo-offers']);
  }
}

/** A free-delivery minimum is a number of 0 or more (0 = always free). */
function isValidMinimum(value: unknown): boolean {
  return value !== null && value !== undefined && value !== '' && !Number.isNaN(Number(value)) && Number(value) >= 0;
}
