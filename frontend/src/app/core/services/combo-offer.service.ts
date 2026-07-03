import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ComboOfferDetail, ComboOfferSummary } from '../models/combo-offer.models';

@Injectable({ providedIn: 'root' })
export class ComboOfferService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  listActive() {
    return this.http.get<ComboOfferSummary[]>(`${this.baseUrl}/combo-offers`);
  }

  getActive(id: number) {
    return this.http.get<ComboOfferDetail>(`${this.baseUrl}/combo-offers/${id}`);
  }
}
