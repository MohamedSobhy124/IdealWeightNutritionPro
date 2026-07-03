export interface ServiceOffer {
  id: number;
  discountType: string;
  discountValue: number;
  startDate: string;
  endDate: string;
}

export interface ServiceSubscriptionSummary {
  id: number;
  title: string;
  titleAr?: string | null;
  description?: string | null;
  descriptionAr?: string | null;
  price: number;
  salePrice?: number | null;
  serviceType: string;
  imageUrl?: string | null;
  hasActiveOffer: boolean;
}

export interface ServiceSubscriptionDetail extends ServiceSubscriptionSummary {
  offlinePaymentPercent?: number | null;
  imageUrls: string[];
  activeOffers: ServiceOffer[];
}
