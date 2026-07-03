export interface City {
  id: number;
  name: string;
  nameAr?: string;
  emirate: string;
  deliveryCharge: number;
}

export interface RemoteArea {
  id: number;
  cityId: number;
  name: string;
  nameAr?: string;
  deliveryCharge: number;
}

export interface ShippingQuoteRequest {
  cityId: number;
  remoteAreaId?: number;
}

export interface ShippingQuoteResponse {
  subtotal: number;
  discount: number;
  shipping: number;
  total: number;
  cityName: string;
  areaName?: string;
}

export interface CreateOrderRequest {
  name: string;
  email: string;
  phoneNumber: string;
  streetAddress: string;
  cityId: number;
  remoteAreaId?: number;
  state?: string;
  postalCode?: string;
  otp?: string;
  paymentMethod: string;
  createAccountForGuest?: boolean;
}

export interface CreateOrderResponse {
  orderId: number;
  orderStatus: string;
  paymentStatus: string;
  orderTotal: number;
  paymentMethod?: string;
  requiresPaymentAction?: boolean;
  paymentSessionId?: string;
  paymentRedirectUrl?: string;
  accountCreated?: boolean;
  accountLinked?: boolean;
}

export interface PaymentMethodOption {
  id: string;
  label: string;
  available: boolean;
  unavailableReason?: string;
  unavailableReasonCode?: string;
  minimumAmount?: number;
}

export interface PaymentMethodsResponse {
  methods: PaymentMethodOption[];
}

export interface CompletePaymentResponse {
  orderId: number;
  orderStatus: string;
  paymentStatus: string;
  isPaid: boolean;
  message?: string;
}

export interface OtpMessageResponse {
  message: string;
}
