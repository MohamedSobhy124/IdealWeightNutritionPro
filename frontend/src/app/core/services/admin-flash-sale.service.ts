import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AddAdminFlashSaleItemRequest,
  AdminFlashSaleDetail,
  AdminFlashSaleListItem,
  UpsertAdminFlashSaleRequest,
} from '../models/admin-flash-sale.models';

@Injectable({ providedIn: 'root' })
export class AdminFlashSaleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list() {
    return this.http.get<AdminFlashSaleListItem[]>(`${this.baseUrl}/admin/flash-sales`, {
      withCredentials: true,
    });
  }

  get(id: number) {
    return this.http.get<AdminFlashSaleDetail>(`${this.baseUrl}/admin/flash-sales/${id}`, {
      withCredentials: true,
    });
  }

  create(request: UpsertAdminFlashSaleRequest) {
    return this.http.post<AdminFlashSaleDetail>(`${this.baseUrl}/admin/flash-sales`, request, {
      withCredentials: true,
    });
  }

  update(id: number, request: UpsertAdminFlashSaleRequest) {
    return this.http.put<AdminFlashSaleDetail>(`${this.baseUrl}/admin/flash-sales/${id}`, request, {
      withCredentials: true,
    });
  }

  toggle(id: number) {
    return this.http.post(`${this.baseUrl}/admin/flash-sales/${id}/toggle`, {}, {
      withCredentials: true,
    });
  }

  delete(id: number) {
    return this.http.delete(`${this.baseUrl}/admin/flash-sales/${id}`, { withCredentials: true });
  }

  addItem(flashSaleId: number, request: AddAdminFlashSaleItemRequest) {
    return this.http.post<AdminFlashSaleDetail>(
      `${this.baseUrl}/admin/flash-sales/${flashSaleId}/items`,
      request,
      { withCredentials: true }
    );
  }

  removeItem(itemId: number) {
    return this.http.delete(`${this.baseUrl}/admin/flash-sales/items/${itemId}`, {
      withCredentials: true,
    });
  }
}
