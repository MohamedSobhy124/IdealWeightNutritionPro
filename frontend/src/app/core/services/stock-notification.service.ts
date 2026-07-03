import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface StockNotificationSubscribeRequest {
  email?: string;
  phoneNumber?: string;
  productVariantId?: number;
}

export interface StockNotificationSubscribeResponse {
  message: string;
}

@Injectable({ providedIn: 'root' })
export class StockNotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  subscribe(productId: number, request: StockNotificationSubscribeRequest) {
    return this.http.post<StockNotificationSubscribeResponse>(
      `${this.baseUrl}/products/${productId}/stock-notify`,
      request
    );
  }
}
