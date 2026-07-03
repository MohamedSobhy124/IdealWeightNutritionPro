import { isPlatformBrowser } from '@angular/common';
import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnDestroy, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
  Brand,
  Category,
  CategoryProductSection,
  ProductListItem,
} from '../../core/models/catalogue.models';
import { ComboOfferSummary } from '../../core/models/combo-offer.models';
import { FlashSaleProductPrice, FlashSaleSummary } from '../../core/models/flash-sale.models';
import { ServiceSubscriptionSummary } from '../../core/models/service.models';
import { CatalogueService } from '../../core/services/catalogue.service';
import { CartService } from '../../core/services/cart.service';
import { ComboOfferService } from '../../core/services/combo-offer.service';
import { FlashSaleService } from '../../core/services/flash-sale.service';
import { VideoBanner, VideoBannerService } from '../../core/services/video-banner.service';
import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';
import { LocaleService } from '../../core/services/locale.service';
import { AuthService } from '../../core/services/auth.service';
import { WishlistService } from '../../core/services/wishlist.service';
import { ReviewService } from '../../core/services/review.service';
import {
  productQuickAddButtonLabel,
  quickAddProduct,
  shouldNavigateToProductForQuickAdd,
} from '../../core/utils/product-quick-add';
import { FeaturedReview } from '../../core/models/review.models';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { ProductCardPricing, resolveProductCardPricing } from '../../core/utils/product-card-pricing';
import { FlashSaleCountdownComponent } from '../promotions/flash-sale-countdown.component';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { SeoService } from '../../core/services/seo.service';
import { environment } from '../../../environments/environment';
import {
  UiBadgeComponent,
  UiProductCardComponent,
  UiSkeletonComponent,
  UiSectionHeaderComponent,
  UiCategoryCardComponent,
  UiBrandCardComponent,
  UiGoalCardComponent,
  UiReviewCardComponent,
  UiTrustStripComponent,
} from '../../shared/ui';
import { RevealOnScrollDirective } from '../../shared/directives/reveal-on-scroll.directive';

type HeroSlide =
  | {
      type: 'video';
      title: string;
      lead: string;
      videoUrl: string;
      posterUrl?: string | null;
      primaryLink: string;
      secondaryLink: string;
      secondaryLabelKey: UiKey;
    }
  | {
      type: 'image';
      title: string;
      lead: string;
      imageUrl: string;
      primaryLink: string;
      secondaryLink: string;
      secondaryLabelKey: UiKey;
    }
  | {
      type: 'flash';
      title: string;
      lead: string;
      link: string;
      imageUrl?: string | null;
      primaryLink: string;
      secondaryLink: string;
      secondaryLabelKey: UiKey;
    }
  | {
      type: 'combo';
      title: string;
      lead: string;
      link: string;
      imageUrl?: string | null;
      primaryLink: string;
      secondaryLink: string;
      secondaryLabelKey: UiKey;
    }
  | {
      type: 'default';
      title: string;
      lead: string;
      primaryLink: string;
      secondaryLink: string;
      secondaryLabelKey: UiKey;
    };

