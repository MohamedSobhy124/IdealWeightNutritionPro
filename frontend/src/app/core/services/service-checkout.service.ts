import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  CreateServicePurchaseRequest,
  CreateServicePurchaseResponse,
  ServiceCheckoutQuoteRequest,
  ServiceCheckoutQuoteResponse,
} from '../models/service-checkout.models';
import { PaymentMethodsResponse } from '../models/checkout.models';

@Injectable({ providedIn: 'root' })
export class ServiceCheckoutService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly httpOptions = { withCredentials: true };

  getQuote(request: ServiceCheckoutQuoteRequest) {
    return this.http.post<ServiceCheckoutQuoteResponse>(
      `${this.baseUrl}/services/checkout/quote`,
      request,
      this.httpOptions
    );
  }

  sendOtp(email: string) {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/services/checkout/otp`,
      { email },
      this.httpOptions
    );
  }

  verifyOtp(email: string, otp: string) {
    return this.http.post<{ isValid: boolean; message?: string }>(
      `${this.baseUrl}/services/checkout/verify-otp`,
      { email, otp },
      this.httpOptions
    );
  }

  getPaymentMethods(total: number) {
    const params = new HttpParams().set('total', total.toString());
    return this.http.get<PaymentMethodsResponse>(
      `${this.baseUrl}/services/checkout/payment-methods`,
      { ...this.httpOptions, params }
    );
  }

  purchase(serviceId: number, request: CreateServicePurchaseRequest) {
    return this.http.post<CreateServicePurchaseResponse>(
      `${this.baseUrl}/services/${serviceId}/purchase`,
      request,
      this.httpOptions
    );
  }

  completePayment(purchaseId: number) {
    return this.http.post<{ purchaseId: number; isPaid: boolean; message?: string }>(
      `${this.baseUrl}/services/purchases/${purchaseId}/complete-payment`,
      {},
      this.httpOptions
    );
  }
}
