import { DatePipe, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, OnInit, PLATFORM_ID, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CatalogueService } from '../../core/services/catalogue.service';
import { LocaleService } from '../../core/services/locale.service';
import { ServiceSubscriptionService } from '../../core/services/service-subscription.service';
import { IwnCurrencyPipe } from '../../core/pipes/iwn-currency.pipe';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ServiceSubscriptionDetail } from '../../core/models/service.models';
import { serviceTypeLabel } from '../../core/utils/status-label';
import { ProductReviewsComponent } from '../catalogue/product-reviews.component';
import { UiBadgeComponent, UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [RouterLink, DatePipe, ProductReviewsComponent, IwnCurrencyPipe, UiBadgeComponent, UiCardComponent, UiEmptyStateComponent, UiPageHeaderComponent, UiSkeletonComponent],
  templateUrl: './service-detail.component.html',
  styleUrl: './service-detail.component.css',
})
export class ServiceDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly servicesApi = inject(ServiceSubscriptionService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly gallery = viewChild<ElementRef<HTMLElement>>('gallery');
  readonly catalogue = inject(CatalogueService);
  readonly locale = inject(LocaleService);

  readonly service = signal<ServiceSubscriptionDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly activeImage = signal<string | null>(null);
  private syncingGallery = false;

  readonly title = computed(() => {
    const s = this.service();
    return s ? this.locale.pick(s.title, s.titleAr) : '';
  });

  readonly description = computed(() => {
    const s = this.service();
    return s ? this.locale.pick(s.description ?? '', s.descriptionAr) : '';
  });

  readonly displayPrice = computed(() => {
    const s = this.service();
    if (!s) return 0;
    return s.salePrice != null && s.hasActiveOffer ? s.salePrice : s.price;
  });

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  typeLabel(type: string | null | undefined): string {
    return serviceTypeLabel(type, (key) => this.t(key));
  }

  chooseImage(url: string): void {
    this.activeImage.set(url);
    this.scrollGalleryTo(url);
  }

  onGalleryScroll(event: Event): void {
    if (this.syncingGallery) return;
    const scroller = event.target as HTMLElement;
    const slides = scroller.querySelectorAll<HTMLElement>('[data-gallery-index]');
    if (!slides.length) return;
    const center = scroller.getBoundingClientRect().left + scroller.clientWidth / 2;
    let best = 0;
    let bestDistance = Number.POSITIVE_INFINITY;
    slides.forEach((slide, index) => {
      const box = slide.getBoundingClientRect();
      const distance = Math.abs(box.left + box.width / 2 - center);
      if (distance < bestDistance) {
        bestDistance = distance;
        best = index;
      }
    });
    const url = this.service()?.imageUrls[best];
    if (url && url !== this.activeImage()) this.activeImage.set(url);
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = Number(params.get('id'));
      if (!id) {
        this.error.set(this.t('serviceNotFound'));
        this.loading.set(false);
        return;
      }

      this.loading.set(true);
      this.error.set(null);
      if (!isPlatformBrowser(this.platformId)) return;
      this.servicesApi.getService(id).subscribe({
        next: (service) => {
          this.service.set(service);
          this.activeImage.set(service.imageUrls[0] ?? null);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(this.t('serviceNotFound'));
          this.loading.set(false);
        },
      });
    });
  }

  private scrollGalleryTo(url: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const scroller = this.gallery()?.nativeElement;
    const images = this.service()?.imageUrls ?? [];
    if (!scroller) return;
    const index = images.indexOf(url);
    const slide = scroller.querySelectorAll<HTMLElement>('[data-gallery-index]')[index];
    if (!slide) return;
    const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    this.syncingGallery = true;
    slide.scrollIntoView({ behavior: reduce ? 'auto' : 'smooth', block: 'nearest', inline: 'center' });
    window.setTimeout(() => {
      this.syncingGallery = false;
    }, reduce ? 0 : 450);
  }
}
