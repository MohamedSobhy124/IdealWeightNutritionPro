import { DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, Input, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ProductReview, ProductReviewSummary } from '../../core/models/review.models';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { ReviewService } from '../../core/services/review.service';

import { UiEmptyStateComponent, UiFormFieldComponent, UiModalComponent, UiSectionHeaderComponent, UiSkeletonComponent } from '../../shared/ui';

const COPY = {
  basedOn: { en: 'Based on {n} reviews', ar: 'بناءً على {n} تقييم' },
  showAll: { en: 'Show all {n} reviews', ar: 'عرض كل التقييمات ({n})' },
  showLess: { en: 'Show fewer reviews', ar: 'عرض تقييمات أقل' },
  shareThoughts: { en: 'Used this product? Share your experience with other customers.', ar: 'جربت هذا المنتج؟ شارك تجربتك مع العملاء الآخرين.' },
  starsOf: { en: '{n} stars', ar: '{n} نجوم' },
  customer: { en: 'Customer', ar: 'عميل' },
} as const;

type CopyKey = keyof typeof COPY;

/** Reviews shown before "Show all". */
const INITIAL_VISIBLE = 4;

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, NgTemplateOutlet, UiEmptyStateComponent, UiFormFieldComponent, UiModalComponent, UiSectionHeaderComponent, UiSkeletonComponent],
  selector: 'app-product-reviews',
  templateUrl: './product-reviews.component.html',
  styleUrl: './product-reviews.component.css',
})
export class ProductReviewsComponent implements OnInit {
  @Input() productId?: number;
  @Input() serviceId?: number;

  private readonly reviewsApi = inject(ReviewService);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly reviews = signal<ProductReview[]>([]);
  readonly summary = signal<ProductReviewSummary | null>(null);
  readonly message = signal<string | null>(null);
  readonly isError = signal(false);
  readonly sheetOpen = signal(false);
  readonly showAll = signal(false);
  private readonly host = inject(ElementRef<HTMLElement>);

  /** Average and count: the API summary when present, else derived from the list. */
  readonly stats = computed(() => {
    const s = this.summary();
    const list = this.reviews();
    const count = s?.reviewCount ?? list.length;
    const average = s?.averageRating ?? (list.length ? list.reduce((sum, r) => sum + r.rating, 0) / list.length : 0);
    return { count, average };
  });

  /** 5→1 star breakdown of the loaded reviews. */
  readonly distribution = computed(() => {
    const list = this.reviews();
    const total = list.length || 1;
    return [5, 4, 3, 2, 1].map((stars) => {
      const count = list.filter((r) => Math.round(r.rating) === stars).length;
      return { stars, count, percent: Math.round((count / total) * 100) };
    });
  });

  readonly visibleReviews = computed(() =>
    this.showAll() ? this.reviews() : this.reviews().slice(0, INITIAL_VISIBLE),
  );
  readonly hiddenCount = computed(() => Math.max(0, this.reviews().length - INITIAL_VISIBLE));

  readonly form = this.fb.nonNullable.group({
    rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(1000)]],
  });

  t(key: UiKey): string {
    return this.locale.pick(UI[key].en, UI[key].ar);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const summary$ = this.serviceId
      ? this.reviewsApi.getServiceSummary(this.serviceId)
      : this.productId
        ? this.reviewsApi.getProductSummary(this.productId)
        : null;
    const list$ = this.serviceId
      ? this.reviewsApi.listServiceReviews(this.serviceId)
      : this.productId
        ? this.reviewsApi.listProductReviews(this.productId)
        : null;

    if (!summary$ || !list$) {
      this.loading.set(false);
      return;
    }

    summary$.subscribe({
      next: (summary) => this.summary.set(summary),
    });
    list$.subscribe({
      next: (items) => {
        this.reviews.set(items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) return;
    this.submitting.set(true);
    this.message.set(null);
    this.isError.set(false);

    const { rating, comment } = this.form.getRawValue();
    const request = { rating, comment: comment.trim() };
    const submit$ = this.serviceId
      ? this.reviewsApi.submitServiceReview(this.serviceId, request)
      : this.productId
        ? this.reviewsApi.submitProductReview(this.productId, request)
        : null;

    if (!submit$) {
      this.submitting.set(false);
      return;
    }

    submit$.subscribe({
      next: () => {
        this.message.set(this.t('reviewSubmittedPending'));
        this.form.reset({ rating: 5, comment: '' });
        this.submitting.set(false);
      },
      error: (err) => {
        this.message.set(err?.error?.errors?.[0] ?? this.t('reviewSubmitFailed'));
        this.isError.set(true);
        this.submitting.set(false);
      },
    });
  }

  setRating(rating: number): void {
    this.form.controls.rating.setValue(rating);
    this.form.controls.rating.markAsDirty();
    this.form.controls.rating.markAsTouched();
  }

  stars(rating: number): number[] {
    return Array.from({ length: rating }, (_, i) => i + 1);
  }

  c(key: CopyKey, n?: number | string): string {
    const text = this.locale.pick(COPY[key].en, COPY[key].ar);
    return n == null ? text : text.replace('{n}', String(n));
  }

  /** Some accounts use their email as display name; never show it publicly. */
  displayName(name: string | null | undefined): string {
    const value = (name ?? '').trim();
    if (!value) return this.c('customer');
    if (!value.includes('@')) return value;
    const first = value.split('@')[0].replace(/[._\-+0-9]+/g, ' ').trim().split(/\s+/)[0] ?? '';
    return first ? first.charAt(0).toUpperCase() + first.slice(1) : this.c('customer');
  }

  initial(name: string | null | undefined): string {
    return this.displayName(name).charAt(0).toUpperCase();
  }

  /** Stable avatar tint per reviewer. */
  tone(name: string | null | undefined): number {
    const value = this.displayName(name);
    let hash = 0;
    for (let i = 0; i < value.length; i++) hash = (hash + value.charCodeAt(i)) % 4;
    return hash;
  }

  reviewDate(iso: string | null | undefined): string {
    if (!iso) return '';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return '';
    try {
      return new Intl.DateTimeFormat(this.locale.isArabic() ? 'ar-AE' : 'en-AE', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
      }).format(date);
    } catch {
      return date.toLocaleDateString();
    }
  }

  /** Desktop: jump to the inline form. Mobile: the form lives in the reviews sheet. */
  goToWriteReview(): void {
    const target = (this.host.nativeElement as HTMLElement).querySelector<HTMLElement>('#write-review');
    if (target && target.offsetParent !== null) {
      target.scrollIntoView({ behavior: 'smooth', block: 'center' });
      target.querySelector<HTMLElement>('textarea, a')?.focus({ preventScroll: true });
    } else {
      this.sheetOpen.set(true);
    }
  }
}
