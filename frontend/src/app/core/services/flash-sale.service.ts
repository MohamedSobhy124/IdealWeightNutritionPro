import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { FlashSaleDetail, FlashSaleProductPrice, FlashSaleSummary } from '../models/flash-sale.models';

@Injectable({ providedIn: 'root' })
export class FlashSaleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  listActive() {
    return this.http.get<FlashSaleSummary[]>(`${this.baseUrl}/flash-sales`);
  }

  getActive(id: number) {
    return this.http.get<FlashSaleDetail>(`${this.baseUrl}/flash-sales/${id}`);
  }

  listProductPrices() {
    return this.http.get<FlashSaleProductPrice[]>(`${this.baseUrl}/flash-sales/product-prices`);
  }
}
