import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminServiceOfferDetail,
  AdminServiceOfferListItem,
  UpsertAdminServiceOfferRequest,
} from '../models/admin-service-offer.models';

@Injectable({ providedIn: 'root' })
export class AdminServiceOfferService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list(serviceSubscriptionId?: number) {
    let params = new HttpParams();
    if (serviceSubscriptionId != null) {
      params = params.set('serviceSubscriptionId', serviceSubscriptionId);
    }
    return this.http.get<AdminServiceOfferListItem[]>(`${this.baseUrl}/admin/service-offers`, {
      params,
      withCredentials: true,
    });
  }

  get(id: number) {
    return this.http.get<AdminServiceOfferDetail>(`${this.baseUrl}/admin/service-offers/${id}`, {
      withCredentials: true,
    });
  }

  create(request: UpsertAdminServiceOfferRequest) {
    return this.http.post<AdminServiceOfferDetail>(`${this.baseUrl}/admin/service-offers`, request, {
      withCredentials: true,
    });
  }

  update(id: number, request: UpsertAdminServiceOfferRequest) {
    return this.http.put<AdminServiceOfferDetail>(`${this.baseUrl}/admin/service-offers/${id}`, request, {
      withCredentials: true,
    });
  }

  toggle(id: number) {
    return this.http.post(`${this.baseUrl}/admin/service-offers/${id}/toggle`, {}, {
      withCredentials: true,
    });
  }

  delete(id: number) {
    return this.http.delete(`${this.baseUrl}/admin/service-offers/${id}`, { withCredentials: true });
  }
}
