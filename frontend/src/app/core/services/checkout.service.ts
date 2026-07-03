import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  City,
  CompletePaymentResponse,
  CreateOrderRequest,
  CreateOrderResponse,
  OtpMessageResponse,
  PaymentMethodsResponse,
  RemoteArea,
  ShippingQuoteRequest,
  ShippingQuoteResponse,
} from '../models/checkout.models';

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly httpOptions = { withCredentials: true };

  listCities() {
    return this.http.get<City[]>(`${this.baseUrl}/checkout/cities`, this.httpOptions);
  }

  listRemoteAreas(cityId: number) {
    return this.http.get<RemoteArea[]>(
      `${this.baseUrl}/checkout/cities/${cityId}/remote-areas`,
      this.httpOptions
    );
  }

  getShippingQuote(request: ShippingQuoteRequest) {
    return this.http.post<ShippingQuoteResponse>(
      `${this.baseUrl}/checkout/shipping-quote`,
      request,
      this.httpOptions
    );
  }

  sendOtp(email: string) {
    return this.http.post<OtpMessageResponse>(
      `${this.baseUrl}/checkout/otp`,
      { email },
      this.httpOptions
    );
  }

  verifyOtp(email: string, otp: string) {
    return this.http.post<OtpMessageResponse>(
      `${this.baseUrl}/checkout/verify-otp`,
      { email, otp },
      this.httpOptions
    );
  }

  placeOrder(request: CreateOrderRequest) {
    return this.http.post<CreateOrderResponse>(
      `${this.baseUrl}/checkout`,
      request,
      this.httpOptions
    );
  }

  getPaymentMethods(total: number) {
    return this.http.get<PaymentMethodsResponse>(
      `${this.baseUrl}/checkout/payment-methods`,
      { ...this.httpOptions, params: { total: total.toString() } }
    );
  }

  completePayment(orderId: number) {
    return this.http.post<CompletePaymentResponse>(
      `${this.baseUrl}/checkout/orders/${orderId}/complete-payment`,
      {},
      this.httpOptions
    );
  }
}
