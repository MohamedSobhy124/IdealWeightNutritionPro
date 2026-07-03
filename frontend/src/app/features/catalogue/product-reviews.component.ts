import { DecimalPipe } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { UI, UiKey } from '../../core/i18n/ui-text';
import { ProductReview, ProductReviewSummary } from '../../core/models/review.models';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { ReviewService } from '../../core/services/review.service';

import { UiCardComponent, UiEmptyStateComponent, UiFormFieldComponent, UiReviewCardComponent, UiSkeletonComponent } from '../../shared/ui';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, UiCardComponent, UiEmptyStateComponent, UiFormFieldComponent, UiReviewCardComponent, UiSkeletonComponent],
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
}
