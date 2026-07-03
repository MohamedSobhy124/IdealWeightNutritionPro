import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminPromoCodeDetail,
  AdminPromoCodeListItem,
  PromoCodeExclusions,
  UpsertAdminPromoCodeRequest,
} from '../models/admin-promo.models';

@Injectable({ providedIn: 'root' })
export class AdminPromoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list() {
    return this.http.get<AdminPromoCodeListItem[]>(`${this.baseUrl}/admin/promo-codes`, {
      withCredentials: true,
    });
  }

  get(id: number) {
    return this.http.get<AdminPromoCodeDetail>(`${this.baseUrl}/admin/promo-codes/${id}`, {
      withCredentials: true,
    });
  }

  create(request: UpsertAdminPromoCodeRequest) {
    return this.http.post<AdminPromoCodeDetail>(`${this.baseUrl}/admin/promo-codes`, request, {
      withCredentials: true,
    });
  }

  update(id: number, request: UpsertAdminPromoCodeRequest) {
    return this.http.put<AdminPromoCodeDetail>(`${this.baseUrl}/admin/promo-codes/${id}`, request, {
      withCredentials: true,
    });
  }

  toggle(id: number) {
    return this.http.post(`${this.baseUrl}/admin/promo-codes/${id}/toggle`, {}, {
      withCredentials: true,
    });
  }

  delete(id: number) {
    return this.http.delete(`${this.baseUrl}/admin/promo-codes/${id}`, { withCredentials: true });
  }

  getExclusions(promoId: number) {
    return this.http.get<PromoCodeExclusions>(`${this.baseUrl}/admin/promo-codes/${promoId}/exclusions`, {
      withCredentials: true,
    });
  }

  addExcludedProduct(promoId: number, productId: number) {
    return this.http.post(
      `${this.baseUrl}/admin/promo-codes/${promoId}/exclusions/products`,
      { productId },
      { withCredentials: true }
    );
  }

  removeExcludedProduct(exclusionId: number) {
    return this.http.delete(`${this.baseUrl}/admin/promo-codes/exclusions/products/${exclusionId}`, {
      withCredentials: true,
    });
  }

  addExcludedCombo(promoId: number, comboOfferId: number) {
    return this.http.post(
      `${this.baseUrl}/admin/promo-codes/${promoId}/exclusions/combos`,
      { comboOfferId },
      { withCredentials: true }
    );
  }

  removeExcludedCombo(exclusionId: number) {
    return this.http.delete(`${this.baseUrl}/admin/promo-codes/exclusions/combos/${exclusionId}`, {
      withCredentials: true,
    });
  }

  addExcludedService(promoId: number, serviceSubscriptionId: number) {
    return this.http.post(
      `${this.baseUrl}/admin/promo-codes/${promoId}/exclusions/services`,
      { serviceSubscriptionId },
      { withCredentials: true }
    );
  }

  removeExcludedService(exclusionId: number) {
    return this.http.delete(`${this.baseUrl}/admin/promo-codes/exclusions/services/${exclusionId}`, {
      withCredentials: true,
    });
  }
}