@Component({
  standalone: true,
  imports: [
    RouterLink,
    DecimalPipe,
    IwnCurrencyPipe,
    FlashSaleCountdownComponent,
    UiBadgeComponent,
    UiProductCardComponent,
    UiSkeletonComponent,
    UiSectionHeaderComponent,
    UiCategoryCardComponent,
    UiBrandCardComponent,
    UiGoalCardComponent,
    UiReviewCardComponent,
    UiTrustStripComponent,
    RevealOnScrollDirective,
  ],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.css',
})
export class HomePageComponent implements OnInit, OnDestroy {
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);
  readonly auth = inject(AuthService);
  readonly wishlist = inject(WishlistService);
  private readonly cart = inject(CartService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly flashSalesApi = inject(FlashSaleService);
  private readonly combosApi = inject(ComboOfferService);
  private readonly videoBannerApi = inject(VideoBannerService);
  private readonly servicesApi = inject(ServiceSubscriptionService);
  private readonly reviewsApi = inject(ReviewService);
  private readonly seo = inject(SeoService);

  readonly flashSales = signal<FlashSaleSummary[]>([]);
  readonly combos = signal<ComboOfferSummary[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly brands = signal<Brand[]>([]);
  readonly bestSellers = signal<ProductListItem[]>([]);
  readonly newArrivals = signal<ProductListItem[]>([]);
  readonly discountedProducts = signal<ProductListItem[]>([]);
  readonly services = signal<ServiceSubscriptionSummary[]>([]);
  readonly categorySections = signal<CategoryProductSection[]>([]);
  readonly selectedCategoryId = signal<number | null>(null);
  readonly videoBanner = signal<VideoBanner | null>(null);
  readonly flashPrices = signal<FlashSaleProductPrice[]>([]);
  readonly loadingPromos = signal(true);
  readonly loadingMerch = signal(true);
  readonly loadingProducts = signal(true);
  readonly loadingReviews = signal(true);
  readonly featuredReviews = signal<FeaturedReview[]>([]);
  readonly wishlistBusyId = signal<number | null>(null);
  readonly quickAddBusyId = signal<number | null>(null);
  readonly heroIndex = signal(0);
  readonly heroPaused = signal(false);
  readonly heroTransform = computed(() => {
    const offset = this.heroIndex() * 100;
    return this.locale.isArabic() ? `translateX(${offset}%)` : `translateX(-${offset}%)`;
  });
  private heroTimer: ReturnType<typeof setInterval> | null = null;

  readonly trustItems = computed(() => [
    { icon: 'verified', label: this.t('trustedOriginals') },
    { icon: 'local_shipping', label: this.t('sameDayDelivery') },
    { icon: 'support_agent', label: this.t('expertSupportShort') },
    { icon: 'lock', label: this.t('securePayments') },
  ]);

  readonly healthGoals = computed(() => [
    {
      title: this.t('goalWeightLoss'),
      subtitle: this.t('goalWeightLossLead'),
      icon: 'monitor_weight',
      queryParams: { search: 'weight' },
    },
    {
      title: this.t('goalMuscleGain'),
      subtitle: this.t('goalMuscleGainLead'),
      icon: 'fitness_center',
      queryParams: { search: 'protein' },
    },
    {
      title: this.t('goalWellness'),
      subtitle: this.t('goalWellnessLead'),
      icon: 'spa',
      queryParams: { search: 'vitamin' },
    },
    {
      title: this.t('goalEnergy'),
      subtitle: this.t('goalEnergyLead'),
      icon: 'bolt',
      queryParams: { search: 'energy' },
    },
  ]);

  readonly staticCustomerReviews = computed(() => [
    { id: 1, quote: this.t('reviewQuote1'), author: this.t('reviewAuthor1'), location: this.t('reviewLocation1'), rating: 5 },
    { id: 2, quote: this.t('reviewQuote2'), author: this.t('reviewAuthor2'), location: this.t('reviewLocation2'), rating: 5 },
    { id: 3, quote: this.t('reviewQuote3'), author: this.t('reviewAuthor3'), location: this.t('reviewLocation3'), rating: 5 },
  ]);

  readonly displayReviews = computed(() => {
    const apiReviews = this.featuredReviews();
    if (apiReviews.length > 0) {
      return apiReviews.map((review) => ({
        id: review.id,
        quote: review.comment,
        author: review.userName,
        location: review.location ?? null,
        rating: review.rating,
      }));
    }
    return this.staticCustomerReviews();
  });

  readonly activeCategoryProducts = computed(() => {
    const sections = this.categorySections();
    const selectedId = this.selectedCategoryId();
    const section =
      sections.find((s) => s.category.id === selectedId) ?? sections[0] ?? null;
    return section?.products ?? [];
  });

  readonly activeCategoryName = computed(() => {
    const sections = this.categorySections();
    const selectedId = this.selectedCategoryId();
    const section =
      sections.find((s) => s.category.id === selectedId) ?? sections[0] ?? null;
    if (!section) return '';
    return this.locale.pick(section.category.name, section.category.nameAr);
  });

  readonly heroSlides = computed((): HeroSlide[] => {
    const slides: HeroSlide[] = [];
    const banner = this.videoBanner();

    if (banner?.hasVideo && banner.videoUrl) {
      slides.push({
        type: 'video',
        title: this.t('heroTitle'),
        lead: this.t('heroLead'),
        videoUrl: this.catalogue.resolveImageUrl(banner.videoUrl),
        posterUrl: banner.posterUrl ? this.catalogue.resolveImageUrl(banner.posterUrl) : null,
        primaryLink: '/shop',
        secondaryLink: '/flash-sales',
        secondaryLabelKey: 'flashSales',
      });
    } else if (banner?.hasPoster && banner.posterUrl) {
      slides.push({
        type: 'image',
        title: this.t('heroTitle'),
        lead: this.t('heroLead'),
        imageUrl: this.catalogue.resolveImageUrl(banner.posterUrl),
        primaryLink: '/shop',
        secondaryLink: '/offers',
        secondaryLabelKey: 'navOffers',
      });
    }

    for (const sale of this.flashSales().slice(0, 2)) {
      slides.push({
        type: 'flash',
        title: this.flashName(sale),
        lead: this.t('saleEndsIn'),
        link: `/flash-sales/${sale.id}`,
        imageUrl: sale.imageUrl,
        primaryLink: `/flash-sales/${sale.id}`,
        secondaryLink: '/shop',
        secondaryLabelKey: 'shopNow',
      });
    }

    for (const combo of this.combos().slice(0, 1)) {
      slides.push({
        type: 'combo',
        title: this.comboName(combo),
        lead: this.t('comboOffers'),
        link: `/combos/${combo.id}`,
        imageUrl: combo.imageUrl,
        primaryLink: `/combos/${combo.id}`,
        secondaryLink: '/combos',
        secondaryLabelKey: 'viewAll',
      });
    }

    if (slides.length === 0) {
      slides.push({
        type: 'default',
        title: this.t('heroTitle'),
        lead: this.t('heroLead'),
        primaryLink: '/shop',
        secondaryLink: '/flash-sales',
        secondaryLabelKey: 'flashSales',
      });
    }

    return slides;
  });

  private static readonly homeSectionLimit = 4;
  private static readonly productSectionSize = 8;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  productTitle(p: ProductListItem): string {
    return this.locale.pick(p.title, p.titleAr);
  }

  categoryName(c: Category): string {
    return this.locale.pick(c.name, c.nameAr);
  }

  brandName(b: Brand): string {
    return this.locale.pick(b.name, b.nameAr);
  }

  flashName(sale: FlashSaleSummary): string {
    return this.locale.pick(sale.name, sale.nameAr);
  }

  comboName(combo: ComboOfferSummary): string {
    return this.locale.pick(combo.name, combo.nameAr);
  }

  serviceTitle(service: ServiceSubscriptionSummary): string {
    return this.locale.pick(service.title, service.titleAr);
  }

  selectCategory(categoryId: number): void {
    this.selectedCategoryId.set(categoryId);
  }

  cardPricing(product: ProductListItem): ProductCardPricing {
    return resolveProductCardPricing(product, this.flashPrices());
  }

  isInWishlist(productId: number): boolean {
    return this.wishlist.isInWishlist(productId);
  }

  toggleWishlist(_event: Event, productId: number): void {
    if (!this.auth.isAuthenticated()) return;
    this.wishlistBusyId.set(productId);
    this.wishlist.toggle(productId).subscribe({
      next: () => this.wishlistBusyId.set(null),
      error: () => this.wishlistBusyId.set(null),
    });
  }

  quickAddLabel(product: ProductListItem): string {
    return productQuickAddButtonLabel(product, {
      addToCart: this.t('addToCart'),
      chooseVariant: this.t('chooseVariant'),
    });
  }

  quickAdd(product: ProductListItem): void {
    if (!shouldNavigateToProductForQuickAdd(product)) {
      this.quickAddBusyId.set(product.id);
    }

    quickAddProduct(this.router, this.cart, product, {
      onAdded: () => this.quickAddBusyId.set(null),
      onFailed: () => this.quickAddBusyId.set(null),
    });
  }

  selectHero(index: number): void {
    this.heroIndex.set(index);
  }

  prevHero(): void {
    const length = this.heroSlides().length;
    if (!length) return;
    this.heroIndex.set((this.heroIndex() - 1 + length) % length);
  }

  nextHero(): void {
    const length = this.heroSlides().length;
    if (!length) return;
    this.heroIndex.set((this.heroIndex() + 1) % length);
  }

  private absoluteUrl(path: string): string {
    const base = environment.siteUrl?.replace(/\/$/, '') || 'https://idealweightnutrition.ae';
    return `${base}${path.startsWith('/') ? path : `/${path}`}`;
  }

  private emitHomeStructuredData(): void {
    const organization = {
      '@context': 'https://schema.org',
      '@type': 'Organization',
      name: this.locale.pick(environment.siteName, environment.siteNameAr ?? environment.siteName),
      url: this.absoluteUrl('/'),
      logo: this.absoluteUrl('/assets/logo.svg'),
      contactPoint: [{ '@type': 'ContactPoint', contactType: 'customer support', availableLanguage: ['en', 'ar'] }],
    };

    const website = {
      '@context': 'https://schema.org',
      '@type': 'WebSite',
      name: this.locale.pick(environment.siteName, environment.siteNameAr ?? environment.siteName),
      url: this.absoluteUrl('/'),
      potentialAction: {
        '@type': 'SearchAction',
        target: `${this.absoluteUrl('/shop')}?search={query}`,
        'query-input': 'required name=query',
      },
    };

    this.seo.setStructuredData('home-organization', organization);
    this.seo.setStructuredData('home-website', website);
  }

  private emitFeaturedProductsStructuredData(): void {
    const entries = this.bestSellers().slice(0, 8).map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      url: this.absoluteUrl(`/product/${item.slug}`),
      name: this.productTitle(item),
      image: this.catalogue.resolveImageUrl(item.imageUrl),
    }));

    this.seo.setStructuredData('home-featured-products', {
      '@context': 'https://schema.org',
      '@type': 'ItemList',
      name: this.t('bestSellers'),
      itemListElement: entries,
    });
  }

  private dedupeProductRails(
    discounted: ProductListItem[],
    best: ProductListItem[],
    newest: ProductListItem[],
  ): { discounted: ProductListItem[]; bestSellers: ProductListItem[]; newArrivals: ProductListItem[] } {
    const seen = new Set<number>();
    for (const product of discounted) {
      seen.add(product.id);
    }
    const bestSellers = best.filter((product) => !seen.has(product.id));
    for (const product of bestSellers) {
      seen.add(product.id);
    }
    const newArrivals = newest.filter((product) => !seen.has(product.id));
    return { discounted, bestSellers, newArrivals };
  }

  ngOnInit(): void {
    this.emitHomeStructuredData();

    if (this.auth.isAuthenticated()) {
      this.wishlist.loadProductIds().subscribe();
    }

    forkJoin({
      flash: this.flashSalesApi.listActive().pipe(catchError(() => of([] as FlashSaleSummary[]))),
      flashPrices: this.flashSalesApi.listProductPrices().pipe(catchError(() => of([] as FlashSaleProductPrice[]))),
      combos: this.combosApi.listActive().pipe(catchError(() => of([] as ComboOfferSummary[]))),
      banner: this.videoBannerApi.getPublicBanner().pipe(catchError(() => of(null))),
      categories: this.catalogue.listCategories().pipe(catchError(() => of([] as Category[]))),
      brands: this.catalogue.listBrands().pipe(catchError(() => of([] as Brand[]))),
      discounted: this.catalogue
        .listDiscountedProducts(HomePageComponent.productSectionSize)
        .pipe(catchError(() => of([] as ProductListItem[]))),
      categorySections: this.catalogue
        .getCategoryProductSections(6, 4)
        .pipe(catchError(() => of([] as CategoryProductSection[]))),
      services: this.servicesApi
        .listServices()
        .pipe(catchError(() => of([] as ServiceSubscriptionSummary[]))),
      bestSellers: this.catalogue
        .listProducts({
          page: 1,
          pageSize: HomePageComponent.productSectionSize,
          sortBy: 'trending',
          availability: 'instock',
        })
        .pipe(catchError(() => of({ items: [] as ProductListItem[], totalCount: 0, page: 1, pageSize: 8 }))),
      newArrivals: this.catalogue
        .listProducts({
          page: 1,
          pageSize: HomePageComponent.productSectionSize,
          sortBy: 'new',
          availability: 'instock',
        })
        .pipe(catchError(() => of({ items: [] as ProductListItem[], totalCount: 0, page: 1, pageSize: 8 }))),
      featuredReviews: this.reviewsApi.listFeatured(6).pipe(catchError(() => of([] as FeaturedReview[]))),
    }).subscribe({
      next: ({
        flash,
        flashPrices,
        combos,
        banner,
        categories,
        brands,
        discounted,
        categorySections,
        services,
        bestSellers,
        newArrivals,
        featuredReviews,
      }) => {
        this.flashSales.set(flash.slice(0, HomePageComponent.homeSectionLimit));
        this.flashPrices.set(flashPrices);
        this.combos.set(combos.slice(0, HomePageComponent.homeSectionLimit));
        this.videoBanner.set(banner);
        this.categories.set(categories.slice(0, HomePageComponent.homeSectionLimit * 2));
        this.brands.set(brands.slice(0, HomePageComponent.homeSectionLimit * 2));
        const rails = this.dedupeProductRails(discounted, bestSellers.items, newArrivals.items);
        this.discountedProducts.set(rails.discounted);
        this.categorySections.set(categorySections);
        if (categorySections.length > 0) {
          this.selectedCategoryId.set(categorySections[0].category.id);
        }
        this.services.set(services.slice(0, HomePageComponent.homeSectionLimit));
        this.bestSellers.set(rails.bestSellers);
        this.emitFeaturedProductsStructuredData();
        this.newArrivals.set(rails.newArrivals);
        this.featuredReviews.set(featuredReviews);
        this.loadingPromos.set(false);
        this.loadingMerch.set(false);
        this.loadingProducts.set(false);
        this.loadingReviews.set(false);
      },
      error: () => {
        this.loadingPromos.set(false);
        this.loadingMerch.set(false);
        this.loadingProducts.set(false);
        this.loadingReviews.set(false);
      },
    });

    if (isPlatformBrowser(this.platformId)) {
      this.heroTimer = setInterval(() => {
        if (!this.heroPaused()) {
          this.nextHero();
        }
      }, 5000);
    }
  }

  ngOnDestroy(): void {
    this.seo.clearStructuredData('home-organization');
    this.seo.clearStructuredData('home-website');
    this.seo.clearStructuredData('home-featured-products');

    if (this.heroTimer) {
      clearInterval(this.heroTimer);
      this.heroTimer = null;
    }
  }
}
