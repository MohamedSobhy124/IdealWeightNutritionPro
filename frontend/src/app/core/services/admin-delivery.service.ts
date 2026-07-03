import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminCity,
  AdminRemoteArea,
  UpsertAdminCityRequest,
  UpsertAdminRemoteAreaRequest,
} from '../models/admin-delivery.models';

@Injectable({ providedIn: 'root' })
export class AdminDeliveryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  listCities() {
    return this.http.get<AdminCity[]>(`${this.baseUrl}/admin/cities`, { withCredentials: true });
  }

  createCity(request: UpsertAdminCityRequest) {
    return this.http.post<AdminCity>(`${this.baseUrl}/admin/cities`, request, {
      withCredentials: true,
    });
  }

  updateCity(id: number, request: UpsertAdminCityRequest) {
    return this.http.put<AdminCity>(`${this.baseUrl}/admin/cities/${id}`, request, {
      withCredentials: true,
    });
  }

  deleteCity(id: number) {
    return this.http.delete(`${this.baseUrl}/admin/cities/${id}`, { withCredentials: true });
  }

  listRemoteAreas(cityId: number) {
    return this.http.get<AdminRemoteArea[]>(
      `${this.baseUrl}/admin/cities/${cityId}/remote-areas`,
      { withCredentials: true }
    );
  }

  createRemoteArea(cityId: number, request: UpsertAdminRemoteAreaRequest) {
    return this.http.post<AdminRemoteArea>(
      `${this.baseUrl}/admin/cities/${cityId}/remote-areas`,
      request,
      { withCredentials: true }
    );
  }

  updateRemoteArea(cityId: number, areaId: number, request: UpsertAdminRemoteAreaRequest) {
    return this.http.put<AdminRemoteArea>(
      `${this.baseUrl}/admin/cities/${cityId}/remote-areas/${areaId}`,
      request,
      { withCredentials: true }
    );
  }

  deleteRemoteArea(areaId: number) {
    return this.http.delete(`${this.baseUrl}/admin/remote-areas/${areaId}`, {
      withCredentials: true,
    });
  }
}
