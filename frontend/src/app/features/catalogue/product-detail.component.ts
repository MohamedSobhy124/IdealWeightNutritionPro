import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CatalogueService } from '../../core/services/catalogue.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { StockNotificationService } from '../../core/services/stock-notification.service';
import { WhatsAppService, WhatsAppSettings } from '../../core/services/whatsapp.service';
import { LocaleService } from '../../core/services/locale.service';
import { SeoService } from '../../core/services/seo.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { Brand, ProductDetail, ProductOption, ProductVariant } from '../../core/models/catalogue.models';
import { ProductReviewsComponent } from './product-reviews.component';
import { UiBadgeComponent, UiCardComponent, UiEmptyStateComponent, UiModalComponent, UiPageHeaderComponent, UiSkeletonComponent, type UiBreadcrumb } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DecimalPipe,
    FormsModule,
    ProductReviewsComponent,
    IwnCurrencyPipe,
    UiBadgeComponent,
    UiCardComponent,
    UiModalComponent,
    UiSkeletonComponent,
    UiPageHeaderComponent,
    UiEmptyStateComponent,
  ],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css',
})
export class ProductDetailComponent implements OnInit {
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);
  readonly auth = inject(AuthService);
  readonly wishlist = inject(WishlistService);
  private readonly stockNotify = inject(StockNotificationService);
  private readonly whatsapp = inject(WhatsAppService);
  private readonly cart = inject(CartService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);

  quantity = 1;
  readonly product = signal<ProductDetail | null>(null);
  readonly selectedOptionValues = signal<Record<number, number>>({});
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly adding = signal(false);
  readonly addMessage = signal<string | null>(null);
  readonly wishlistMessage = signal<string | null>(null);
  readonly wishlistBusy = signal(false);
  readonly stockNotifyEmail = signal('');
  readonly stockNotifyPhone = signal('');
  readonly stockNotifyMessage = signal<string | null>(null);
  readonly stockNotifyBusy = signal(false);
  readonly stockNotifyError = signal(false);
  readonly whatsappSettings = signal<WhatsAppSettings | null>(null);
  readonly selectedImage = signal<string | null>(null);
  readonly detailsExpanded = signal(true);
  readonly specificationExpanded = signal(false);
  readonly deliveryExpanded = signal(false);
  readonly imageZoomOpen = signal(false);

  readonly selectedVariant = computed(() => this.resolveSelectedVariant());
  readonly displayPrice = computed(() => {
    const variant = this.selectedVariant();
    const product = this.product();
    if (variant) return variant.price;
    return product?.price ?? 0;
  });
  readonly displayListPrice = computed(() => {
    const variant = this.selectedVariant();
    const product = this.product();
    if (variant?.listPrice) return variant.listPrice;
    return product?.listPrice ?? 0;
  });
  readonly displayInStock = computed(() => {
    const product = this.product();
    if (!product) return false;
    if (product.productType === 'Variable') {
      const variant = this.selectedVariant();
      return !!variant && variant.stockQuantity > 0;
    }
    return product.inStock;
  });
  readonly displayStockQuantity = computed(() => {
    const product = this.product();
    if (!product) return 0;
    if (product.productType === 'Variable') {
      return this.selectedVariant()?.stockQuantity ?? 0;
    }
    return product.stockQuantity;
  });
  readonly displayImages = computed(() => {
    const product = this.product();
    if (!product) return [] as string[];
    const uniqueImages = this.uniqueImageUrls(product.imageUrls);
    const variantImage = this.selectedVariant()?.imageUrl;
    if (variantImage) {
      return [variantImage, ...uniqueImages.filter((url) => url !== variantImage)];
    }
    return uniqueImages;
  });
  readonly activeImage = computed(() => this.selectedImage() ?? this.displayImages()[0] ?? '');

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  productTitle(p: ProductDetail): string {
    return this.locale.pick(p.title, p.titleAr);
  }

  productBreadcrumbs(p: ProductDetail): UiBreadcrumb[] {
    const crumbs: UiBreadcrumb[] = [{ label: this.t('shop'), link: '/shop' }];
    if (p.category) {
      crumbs.push({
        label: this.locale.pick(p.category.name, p.category.nameAr),
        link: '/shop',
        queryParams: { categoryId: p.category.id },
      });
    }
    return crumbs;
  }

  optionName(option: ProductOption): string {
    return this.locale.pick(option.name, option.nameAr);
  }

  optionValueLabel(option: ProductOption, valueId: number): string {
    const value = option.values.find((v) => v.id === valueId);
    return value ? this.locale.pick(value.value, value.valueAr) : '';
  }

  brandName(brand: Brand): string {
    return this.locale.pick(brand.name, brand.nameAr);
  }

  productDescription(p: ProductDetail): string {
    return this.locale.pick(p.description, p.descriptionAr);
  }

  productSpecification(p: ProductDetail): string | undefined {
    const spec = this.locale.pick(p.specification ?? '', p.specificationAr ?? '');
    return spec || undefined;
  }

  productWhatsAppUrl(p: ProductDetail): string | null {
    const settings = this.whatsappSettings();
    if (!settings?.enabled || !settings.phoneNumber) return null;

    const intro = this.locale.pick(settings.defaultMessage, settings.defaultMessageAr);
    const title = this.productTitle(p);
    const message = `${intro}\n\n${title}\n${window.location.href}`;
    return this.whatsapp.buildChatUrl(settings.phoneNumber, message);
  }

  isVariable(p: ProductDetail): boolean {
    return p.productType === 'Variable';
  }

  ngOnInit(): void {
    this.whatsapp.getSettings().subscribe((settings) => this.whatsappSettings.set(settings));

    this.route.paramMap.subscribe((params) => {
      const slug = params.get('slug');
      if (!slug) {
        this.error.set(this.t('productNotFound'));
        this.loading.set(false);
        return;
      }
      this.loading.set(true);
      this.catalogue.getProductBySlug(slug).subscribe({
        next: (p) => {
          this.product.set(p);
          this.initializeVariantSelection(p);
          this.selectedImage.set(this.uniqueImageUrls(p.imageUrls)[0] ?? null);
          if (p) {
            this.seo.applyPage({
              title: p.title,
              titleAr: p.titleAr,
              description: this.truncate(p.description, 160),
              descriptionAr: this.truncate(p.descriptionAr, 160),
              imageUrl: this.uniqueImageUrls(p.imageUrls)[0],
              path: `/product/${p.slug}`,
              type: 'product',
            });
          }
          this.loading.set(false);
        },
        error: () => {
          this.error.set(this.t('productNotFound'));
          this.loading.set(false);
        },
      });
    });
  }

  selectOptionValue(optionId: number, valueId: number): void {
    this.selectedOptionValues.update((current) => ({ ...current, [optionId]: valueId }));
    this.quantity = 1;
    this.selectedImage.set(null);
  }

  changeQuantity(delta: number): void {
    const max = this.displayStockQuantity() || 99;
    this.quantity = Math.max(1, Math.min(max, this.quantity + delta));
  }

  chooseImage(url: string): void {
    this.selectedImage.set(url);
  }

  isOptionValueAvailable(optionId: number, valueId: number): boolean {
    const product = this.product();
    if (!product || product.productType !== 'Variable') return true;

    const tentative = { ...this.selectedOptionValues(), [optionId]: valueId };
    const selectedIds = Object.values(tentative);
    return product.variants.some(
      (variant) =>
        variant.stockQuantity > 0 &&
        selectedIds.every((id) => variant.optionValueIds.includes(id)) &&
        variant.optionValueIds.length >= selectedIds.length
    );
  }

  isInWishlist(productId: number): boolean {
    return this.wishlist.isInWishlist(productId);
  }

  toggleWishlist(product: ProductDetail): void {
    if (!this.auth.isAuthenticated()) {
      this.wishlistMessage.set(this.t('loginForWishlist'));
      return;
    }
    this.wishlistBusy.set(true);
    this.wishlistMessage.set(null);
    this.wishlist.toggle(product.id).subscribe({
      next: (res) => {
        this.wishlistMessage.set(res.isInWishlist ? this.t('wishlistAdded') : this.t('wishlistRemoved'));
        this.wishlistBusy.set(false);
      },
      error: () => {
        this.wishlistMessage.set(this.t('wishlistFailed'));
        this.wishlistBusy.set(false);
      },
    });
  }

  subscribeStockNotify(product: ProductDetail): void {
    const email = this.stockNotifyEmail().trim();
    const phone = this.stockNotifyPhone().trim();
    if (!email && !phone) {
      this.stockNotifyMessage.set(this.t('stockNotifyContactRequired'));
      this.stockNotifyError.set(true);
      return;
    }
    this.stockNotifyBusy.set(true);
    this.stockNotifyMessage.set(null);
    this.stockNotifyError.set(false);
    const variantId = this.selectedVariant()?.id;
    this.stockNotify
      .subscribe(product.id, {
        email: email || undefined,
        phoneNumber: phone || undefined,
        productVariantId: variantId,
      })
      .subscribe({
        next: (res) => {
          this.stockNotifyMessage.set(res.message || this.t('stockNotifySubscribed'));
          this.stockNotifyBusy.set(false);
        },
        error: (err) => {
          this.stockNotifyMessage.set(err?.error?.errors?.[0] ?? this.t('stockNotifyFailed'));
          this.stockNotifyError.set(true);
          this.stockNotifyBusy.set(false);
        },
      });
  }

  addToCart(product: ProductDetail): void {
    const variant = this.selectedVariant();
    if (this.isVariable(product) && !variant) {
      this.addMessage.set(this.t('selectVariant'));
      return;
    }

    this.adding.set(true);
    this.addMessage.set(null);
    this.cart
      .addItem({
        productId: product.id,
        quantity: this.quantity,
        productVariantId: variant?.id,
      })
      .subscribe({
        next: () => {
          this.addMessage.set(this.t('addedToCart'));
          this.adding.set(false);
        },
        error: () => {
          this.addMessage.set(this.t('addToCartFailed'));
          this.adding.set(false);
        },
      });
  }

  buyNow(product: ProductDetail): void {
    const variant = this.selectedVariant();
    if (this.isVariable(product) && !variant) {
      this.addMessage.set(this.t('selectVariant'));
      return;
    }
    if (!this.displayInStock()) return;

    this.adding.set(true);
    this.addMessage.set(null);
    this.cart
      .addItem({
        productId: product.id,
        quantity: this.quantity,
        productVariantId: variant?.id,
      })
      .subscribe({
        next: () => {
          this.adding.set(false);
          this.router.navigate(['/checkout']);
        },
        error: () => {
          this.addMessage.set(this.t('addToCartFailed'));
          this.adding.set(false);
        },
      });
  }

  openImageZoom(): void {
    this.imageZoomOpen.set(true);
  }

  closeImageZoom(): void {
    this.imageZoomOpen.set(false);
  }

  private initializeVariantSelection(product: ProductDetail): void {
    if (product.productType !== 'Variable' || product.variants.length === 0) {
      this.selectedOptionValues.set({});
      return;
    }

    const first =
      product.variants.find((variant) => variant.stockQuantity > 0) ?? product.variants[0];
    const selection: Record<number, number> = {};
    for (const option of product.options) {
      const value = option.values.find((entry) => first.optionValueIds.includes(entry.id));
      if (value) selection[option.id] = value.id;
    }
    this.selectedOptionValues.set(selection);
  }

  private resolveSelectedVariant(): ProductVariant | null {
    const product = this.product();
    if (!product || product.productType !== 'Variable') return null;

    const selectedIds = Object.values(this.selectedOptionValues());
    if (selectedIds.length !== product.options.length) return null;

    return (
      product.variants.find(
        (variant) =>
          variant.optionValueIds.length === selectedIds.length &&
          selectedIds.every((id) => variant.optionValueIds.includes(id))
      ) ?? null
    );
  }

  private truncate(value: string, maxLength: number): string {
    const text = value.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
    if (text.length <= maxLength) return text;
    return `${text.slice(0, maxLength - 1).trim()}…`;
  }

  private uniqueImageUrls(urls: string[]): string[] {
    const seen = new Set<string>();
    return urls.filter((url) => {
      if (!url) return false;
      const key = url.trim().replace(/\\/g, '/').replace(/\/{2,}/g, '/').toLowerCase();
      if (!key || seen.has(key)) return false;
      seen.add(key);
      return true;
    });
  }
}
