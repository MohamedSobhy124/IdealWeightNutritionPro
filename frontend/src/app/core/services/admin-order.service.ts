import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminOrderActionResponse,
  AdminOrderAuditLog,
  AdminOrderDetail,
  AdminOrderListResponse,
  AdminOrderQuery,
  AdminOrderStatistics,
  ForceOrderActionRequest,
  ShipOrderRequest,
  UpdateOrderLineRequest,
} from '../models/admin-order.models';

@Injectable({ providedIn: 'root' })
export class AdminOrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly httpOptions = { withCredentials: true };

  listOrders(query: AdminOrderQuery = {}) {
    let params = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? 50));

    if (query.status) params = params.set('status', query.status);
    if (query.paymentStatus) params = params.set('paymentStatus', query.paymentStatus);
    if (query.paymentMethod) params = params.set('paymentMethod', query.paymentMethod);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    if (query.search?.trim()) params = params.set('search', query.search.trim());

    return this.http.get<AdminOrderListResponse>(`${this.baseUrl}/admin/orders`, {
      ...this.httpOptions,
      params,
    });
  }

  getStatistics() {
    return this.http.get<AdminOrderStatistics>(`${this.baseUrl}/admin/orders/statistics`, this.httpOptions);
  }

  getAuditLog(orderId: number) {
    return this.http.get<AdminOrderAuditLog[]>(
      `${this.baseUrl}/admin/orders/${orderId}/audit-log`,
      this.httpOptions
    );
  }

  exportCsv(query: AdminOrderQuery = {}) {
    let params = new HttpParams();
    if (query.status) params = params.set('status', query.status);
    if (query.paymentStatus) params = params.set('paymentStatus', query.paymentStatus);
    if (query.paymentMethod) params = params.set('paymentMethod', query.paymentMethod);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    if (query.search?.trim()) params = params.set('search', query.search.trim());

    return this.http.get(`${this.baseUrl}/admin/orders/export`, {
      params,
      responseType: 'blob',
      withCredentials: true,
    });
  }

  exportProductProfits(query: Pick<AdminOrderQuery, 'dateFrom' | 'dateTo'> = {}) {
    let params = new HttpParams();
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);

    return this.http.get(`${this.baseUrl}/admin/orders/export-product-profits`, {
      params,
      responseType: 'blob',
      withCredentials: true,
    });
  }

  getOrder(orderId: number) {
    return this.http.get<AdminOrderDetail>(`${this.baseUrl}/admin/orders/${orderId}`, this.httpOptions);
  }

  startProcessing(orderId: number) {
    return this.http.post<AdminOrderActionResponse>(
      `${this.baseUrl}/admin/orders/${orderId}/start-processing`,
      {},
      this.httpOptions
    );
  }

  shipOrder(orderId: number, request: ShipOrderRequest) {
    return this.http.post<AdminOrderActionResponse>(
      `${this.baseUrl}/admin/orders/${orderId}/ship`,
      request,
      this.httpOptions
    );
  }

  markDelivered(orderId: number) {
    return this.http.post<AdminOrderActionResponse>(
      `${this.baseUrl}/admin/orders/${orderId}/deliver`,
      {},
      this.httpOptions
    );
  }

  cancelOrder(orderId: number) {
    return this.http.post<AdminOrderActionResponse>(
      `${this.baseUrl}/admin/orders/${orderId}/cancel`,
      {},
      this.httpOptions
    );
  }

  recheckPayment(orderId: number) {
    return this.http.post<AdminOrderActionResponse>(
      `${this.baseUrl}/admin/orders/${orderId}/recheck-payment`,
      {},
      this.httpOptions
    );
  }

  forceComplete(orderId: number, request: ForceOrderActionRequest) {
    return this.http.post<AdminOrderActionResponse>(
      `${this.baseUrl}/admin/orders/${orderId}/force-complete`,
      request,
      this.httpOptions
    );
  }

  forceCancel(orderId: number, request: ForceOrderActionRequest) {
    return this.http.post<AdminOrderActionResponse>(
      `${this.baseUrl}/admin/orders/${orderId}/force-cancel`,
      request,
      this.httpOptions
    );
  }

  updateLineItem(orderId: number, request: UpdateOrderLineRequest) {
    return this.http.put<AdminOrderActionResponse>(
      `${this.baseUrl}/admin/orders/${orderId}/line-item`,
      request,
      this.httpOptions
    );
  }
}
