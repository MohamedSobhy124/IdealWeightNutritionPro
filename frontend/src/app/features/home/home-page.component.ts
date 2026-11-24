import { isPlatformBrowser } from '@angular/common';
import { DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnDestroy, OnInit, PLATFORM_ID, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import {
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
  UiReviewCardComponent,
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
      imageUrl?: string | null;
      primaryLink: string;
      secondaryLink: string;
      secondaryLabelKey: UiKey;
    };

type DealsTab = 'flash' | 'sale' | 'combos';
type ProductsTab = 'best' | 'new';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    RouterLink,
    DecimalPipe,
    NgTemplateOutlet,
    IwnCurrencyPipe,
    FlashSaleCountdownComponent,
    UiBadgeComponent,
    UiProductCardComponent,
    UiSkeletonComponent,
    UiSectionHeaderComponent,
        UiReviewCardComponent,
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
  static readonly heroIntervalMs = 6000;
  readonly heroIntervalMs = HomePageComponent.heroIntervalMs;

  /** Deals band: one tinted section with tabs for whichever promo data exists. */
  readonly dealsTabChoice = signal<DealsTab | null>(null);
  readonly dealsTabs = computed(() => {
    const tabs: { id: DealsTab; label: string; icon: string }[] = [];
    if (this.flashSales().length > 0) tabs.push({ id: 'flash', label: this.t('flashSales'), icon: 'bolt' });
    if (this.discountedProducts().length > 0) tabs.push({ id: 'sale', label: this.t('homeDealsOnSale'), icon: 'sell' });
    if (this.combos().length > 0) tabs.push({ id: 'combos', label: this.t('comboOffers'), icon: 'inventory_2' });
    return tabs;
  });
  readonly activeDealsTab = computed<DealsTab | null>(() => {
    const tabs = this.dealsTabs();
    const chosen = this.dealsTabChoice();
    return tabs.find((tab) => tab.id === chosen)?.id ?? tabs[0]?.id ?? null;
  });
  readonly dealsViewAllLink = computed(() => {
    switch (this.activeDealsTab()) {
      case 'flash':
        return '/flash-sales';
      case 'combos':
        return '/combos';
      default:
        return '/offers';
    }
  });

  /** Best sellers / new arrivals share one tabbed rail. */
  readonly productsTabChoice = signal<ProductsTab | null>(null);
  readonly productsTabs = computed(() => {
    const tabs: { id: ProductsTab; label: string }[] = [];
    if (this.bestSellers().length > 0) tabs.push({ id: 'best', label: this.t('bestSellers') });
    if (this.newArrivals().length > 0) tabs.push({ id: 'new', label: this.t('newArrivals') });
    return tabs;
  });
  readonly activeProductsTab = computed<ProductsTab | null>(() => {
    const tabs = this.productsTabs();
    const chosen = this.productsTabChoice();
    return tabs.find((tab) => tab.id === chosen)?.id ?? tabs[0]?.id ?? null;
  });
  readonly activeProducts = computed(() =>
    this.activeProductsTab() === 'new' ? this.newArrivals() : this.bestSellers(),
  );

  /** Opener tiles: up to 5 categories plus an "All products" tile. */
  readonly introCategories = computed(() => this.categories().slice(0, 5));

  /** Only summarise real reviews; the static fallbacks must not pose as data. */
  readonly reviewSummary = computed(() => {
    const reviews = this.featuredReviews();
    if (reviews.length === 0) return null;
    const average = reviews.reduce((sum, review) => sum + review.rating, 0) / reviews.length;
    return { average, count: reviews.length };
  });

  readonly aboutPhotos = [
    'https://idealweightnutrition.ae/Images/Products/AboutUs00.jpeg',
    'https://idealweightnutrition.ae/Images/Products/AboutUs0.jpeg',
    'https://idealweightnutrition.ae/Images/Products/AboutUs1.jpeg',
    'https://idealweightnutrition.ae/Images/Products/AboutUs2.jpeg',
    'https://idealweightnutrition.ae/Images/Products/AboutUs3.jpeg',
  ];
  readonly aboutPhoto = signal(0);
  readonly welcomePoints = computed(() => [
    this.t('homeWelcomePoint1'),
    this.t('homeWelcomePoint2'),
    this.t('homeWelcomePoint3'),
  ]);
  readonly branches = computed(() => [
    { name: this.t('branchShamkha'), address: this.t('branchShamkhaAddress'), map: 'https://maps.app.goo.gl/9SU1q2p69vfRTkMn8' },
    { name: this.t('branchWathba'), address: this.t('branchWathbaAddress'), map: 'https://maps.app.goo.gl/12J3mbtkGsk6FKLp9' },
  ]);
  readonly whyCards = computed(() => [
    { icon: 'verified', title: this.t('whyQualityTitle'), lead: this.t('whyQualityLead') },
    { icon: 'support_agent', title: this.t('whyGuidanceTitle'), lead: this.t('whyGuidanceLead') },
    { icon: 'local_shipping', title: this.t('whyDeliveryTitle'), lead: this.t('whyDeliveryLead') },
    { icon: 'health_and_safety', title: this.t('whyGuaranteeTitle'), lead: this.t('whyGuaranteeLead') },
  ]);
  readonly aboutPoints = computed(() => [
    { icon: 'workspace_premium', label: this.t('aboutPointQuality') },
    { icon: 'favorite', label: this.t('aboutPointService') },
    { icon: 'local_shipping', label: this.t('aboutPointShipping') },
    { icon: 'storefront', label: this.t('aboutPointBranches') },
  ]);
  readonly homeStats = computed(() => [
    { value: '5,000+', label: this.t('homeStatProducts') },
    { value: '20,000+', label: this.t('homeStatCustomers') },
    { value: '50+', label: this.t('homeStatBrands') },
    { value: '100%', label: this.t('homeStatOriginal') },
  ]);
  readonly seoPoints = computed(() => [
    { icon: 'verified', title: this.t('homeSeoOriginalTitle'), lead: this.t('homeSeoOriginalLead') },
    { icon: 'local_shipping', title: this.t('homeSeoDeliveryTitle'), lead: this.t('homeSeoDeliveryLead') },
    { icon: 'sell', title: this.t('homeSeoPriceTitle'), lead: this.t('homeSeoPriceLead') },
    { icon: 'forum', title: this.t('homeSeoAdviceTitle'), lead: this.t('homeSeoAdviceLead') },
  ]);
  readonly supplementRange = computed(() => [
    { icon: 'fitness_center', title: this.t('homeRangeProtein'), lead: this.t('homeRangeProteinLead') },
    { icon: 'trending_up', title: this.t('homeRangeMass'), lead: this.t('homeRangeMassLead') },
    { icon: 'local_fire_department', title: this.t('homeRangeCut'), lead: this.t('homeRangeCutLead') },
    { icon: 'medication', title: this.t('homeRangeVitamins'), lead: this.t('homeRangeVitaminsLead') },
    { icon: 'bolt', title: this.t('homeRangePerformance'), lead: this.t('homeRangePerformanceLead') },
    { icon: 'favorite', title: this.t('homeRangeWellness'), lead: this.t('homeRangeWellnessLead') },
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
        author: this.publicAuthorName(review.userName),
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

  readonly nextFlashEndDate = computed(() => {
    const now = Date.now();
    return (
      this.flashSales()
        .map((sale) => sale.endDate)
        .filter((endDate) => new Date(endDate).getTime() > now)
        .sort()[0] ?? null
    );
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
        imageUrl: HomePageComponent.heroFallbackPhoto,
        primaryLink: '/shop',
        secondaryLink: '/flash-sales',
        secondaryLabelKey: 'flashSales',
      });
    }

    return slides;
  });

  /** Store photo shown in the hero until an admin video/poster banner is uploaded. */
  private static readonly heroFallbackPhoto = 'https://idealweightnutrition.ae/Images/Products/AboutUs2.jpeg';
  private static readonly homeSectionLimit = 4;
  private static readonly productSectionSize = 8;

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  /** Picks a Material icon from the category name (EN or AR) for tiles without an image. */
  categoryIcon(category: Category): string {
    const name = `${category.name ?? ''} ${category.nameAr ?? ''}`.toLowerCase();
    const rules: [RegExp, string][] = [
      [/weight|وزن|diet|رجيم|slim|تنحيف/, 'monitor_weight'],
      [/sport|رياض|protein|بروتين|muscle|عضل|gym/, 'fitness_center'],
      [/food|غذ|اغذ|أغذ|snack|وجب/, 'restaurant'],
      [/vitamin|فيتامين|supplement|مكمل/, 'medication'],
      [/health|صح|wellness|عافي/, 'spa'],
      [/energy|طاق/, 'bolt'],
      [/beauty|جمال|skin|بشر/, 'face_retouching_natural'],
      [/drink|مشروب|juice|عصير/, 'local_cafe'],
    ];
    return rules.find(([pattern]) => pattern.test(name))?.[1] ?? 'eco';
  }

  /**
   * Picks up to `limit` reviews round-robin by reviewer (keeping the API's
   * quality order), so the same name repeats only when there aren't enough
   * different customers.
   */
  private diverseReviews(reviews: FeaturedReview[], limit: number): FeaturedReview[] {
    const groups = new Map<string, FeaturedReview[]>();
    for (const review of reviews) {
      const key = (review.userName ?? '').trim().toLowerCase() || `#${review.id}`;
      const list = groups.get(key);
      if (list) list.push(review);
      else groups.set(key, [review]);
    }
    const queues = [...groups.values()];
    const picked: FeaturedReview[] = [];
    while (picked.length < limit && queues.some((q) => q.length > 0)) {
      for (const queue of queues) {
        if (picked.length >= limit) break;
        const next = queue.shift();
        if (next) picked.push(next);
      }
    }
    return picked;
  }

  /** Some accounts use their email as the display name; never show it publicly. */
  private publicAuthorName(name: string | null | undefined): string {
    const value = (name ?? '').trim();
    if (!value.includes('@')) return value || this.t('homeVerifiedCustomer');
    const local = value.split('@')[0].replace(/[._\-+0-9]+/g, ' ').trim();
    const first = local.split(/\s+/)[0] ?? '';
    return first ? first.charAt(0).toUpperCase() + first.slice(1) : this.t('homeVerifiedCustomer');
  }

  reviewSummaryText(summary: { average: number; count: number }): string {
    return this.t('homeReviewsSummary')
      .replace('{avg}', summary.average.toFixed(1))
      .replace('{n}', String(summary.count));
  }

  slideLabel(index: number): string {
    return this.t('slideNumber').replace('{n}', String(index + 1));
  }

  productTitle(p: ProductListItem): string {
    return this.locale.pick(p.title, p.titleAr);
  }

  categoryName(c: Category): string {
    return this.locale.pick(c.name, c.nameAr);
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

  serviceLead(service: ServiceSubscriptionSummary): string {
    return this.locale.pick(service.description ?? '', service.descriptionAr);
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
    this.restartHeroTimer();
  }

  prevHero(): void {
    const length = this.heroSlides().length;
    if (!length) return;
    this.heroIndex.set((this.heroIndex() - 1 + length) % length);
    this.restartHeroTimer();
  }

  nextHero(manual = true): void {
    const length = this.heroSlides().length;
    if (!length) return;
    this.heroIndex.set((this.heroIndex() + 1) % length);
    if (manual) this.restartHeroTimer();
  }

  /** Keeps the progress pill and the auto-advance in step after a manual change. */
  private restartHeroTimer(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    if (this.heroTimer) clearInterval(this.heroTimer);
    this.heroTimer = setInterval(() => {
      if (!this.heroPaused() && this.heroSlides().length > 1) {
        this.nextHero(false);
      }
    }, HomePageComponent.heroIntervalMs);
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
      // Ask for the max pool (12) so we can pick 6 with as many different reviewers as possible.
      featuredReviews: this.reviewsApi.listFeatured(12).pipe(catchError(() => of([] as FeaturedReview[]))),
    }).subscribe({
      next: ({
        flash,
        flashPrices,
        combos,
        banner,
        categories,
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
        this.featuredReviews.set(this.diverseReviews(featuredReviews, 6));
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

    this.restartHeroTimer();
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
