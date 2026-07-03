import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Order, OrderSummary, TrackOrderRequest } from '../models/order.models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly httpOptions = { withCredentials: true };

  getOrder(orderId: number, email?: string) {
    let params = new HttpParams();
    if (email) {
      params = params.set('email', email);
    }
    return this.http.get<Order>(`${this.baseUrl}/orders/${orderId}`, {
      ...this.httpOptions,
      params,
    });
  }

  downloadInvoice(orderId: number, email?: string) {
    let params = new HttpParams();
    if (email) {
      params = params.set('email', email);
    }
    return this.http.get(`${this.baseUrl}/orders/${orderId}/invoice`, {
      ...this.httpOptions,
      params,
      responseType: 'blob',
    });
  }

  listMyOrders() {
    return this.http.get<OrderSummary[]>(`${this.baseUrl}/orders`, this.httpOptions);
  }

  trackOrder(request: TrackOrderRequest) {
    return this.http.post<Order>(`${this.baseUrl}/orders/track`, request, this.httpOptions);
  }
}
