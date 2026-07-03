import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AdminCompany, UpsertAdminCompanyRequest } from '../models/admin-company.models';

@Injectable({ providedIn: 'root' })
export class AdminCompanyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list() {
    return this.http.get<AdminCompany[]>(`${this.baseUrl}/admin/companies`, {
      withCredentials: true,
    });
  }

  get(id: number) {
    return this.http.get<AdminCompany>(`${this.baseUrl}/admin/companies/${id}`, {
      withCredentials: true,
    });
  }

  create(request: UpsertAdminCompanyRequest) {
    return this.http.post<AdminCompany>(`${this.baseUrl}/admin/companies`, request, {
      withCredentials: true,
    });
  }

  update(id: number, request: UpsertAdminCompanyRequest) {
    return this.http.put<AdminCompany>(`${this.baseUrl}/admin/companies/${id}`, request, {
      withCredentials: true,
    });
  }

  delete(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/admin/companies/${id}`, {
      withCredentials: true,
    });
  }
}
