import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminUserListItem,
  CreateAdminUserRequest,
  CreateAdminUserResponse,
} from '../models/admin-user.models';

@Injectable({ providedIn: 'root' })
export class AdminUserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list() {
    return this.http.get<AdminUserListItem[]>(`${this.baseUrl}/admin/users`, {
      withCredentials: true,
    });
  }

  create(request: CreateAdminUserRequest) {
    return this.http.post<CreateAdminUserResponse>(`${this.baseUrl}/admin/users`, request, {
      withCredentials: true,
    });
  }
}
