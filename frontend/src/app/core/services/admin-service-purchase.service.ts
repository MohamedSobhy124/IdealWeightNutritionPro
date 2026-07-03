import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminServicePurchaseDetail,
  AdminServicePurchaseActionResponse,
  AdminServicePurchaseListResponse,
  AdminServicePurchaseQuery,
  AdminServicePurchaseStatistics,
  UpdateAdminServicePurchaseRequest,
} from '../models/admin-service-purchase.models';

@Injectable({ providedIn: 'root' })
export class AdminServicePurchaseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list(query: AdminServicePurchaseQuery = {}) {
    let params = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? 25));

    if (query.paymentStatus) params = params.set('paymentStatus', query.paymentStatus);
    if (query.serviceStatus) params = params.set('serviceStatus', query.serviceStatus);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    if (query.search?.trim()) params = params.set('search', query.search.trim());

    return this.http.get<AdminServicePurchaseListResponse>(`${this.baseUrl}/admin/service-purchases`, {
      params,
      withCredentials: true,
    });
  }

  get(id: number) {
    return this.http.get<AdminServicePurchaseDetail>(`${this.baseUrl}/admin/service-purchases/${id}`, {
      withCredentials: true,
    });
  }

  exportCsv(query: AdminServicePurchaseQuery = {}) {
    let params = new HttpParams();
    if (query.paymentStatus) params = params.set('paymentStatus', query.paymentStatus);
    if (query.serviceStatus) params = params.set('serviceStatus', query.serviceStatus);
    if (query.dateFrom) params = params.set('dateFrom', query.dateFrom);
    if (query.dateTo) params = params.set('dateTo', query.dateTo);
    if (query.search?.trim()) params = params.set('search', query.search.trim());

    return this.http.get(`${this.baseUrl}/admin/service-purchases/export`, {
      params,
      responseType: 'blob',
      withCredentials: true,
    });
  }

  getStatistics() {
    return this.http.get<AdminServicePurchaseStatistics>(
      `${this.baseUrl}/admin/service-purchases/statistics`,
      { withCredentials: true }
    );
  }

  update(id: number, request: UpdateAdminServicePurchaseRequest) {
    return this.http.put<AdminServicePurchaseActionResponse>(
      `${this.baseUrl}/admin/service-purchases/${id}`,
      request,
      { withCredentials: true }
    );
  }
}
