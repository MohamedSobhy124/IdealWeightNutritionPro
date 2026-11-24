import { ChangeDetectionStrategy, Component, computed, HostListener, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HasUnsavedChanges } from '../../../core/guards/unsaved-changes.guard';
import { IwnCurrencyPipe } from '../../../core/pipes/iwn-currency.pipe';
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
import { UiConfirmService } from '../../../shared/ui/ui-confirm.service';
import { UiToastService } from '../../../shared/ui/ui-toast.service';
import {
  UiBadgeComponent,
  UiCardComponent,
  UiEmptyStateComponent,
  UiErrorSummaryComponent,
  UiFormBarComponent,
  UiFormFieldComponent,
  UiFormSectionNavComponent,
  UiPageHeaderComponent,
  UiSkeletonComponent,
  type UiFormError,
  type UiFormSection,
} from '../../../shared/ui';


@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterLink,
    FormsModule,
    UiPageHeaderComponent,
    UiCardComponent,
    UiFormFieldComponent,
    UiEmptyStateComponent,
    UiSkeletonComponent,
    UiBadgeComponent,
    UiErrorSummaryComponent,
    UiFormBarComponent,
    UiFormSectionNavComponent,
    IwnCurrencyPipe,
  ],
  templateUrl: './admin-product-detail.component.html',
  styleUrl: './admin-product-detail.component.css',
})
export class AdminProductDetailComponent implements OnInit, HasUnsavedChanges {
  private readonly route = inject(ActivatedRoute);
  private readonly productsApi = inject(AdminProductService);
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);
  private readonly confirmDialog = inject(UiConfirmService);
  private readonly toast = inject(UiToastService);
  private readonly router = inject(Router);


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
  readonly formErrors = signal<UiFormError[]>([]);
  /** JSON of the main form fields as last loaded/saved; compared to detect unsaved edits. */
  private savedSnapshot = '';


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
  expiryDate = '';


  newOptionName = '';
  newOptionNameAr = '';
  newOptionOrder = 0;
  newValueText: Record<number, string> = {};
  newValueTextAr: Record<number, string> = {};
  newValueOrder: Record<number, number> = {};


  // ---- Form layout, dirty state, validation -----------------------------
  readonly sections = computed<UiFormSection[]>(() => {
    const errors = this.formErrors();
    const count = (ids: string[]) => errors.filter((e) => e.fieldId && ids.includes(e.fieldId)).length;
    const list: UiFormSection[] = [
      { id: 'pd-basic', label: this.t('sectionBasicInfo'), errors: count(['pd-title']) },
      { id: 'pd-pricing', label: this.t('sectionPricing'), errors: count(['pd-price', 'pd-list', 'pd-stock']) },
      { id: 'pd-status', label: this.t('sectionVisibility') },
    ];
    if (this.product()?.productType === 'Simple') {
      list.push({ id: 'pd-variant-setup', label: this.t('sectionVariantSetup') });
    } else {
      list.push({ id: 'pd-options', label: this.t('productOptions') });
      if (this.variants().length) list.push({ id: 'pd-variants', label: this.t('variants') });
    }
    list.push({ id: 'pd-images', label: this.t('images') });
    return list;
  });

  discountPercent(): number {
    if (!this.listPrice || this.price >= this.listPrice) return 0;
    return Math.round((1 - this.price / this.listPrice) * 100);
  }

  private formSnapshot(): string {
    return JSON.stringify([
      this.title, this.price, this.listPrice, this.storeCost, this.stockQuantity, this.minimumStockAlert,
      this.isNew, this.isTrending, this.allowFreeDelivery, this.freeDeliveryMinimumAmount, this.isDeleted, this.expiryDate,
    ]);
  }

  hasUnsavedChanges(): boolean {
    return !!this.product() && !this.saving() && this.formSnapshot() !== this.savedSnapshot;
  }

  @HostListener('window:beforeunload', ['$event'])
  protected onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) event.preventDefault();
  }

  private validate(): UiFormError[] {
    const errors: UiFormError[] = [];
    const bad = (n: unknown) => n === null || n === undefined || n === '' || Number.isNaN(Number(n)) || Number(n) < 0;
    if (!this.title.trim()) errors.push({ fieldId: 'pd-title', message: this.t('titleRequired') });
    if (bad(this.price)) errors.push({ fieldId: 'pd-price', message: this.t('priceRequired') });
    if (bad(this.listPrice)) errors.push({ fieldId: 'pd-list', message: this.t('listPriceRequired') });
    if (this.product()?.productType === 'Simple' && bad(this.stockQuantity)) {
      errors.push({ fieldId: 'pd-stock', message: this.t('stockRequired') });
    }
    if (this.allowFreeDelivery && bad(this.freeDeliveryMinimumAmount)) {
      errors.push({ fieldId: 'pd-free-min', message: this.t('freeDeliveryMinimumInvalid') });
    }
    return errors;
  }

  fieldError(id: string): string | null {
    return this.formErrors().find((e) => e.fieldId === id)?.message ?? null;
  }

  cancel(): void {
    void this.router.navigate(['/admin/products']);
  }

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
    this.expiryDate = product.expiryDate ? product.expiryDate.slice(0, 10) : '';
    this.variants.set(
      product.variants.map((v) => ({
        ...v,
        minimumStockAlert: v.minimumStockAlert ?? 5,
        expiryDate: v.expiryDate ? v.expiryDate.slice(0, 10) : undefined,
      }))
    );
    this.options.set(product.options ?? []);
    this.images.set(product.images ?? []);
    this.savedSnapshot = this.formSnapshot();
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
    this.productsApi.setProductType(p.id, { productType: 'Variable' }).subscribe({
      next: () => {
        this.productsApi.get(p.id).subscribe({
          next: (updated) => {
            this.applyProduct(updated);
            this.loadOptions(p.id);
            this.toast.success(this.t('productTypeConverted'));
            this.converting.set(false);
          },
        });
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.converting.set(false);
      },
    });
  }


  addOption(): void {
    const p = this.product();
    if (!p || !this.newOptionName.trim() || !this.newOptionNameAr.trim()) {
      this.toast.error(this.t('optionRequiredFields'));
      return;
    }


    this.optionBusy.set(true);
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
          this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
          this.optionBusy.set(false);
        },
      });
  }


  async removeOption(optionId: number): Promise<void> {
    const p = this.product();
    if (!p || !(await this.confirmDialog.danger(this.t('confirmDeleteOption')))) return;


    this.optionBusy.set(true);
    this.productsApi.deleteOption(p.id, optionId).subscribe({
      next: () => {
        this.loadOptions(p.id);
        this.optionBusy.set(false);
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.optionBusy.set(false);
      },
    });
  }


  addOptionValue(option: AdminProductOption): void {
    const p = this.product();
    const value = (this.newValueText[option.id] ?? '').trim();
    const valueAr = (this.newValueTextAr[option.id] ?? '').trim();
    if (!p || !value || !valueAr) {
      this.toast.error(this.t('optionValueRequiredFields'));
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
          this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
          this.optionBusy.set(false);
        },
      });
  }


  async removeOptionValue(valueId: number): Promise<void> {
    const p = this.product();
    if (!p || !(await this.confirmDialog.danger(this.t('confirmDeleteOptionValue')))) return;


    this.optionBusy.set(true);
    this.productsApi.deleteOptionValue(p.id, valueId).subscribe({
      next: () => {
        this.loadOptions(p.id);
        this.optionBusy.set(false);
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.optionBusy.set(false);
      },
    });
  }


  generateVariants(): void {
    const p = this.product();
    if (!p) return;


    this.generating.set(true);
    this.productsApi.generateVariants(p.id).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.productsApi.get(p.id).subscribe({
          next: (updated) => {
            this.applyProduct(updated);
            this.generating.set(false);
          },
        });
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed'));
        this.generating.set(false);
      },
    });
  }


  saveVariant(variant: AdminProductVariant): void {
    const p = this.product();
    if (!p) return;


    this.variantBusyId.set(variant.id);
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
          this.toast.success(this.t('variantSaved'));
          this.variantBusyId.set(null);
        },
        error: (err) => {
          this.toast.error(err.error?.errors?.[0] ?? this.t('saveFailed'));
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
    this.productsApi.uploadVariantImage(p.id, variant.id, file).subscribe({
      next: (result) => {
        variant.imageUrl = result.imageUrl;
        this.variants.update((list) =>
          list.map((v) => (v.id === variant.id ? { ...v, imageUrl: result.imageUrl } : v))
        );
        this.toast.success(this.t('variantImageUploaded'));
        this.variantImageUploadingId.set(null);
        input.value = '';
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('uploadFailed'));
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
    const errors = this.validate();
    this.formErrors.set(errors);
    if (errors.length) return;
    this.saving.set(true);


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
        expiryDate: this.expiryDate || null,
      })
      .subscribe({
        next: (updated) => {
          this.applyProduct(updated);
          this.toast.success(this.t('productSaved'));
          this.saving.set(false);
        },
        error: (err) => {
          this.toast.error(err.error?.errors?.[0] ?? this.t('saveFailed'));
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
        this.toast.error(err.error?.errors?.[0] ?? this.t('uploadFailed'));
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
    this.productsApi.uploadInfoImage(p.id, file).subscribe({
      next: (image) => {
        this.images.update((list) => [...list, image]);
        this.infoImageUploading.set(false);
        input.value = '';
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('uploadFailed'));
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
        this.toast.success(this.t('saved'));
      },
      error: (err) => {
        this.toast.error(err.error?.errors?.[0] ?? this.t('saveFailed'));
      },
    });
  }


  async removeImage(imageId: number): Promise<void> {
    const p = this.product();
    if (!p || !(await this.confirmDialog.danger(this.t('confirmRemoveImage'), undefined, this.t('remove')))) return;


    this.productsApi.deleteImage(p.id, imageId).subscribe({
      next: () => {
        this.images.update((list) => list.filter((i) => i.id !== imageId));
        this.productsApi.get(p.id).subscribe({
          next: (updated) => this.product.set(updated),
        });
      },
      error: (err) => this.toast.error(err.error?.errors?.[0] ?? this.t('actionFailed')),
    });
  }
}


