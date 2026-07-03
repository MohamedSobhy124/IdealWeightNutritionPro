import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ServiceSubscriptionDetail,
  ServiceSubscriptionSummary,
} from '../models/service.models';
import {
  ServicePurchaseDetail,
  ServicePurchaseSummary,
} from '../models/service-purchase.models';

@Injectable({ providedIn: 'root' })
export class ServiceSubscriptionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  listServices() {
    return this.http.get<ServiceSubscriptionSummary[]>(`${this.baseUrl}/services`);
  }

  getService(id: number) {
    return this.http.get<ServiceSubscriptionDetail>(`${this.baseUrl}/services/${id}`);
  }

  listMyPurchases() {
    return this.http.get<ServicePurchaseSummary[]>(
      `${this.baseUrl}/services/my-purchases`,
      { withCredentials: true }
    );
  }

  getMyPurchase(purchaseId: number) {
    return this.http.get<ServicePurchaseDetail>(
      `${this.baseUrl}/services/purchases/${purchaseId}`,
      { withCredentials: true }
    );
  }
}
