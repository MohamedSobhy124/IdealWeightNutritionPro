import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AdminDashboard } from '../models/admin-dashboard.models';

@Injectable({ providedIn: 'root' })
export class AdminDashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getDashboard() {
    return this.http.get<AdminDashboard>(`${this.baseUrl}/admin/dashboard`, { withCredentials: true });
  }
}
