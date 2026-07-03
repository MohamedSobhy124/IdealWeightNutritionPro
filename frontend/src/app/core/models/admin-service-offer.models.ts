export type ServiceDiscountType = 'Percentage' | 'FixedAmount' | 1 | 2;

export interface AdminServiceOfferListItem {
  id: number;
  serviceSubscriptionId: number;
  serviceTitle: string;
  promoCodeId?: number | null;
  promoCode?: string | null;
  discountType: ServiceDiscountType;
  discountValue: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
}

export interface AdminServiceOfferDetail extends AdminServiceOfferListItem {
  createdDate: string;
}

export interface UpsertAdminServiceOfferRequest {
  serviceSubscriptionId: number;
  promoCodeId?: number | null;
  discountType: ServiceDiscountType;
  discountValue: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
}
