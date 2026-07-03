export type DiscountType = 'Percentage' | 'FixedAmount' | 1 | 2;

export interface AdminPromoCodeListItem {
  id: number;
  code: string;
  description: string;
  discountType: DiscountType;
  discountValue: number;
  startDate: string;
  endDate: string;
  timesUsed: number;
  usageLimit?: number;
  isActive: boolean;
}

export interface AdminPromoCodeDetail {
  id: number;
  code: string;
  description: string;
  discountType: DiscountType;
  discountValue: number;
  minimumOrderAmount?: number;
  maximumDiscountAmount?: number;
  startDate: string;
  endDate: string;
  usageLimit?: number;
  timesUsed: number;
  usageLimitPerUser?: number;
  isActive: boolean;
  excludeDiscountedItems: boolean;
  excludeAllServices: boolean;
}

export interface PromoExcludedProduct {
  id: number;
  productId: number;
  title: string;
  titleAr?: string;
}

export interface PromoExcludedComboOffer {
  id: number;
  comboOfferId: number;
  name: string;
  nameAr?: string;
}

export interface PromoExcludedService {
  id: number;
  serviceSubscriptionId: number;
  title: string;
  titleAr?: string;
}

export interface PromoCodeExclusions {
  products: PromoExcludedProduct[];
  comboOffers: PromoExcludedComboOffer[];
  services: PromoExcludedService[];
}

export interface UpsertAdminPromoCodeRequest {
  code: string;
  description: string;
  discountType: DiscountType;
  discountValue: number;
  minimumOrderAmount?: number;
  maximumDiscountAmount?: number;
  startDate: string;
  endDate: string;
  usageLimit?: number;
  usageLimitPerUser?: number;
  isActive: boolean;
  excludeDiscountedItems: boolean;
  excludeAllServices: boolean;
}
