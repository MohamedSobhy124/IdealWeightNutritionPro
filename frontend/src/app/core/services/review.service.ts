import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminReviewListItem,
  FeaturedReview,
  ProductReview,
  ProductReviewSummary,
  SubmitProductReviewRequest,
} from '../models/review.models';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  listFeatured(count = 6) {
    return this.http.get<FeaturedReview[]>(`${this.baseUrl}/reviews/featured`, {
      params: { count },
    });
  }

  listProductReviews(productId: number) {
    return this.http.get<ProductReview[]>(`${this.baseUrl}/products/${productId}/reviews`);
  }

  getProductSummary(productId: number) {
    return this.http.get<ProductReviewSummary>(`${this.baseUrl}/products/${productId}/reviews/summary`);
  }

  submitProductReview(productId: number, request: SubmitProductReviewRequest) {
    return this.http.post<ProductReview>(`${this.baseUrl}/products/${productId}/reviews`, request, {
      withCredentials: true,
    });
  }

  listServiceReviews(serviceId: number) {
    return this.http.get<ProductReview[]>(`${this.baseUrl}/services/${serviceId}/reviews`);
  }

  getServiceSummary(serviceId: number) {
    return this.http.get<ProductReviewSummary>(`${this.baseUrl}/services/${serviceId}/reviews/summary`);
  }

  submitServiceReview(serviceId: number, request: SubmitProductReviewRequest) {
    return this.http.post<ProductReview>(`${this.baseUrl}/services/${serviceId}/reviews`, request, {
      withCredentials: true,
    });
  }

  listAdmin(status = 'all') {
    return this.http.get<AdminReviewListItem[]>(`${this.baseUrl}/admin/reviews`, {
      params: { status },
      withCredentials: true,
    });
  }

  toggleApproval(reviewId: number) {
    return this.http.post(`${this.baseUrl}/admin/reviews/${reviewId}/toggle-approval`, {}, {
      withCredentials: true,
    });
  }

  deleteReview(reviewId: number) {
    return this.http.delete(`${this.baseUrl}/admin/reviews/${reviewId}`, { withCredentials: true });
  }
}
