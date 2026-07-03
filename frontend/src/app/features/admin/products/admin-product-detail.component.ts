import { Component, inject, OnInit, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';

import { ActivatedRoute, RouterLink } from '@angular/router';

import { ADMIN_UI, AdminUiKey } from '../../../core/i18n/admin-ui-text';

import {

  AdminProductDetail,

  AdminProductImage,

  AdminProductOption,

  AdminProductVariant,

} from '../../../core/models/admin-product.models';

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

  templateUrl: './admin-product-detail.component.html',

  styleUrl: './admin-product-detail.component.css',

})

export class AdminProductDetailComponent implements OnInit {

  private readonly route = inject(ActivatedRoute);

  private readonly productsApi = inject(AdminProductService);

  readonly catalogue = inject(CatalogueService);

  readonly locale = inject(LocaleService);



  readonly product = signal<AdminProductDetail | null>(null);

  readonly variants = signal<AdminProductVariant[]>([]);

  readonly options = signal<AdminProductOption[]>([]);

  readonly images = signal<AdminProductImage[]>([]);

  readonly uploading = signal(false);

  readonly loading = signal(true);

  readonly saving = signal(false);

  readonly converting = signal(false);

  readonly generating = signal(false);

  readonly optionBusy = signal(false);

  readonly variantBusyId = signal<number | null>(null);

  readonly variantImageUploadingId = signal<number | null>(null);
  readonly infoImageUploading = signal(false);

  readonly error = signal<string | null>(null);

  readonly saveError = signal<string | null>(null);

  readonly saveMessage = signal<string | null>(null);



  title = '';

  price = 0;

  listPrice = 0;

  storeCost: number | null = null;

  stockQuantity = 0;

  minimumStockAlert = 5;

  isNew = false;

  isTrending = false;

  allowFreeDelivery = false;

  freeDeliveryMinimumAmount = 0;

  isDeleted = false;



  newOptionName = '';

  newOptionNameAr = '';

  newOptionOrder = 0;

  newValueText: Record<number, string> = {};

  newValueTextAr: Record<number, string> = {};

  newValueOrder: Record<number, number> = {};



