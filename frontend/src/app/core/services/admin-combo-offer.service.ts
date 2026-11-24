import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AddAdminComboOfferItemRequest,
  AdminComboOfferDetail,
  AdminComboOfferImage,
  AdminComboOfferItem,
  UpdateAdminComboOfferItemRequest,
  AdminComboOfferListItem,
  UpsertAdminComboOfferRequest,
} from '../models/admin-combo-offer.models';

@Injectable({ providedIn: 'root' })
export class AdminComboOfferService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list() {
    return this.http.get<AdminComboOfferListItem[]>(`${this.baseUrl}/admin/combo-offers`, {
      withCredentials: true,
    });
  }

  get(id: number) {
    return this.http.get<AdminComboOfferDetail>(`${this.baseUrl}/admin/combo-offers/${id}`, {
      withCredentials: true,
    });
  }

  create(request: UpsertAdminComboOfferRequest) {
    return this.http.post<AdminComboOfferDetail>(`${this.baseUrl}/admin/combo-offers`, request, {
      withCredentials: true,
    });
  }

  update(id: number, request: UpsertAdminComboOfferRequest) {
    return this.http.put<AdminComboOfferDetail>(`${this.baseUrl}/admin/combo-offers/${id}`, request, {
      withCredentials: true,
    });
  }

  toggle(id: number) {
    return this.http.post(`${this.baseUrl}/admin/combo-offers/${id}/toggle`, {}, {
      withCredentials: true,
    });
  }

  delete(id: number) {
    return this.http.delete(`${this.baseUrl}/admin/combo-offers/${id}`, { withCredentials: true });
  }

  addItem(comboOfferId: number, request: AddAdminComboOfferItemRequest) {
    return this.http.post<AdminComboOfferItem>(
      `${this.baseUrl}/admin/combo-offers/${comboOfferId}/items`,
      request,
      { withCredentials: true }
    );
  }

  updateItem(comboOfferId: number, itemId: number, request: UpdateAdminComboOfferItemRequest) {
    return this.http.put<AdminComboOfferItem>(
      `${this.baseUrl}/admin/combo-offers/${comboOfferId}/items/${itemId}`,
      request,
      { withCredentials: true }
    );
  }

  addImage(comboOfferId: number, file: File, imageInfo?: string) {
    const form = new FormData();
    form.append('file', file);
    if (imageInfo?.trim()) form.append('imageInfo', imageInfo.trim());
    return this.http.post<AdminComboOfferImage>(`${this.baseUrl}/admin/combo-offers/${comboOfferId}/images`, form, {
      withCredentials: true,
    });
  }

  deleteImage(comboOfferId: number, imageId: number) {
    return this.http.delete<void>(`${this.baseUrl}/admin/combo-offers/${comboOfferId}/images/${imageId}`, {
      withCredentials: true,
    });
  }

  removeItem(itemId: number) {
    return this.http.delete(`${this.baseUrl}/admin/combo-offers/items/${itemId}`, {
      withCredentials: true,
    });
  }
}
