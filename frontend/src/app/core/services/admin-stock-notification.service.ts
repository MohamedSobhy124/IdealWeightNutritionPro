import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminOrderListResponse,
  AdminOrderQuery,
} from '../models/admin-order.models';

@Injectable({ providedIn: 'root' })
export class AdminStockNotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list(query: {
    activeOnly?: boolean;
    pendingOnly?: boolean;
    search?: string;
    page?: number;
    pageSize?: number;
  } = {}) {
    let params = new HttpParams()
      .set('activeOnly', String(query.activeOnly ?? true))
      .set('pendingOnly', String(query.pendingOnly ?? false))
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? 25));

    if (query.search?.trim()) params = params.set('search', query.search.trim());

    return this.http.get<AdminStockNotificationListResponse>(
      `${this.baseUrl}/admin/stock-notifications`,
      { params, withCredentials: true }
    );
  }

  deactivate(id: number) {
    return this.http.post<AdminStockNotificationActionResponse>(
      `${this.baseUrl}/admin/stock-notifications/${id}/deactivate`,
      {},
      { withCredentials: true }
    );
  }
}

export interface AdminStockNotificationListItem {
  id: number;
  productId: number;
  productTitle: string;
  productVariantId?: number;
  variantSku?: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  isNotified: boolean;
  notifiedDate?: string;
  createdDate: string;
}

export interface AdminStockNotificationListResponse {
  items: AdminStockNotificationListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminStockNotificationActionResponse {
  id: number;
  message: string;
}
