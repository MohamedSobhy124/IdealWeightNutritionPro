export interface ServiceCheckoutQuoteRequest {
  serviceId: number;
  offerId?: number | null;
  promoCode?: string | null;
  customAmount?: number | null;
}

export interface ServiceCheckoutQuoteResponse {
  serviceId: number;
  serviceTitle: string;
  serviceTitleAr?: string | null;
  listPrice: number;
  discountAmount: number;
  totalAmount: number;
  amountToPay: number;
  minPaymentAmount?: number | null;
  isFree: boolean;
  serviceType: string;
  appliedOfferId?: number | null;
  appliedPromoCode?: string | null;
  promoMessage?: string | null;
}

export interface CreateServicePurchaseRequest {
  name: string;
  email: string;
  phoneNumber: string;
  offerId?: number | null;
  promoCode?: string | null;
  customAmount?: number | null;
  otp?: string | null;
  paymentMethod: string;
  createAccountForGuest?: boolean;
}

export interface CreateServicePurchaseResponse {
  purchaseId: number;
  paymentStatus: string;
  amountPaid: number;
  paymentMethod?: string | null;
  requiresPaymentAction: boolean;
  paymentSessionId?: string | null;
  paymentRedirectUrl?: string | null;
  isPaid: boolean;
  accountCreated?: boolean;
  accountLinked?: boolean;
}
