export interface ServicePurchaseSummary {
  id: number;
  serviceSubscriptionId: number;
  serviceTitle: string;
  serviceTitleAr?: string | null;
  serviceImageUrl?: string | null;
  serviceType: string;
  totalAmount: number;
  amountPaid: number;
  discountAmount: number;
  paymentStatus: string;
  status: string;
  purchaseDate: string;
}

export interface ServicePurchaseDetail extends ServicePurchaseSummary {
  serviceDescription?: string | null;
  serviceDescriptionAr?: string | null;
}
