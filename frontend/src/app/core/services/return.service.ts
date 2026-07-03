import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  CreateReturnRequest,
  RefundOrderRequest,
  RefundOrderResponse,
  ReturnListItem,
  ReturnRequest,
} from '../models/return.models';

@Injectable({ providedIn: 'root' })
export class ReturnService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  create(request: CreateReturnRequest) {
    return this.http.post<ReturnRequest>(`${this.baseUrl}/returns`, request, { withCredentials: true });
  }

  listMine() {
    return this.http.get<ReturnListItem[]>(`${this.baseUrl}/returns`, { withCredentials: true });
  }

  get(id: number, email?: string) {
    let params = new HttpParams();
    if (email?.trim()) params = params.set('email', email.trim());
    return this.http.get<ReturnRequest>(`${this.baseUrl}/returns/${id}`, {
      params,
      withCredentials: true,
    });
  }

  listAdmin(status = 'all') {
    const params = new HttpParams().set('status', status);
    return this.http.get<ReturnListItem[]>(`${this.baseUrl}/admin/returns`, {
      params,
      withCredentials: true,
    });
  }

  getAdmin(id: number) {
    return this.http.get<ReturnRequest>(`${this.baseUrl}/admin/returns/${id}`, { withCredentials: true });
  }

  approve(id: number, body: { adminNotes?: string; returnTrackingNumber?: string; returnCarrier?: string }) {
    return this.http.post(`${this.baseUrl}/admin/returns/${id}/approve`, body, { withCredentials: true });
  }

  reject(id: number, body: { rejectionReason: string; adminNotes?: string }) {
    return this.http.post(`${this.baseUrl}/admin/returns/${id}/reject`, body, { withCredentials: true });
  }

  markReceived(id: number) {
    return this.http.post(`${this.baseUrl}/admin/returns/${id}/receive`, {}, { withCredentials: true });
  }

  complete(id: number, body: { refundTransactionId?: string } = {}) {
    return this.http.post(`${this.baseUrl}/admin/returns/${id}/complete`, body, { withCredentials: true });
  }

  cancel(id: number, body: { reason?: string; adminNotes?: string } = {}) {
    return this.http.post(`${this.baseUrl}/admin/returns/${id}/cancel`, body, { withCredentials: true });
  }

  refundOrder(orderId: number, body: RefundOrderRequest) {
    return this.http.post<RefundOrderResponse>(`${this.baseUrl}/admin/orders/${orderId}/refund`, body, {
      withCredentials: true,
    });
  }
}