  t(key: AdminUiKey): string {

    return this.locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);

  }



  ngOnInit(): void {

    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!id) {

      this.error.set(this.t('invalidProduct'));

      this.loading.set(false);

      return;

    }



    this.loadProduct(id);

  }



  loadProduct(id: number): void {

    this.productsApi.get(id).subscribe({

      next: (product) => {

        this.applyProduct(product);

        if (product.productType === 'Variable') {

          this.loadOptions(id);

        }

        this.loading.set(false);

      },

      error: () => {

        this.error.set(this.t('productNotFound'));

        this.loading.set(false);

      },

    });

  }



  applyProduct(product: AdminProductDetail): void {

    this.product.set(product);

    this.title = product.title;

    this.price = product.price;

    this.listPrice = product.listPrice;

    this.storeCost = product.storeCost ?? null;

    this.stockQuantity = product.stockQuantity;

    this.minimumStockAlert = product.minimumStockAlert ?? 5;

    this.isNew = product.isNew;

    this.isTrending = product.isTrending;

    this.allowFreeDelivery = product.allowFreeDelivery;

    this.freeDeliveryMinimumAmount = product.freeDeliveryMinimumAmount ?? 0;

    this.isDeleted = product.isDeleted;

    this.variants.set(
      product.variants.map((v) => ({
        ...v,
        minimumStockAlert: v.minimumStockAlert ?? 5,
        expiryDate: v.expiryDate ? v.expiryDate.slice(0, 10) : undefined,
      }))
    );

    this.options.set(product.options ?? []);

    this.images.set(product.images ?? []);

  }



  loadOptions(productId: number): void {

    this.productsApi.getOptions(productId).subscribe({

      next: (opts) => this.options.set(opts),

    });

  }



  isVariable(): boolean {

    return this.product()?.productType === 'Variable';

  }



  convertToVariable(): void {

    const p = this.product();

    if (!p) return;



    this.converting.set(true);

    this.saveError.set(null);

    this.productsApi.setProductType(p.id, { productType: 'Variable' }).subscribe({

      next: () => {

        this.productsApi.get(p.id).subscribe({

          next: (updated) => {

            this.applyProduct(updated);

            this.loadOptions(p.id);

            this.saveMessage.set(this.t('productTypeConverted'));

            this.converting.set(false);

          },

        });

      },

      error: (err) => {

        this.saveError.set(err.error?.errors?.[0] ?? this.t('actionFailed'));

        this.converting.set(false);

      },

    });

  }



  addOption(): void {

    const p = this.product();

    if (!p || !this.newOptionName.trim() || !this.newOptionNameAr.trim()) {

      this.saveError.set(this.t('optionRequiredFields'));

      return;

    }



    this.optionBusy.set(true);

    this.saveError.set(null);

    this.productsApi

      .addOption(p.id, {

        name: this.newOptionName.trim(),

        nameAr: this.newOptionNameAr.trim(),

        displayOrder: this.newOptionOrder,

      })

      .subscribe({

        next: () => {

          this.newOptionName = '';

          this.newOptionNameAr = '';

          this.newOptionOrder = 0;

          this.loadOptions(p.id);

          this.optionBusy.set(false);

        },

        error: (err) => {

          this.saveError.set(err.error?.errors?.[0] ?? this.t('actionFailed'));

          this.optionBusy.set(false);

        },

      });

  }



  removeOption(optionId: number): void {

    const p = this.product();

    if (!p || !confirm(this.t('confirmDeleteOption'))) return;



    this.optionBusy.set(true);

    this.productsApi.deleteOption(p.id, optionId).subscribe({

      next: () => {

        this.loadOptions(p.id);

        this.optionBusy.set(false);

      },

      error: (err) => {

        this.saveError.set(err.error?.errors?.[0] ?? this.t('actionFailed'));

        this.optionBusy.set(false);

      },

    });

  }



  addOptionValue(option: AdminProductOption): void {

    const p = this.product();

    const value = (this.newValueText[option.id] ?? '').trim();

    const valueAr = (this.newValueTextAr[option.id] ?? '').trim();

    if (!p || !value || !valueAr) {

      this.saveError.set(this.t('optionValueRequiredFields'));

      return;

    }



    this.optionBusy.set(true);

    this.productsApi

      .addOptionValue(p.id, option.id, {

        value,

        valueAr,

        displayOrder: this.newValueOrder[option.id] ?? 0,

      })

      .subscribe({

        next: () => {

          this.newValueText[option.id] = '';

          this.newValueTextAr[option.id] = '';

          this.newValueOrder[option.id] = 0;

          this.loadOptions(p.id);

          this.optionBusy.set(false);

        },

        error: (err) => {

          this.saveError.set(err.error?.errors?.[0] ?? this.t('actionFailed'));

          this.optionBusy.set(false);

        },

      });

  }



  removeOptionValue(valueId: number): void {

    const p = this.product();

    if (!p || !confirm(this.t('confirmDeleteOptionValue'))) return;



    this.optionBusy.set(true);

    this.productsApi.deleteOptionValue(p.id, valueId).subscribe({

      next: () => {

        this.loadOptions(p.id);

        this.optionBusy.set(false);

      },

      error: (err) => {

        this.saveError.set(err.error?.errors?.[0] ?? this.t('actionFailed'));

        this.optionBusy.set(false);

      },

    });

  }



  generateVariants(): void {

    const p = this.product();

    if (!p) return;



    this.generating.set(true);

    this.saveError.set(null);

    this.productsApi.generateVariants(p.id).subscribe({

      next: (res) => {

        this.saveMessage.set(res.message);

        this.productsApi.get(p.id).subscribe({

          next: (updated) => {

            this.applyProduct(updated);

            this.generating.set(false);

          },

        });

      },

      error: (err) => {

        this.saveError.set(err.error?.errors?.[0] ?? this.t('actionFailed'));

        this.generating.set(false);

      },

    });

  }



  saveVariant(variant: AdminProductVariant): void {

    const p = this.product();

    if (!p) return;



    this.variantBusyId.set(variant.id);

    this.saveError.set(null);

    this.productsApi

      .updateVariant(p.id, variant.id, {

        price: variant.price,

        listPrice: variant.listPrice,
        price50: variant.price50,
        price100: variant.price100,

        stockQuantity: variant.stockQuantity,

        minimumStockAlert: variant.minimumStockAlert,

        sku: variant.sku,

        expiryDate: variant.expiryDate || undefined,

      })

      .subscribe({

        next: () => {

          this.saveMessage.set(this.t('variantSaved'));

          this.variantBusyId.set(null);

        },

        error: (err) => {

          this.saveError.set(err.error?.errors?.[0] ?? this.t('saveFailed'));

          this.variantBusyId.set(null);

        },

      });

  }



  onVariantImageSelected(event: Event, variant: AdminProductVariant): void {
    const p = this.product();
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!p || !file) return;

    this.variantImageUploadingId.set(variant.id);
    this.saveError.set(null);

    this.productsApi.uploadVariantImage(p.id, variant.id, file).subscribe({
      next: (result) => {
        variant.imageUrl = result.imageUrl;
        this.variants.update((list) =>
          list.map((v) => (v.id === variant.id ? { ...v, imageUrl: result.imageUrl } : v))
        );
        this.saveMessage.set(this.t('variantImageUploaded'));
        this.variantImageUploadingId.set(null);
        input.value = '';
      },
      error: (err) => {
        this.saveError.set(err.error?.errors?.[0] ?? this.t('uploadFailed'));
        this.variantImageUploadingId.set(null);
        input.value = '';
      },
    });
  }



  optionLabel(option: AdminProductOption): string {

    return this.locale.pick(option.name, option.nameAr);

  }



  optionValueLabel(value: { value: string; valueAr: string }): string {

    return this.locale.pick(value.value, value.valueAr);

  }



  variantLabel(variant: AdminProductVariant): string {

    if (variant.variantName) return variant.variantName;

    return variant.sku ? `SKU ${variant.sku}` : `Variant #${variant.id}`;

  }



  save(): void {

    const p = this.product();

    if (!p) return;



    this.saving.set(true);

    this.saveError.set(null);

    this.saveMessage.set(null);



    this.productsApi

      .update(p.id, {

        title: this.title,

        price: this.price,

        listPrice: this.listPrice,

        storeCost: this.storeCost,

        stockQuantity: this.stockQuantity,

        minimumStockAlert: this.minimumStockAlert,

        isNew: this.isNew,

        isTrending: this.isTrending,

        allowFreeDelivery: this.allowFreeDelivery,

        freeDeliveryMinimumAmount: this.freeDeliveryMinimumAmount,

        isDeleted: this.isDeleted,

      })

      .subscribe({

        next: (updated) => {

          this.applyProduct(updated);

          this.saveMessage.set(this.t('productSaved'));

          this.saving.set(false);

        },

        error: (err) => {

          this.saveError.set(err.error?.errors?.[0] ?? 'Could not save product.');

          this.saving.set(false);

        },

      });

  }



  onImageSelected(event: Event): void {

    const p = this.product();

    const input = event.target as HTMLInputElement;

    const file = input.files?.[0];

    if (!p || !file) return;



    this.uploading.set(true);

    this.saveError.set(null);

    this.productsApi.uploadImage(p.id, file).subscribe({

      next: (image) => {

        this.images.update((list) => [...list, image]);

        this.product.update((current) =>

          current ? { ...current, imageUrl: current.imageUrl || image.imageUrl, images: [...this.images()] } : current

        );

        this.uploading.set(false);

        input.value = '';

      },

      error: (err) => {

        this.saveError.set(err.error?.errors?.[0] ?? 'Image upload failed.');

        this.uploading.set(false);

        input.value = '';

      },

    });

  }

  onInfoImageSelected(event: Event): void {
    const p = this.product();
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!p || !file) return;

    this.infoImageUploading.set(true);
    this.saveError.set(null);
    this.productsApi.uploadInfoImage(p.id, file).subscribe({
      next: (image) => {
        this.images.update((list) => [...list, image]);
        this.infoImageUploading.set(false);
        input.value = '';
      },
      error: (err) => {
        this.saveError.set(err.error?.errors?.[0] ?? this.t('uploadFailed'));
        this.infoImageUploading.set(false);
        input.value = '';
      },
    });
  }

  updateImageInfo(image: AdminProductImage): void {
    const p = this.product();
    if (!p) return;
    this.productsApi.updateImageInfo(p.id, image.id, image.imageInfo).subscribe({
      next: () => {
        this.saveMessage.set(this.t('saved'));
      },
      error: (err) => {
        this.saveError.set(err.error?.errors?.[0] ?? this.t('saveFailed'));
      },
    });
  }



  removeImage(imageId: number): void {

    const p = this.product();

    if (!p) return;



    this.productsApi.deleteImage(p.id, imageId).subscribe({

      next: () => {

        this.images.update((list) => list.filter((i) => i.id !== imageId));

        this.productsApi.get(p.id).subscribe({

          next: (updated) => this.product.set(updated),

        });

      },

      error: (err) => this.saveError.set(err.error?.errors?.[0] ?? 'Could not delete image.'),

    });

  }

}


