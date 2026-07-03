export interface FeaturedReview {
  id: number;
  userName: string;
  location?: string | null;
  rating: number;
  comment: string;
  isVerifiedPurchase: boolean;
}

export interface ProductReview {
  id: number;
  userName: string;
  rating: number;
  comment: string;
  createdAt: string;
  isVerifiedPurchase: boolean;
  helpfulCount: number;
}

export interface ProductReviewSummary {
  averageRating: number;
  reviewCount: number;
}

export interface SubmitProductReviewRequest {
  rating: number;
  comment: string;
}

export interface AdminReviewListItem {
  id: number;
  productId?: number;
  productTitle?: string;
  serviceSubscriptionId?: number;
  serviceTitle?: string;
  reviewType: string;
  userName: string;
  rating: number;
  comment: string;
  createdAt: string;
  isApproved: boolean;
  isVerifiedPurchase: boolean;
}
