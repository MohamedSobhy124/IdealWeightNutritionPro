import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminBrand,
  AdminCategory,
  UpsertAdminBrandRequest,
  UpsertAdminCategoryRequest,
} from '../models/admin-catalogue.models';

@Injectable({ providedIn: 'root' })
export class AdminCatalogueService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  listCategories(includeDeleted = false) {
    const params = new HttpParams().set('includeDeleted', includeDeleted);
    return this.http.get<AdminCategory[]>(`${this.baseUrl}/admin/categories`, {
      params,
      withCredentials: true,
    });
  }

  createCategory(request: UpsertAdminCategoryRequest) {
    return this.http.post<AdminCategory>(`${this.baseUrl}/admin/categories`, request, {
      withCredentials: true,
    });
  }

  updateCategory(id: number, request: UpsertAdminCategoryRequest) {
    return this.http.put<AdminCategory>(`${this.baseUrl}/admin/categories/${id}`, request, {
      withCredentials: true,
    });
  }

  deleteCategory(id: number) {
    return this.http.delete(`${this.baseUrl}/admin/categories/${id}`, { withCredentials: true });
  }

  listBrands(includeDeleted = false) {
    const params = new HttpParams().set('includeDeleted', includeDeleted);
    return this.http.get<AdminBrand[]>(`${this.baseUrl}/admin/brands`, {
      params,
      withCredentials: true,
    });
  }

  createBrand(request: UpsertAdminBrandRequest) {
    return this.http.post<AdminBrand>(`${this.baseUrl}/admin/brands`, request, {
      withCredentials: true,
    });
  }

  updateBrand(id: number, request: UpsertAdminBrandRequest) {
    return this.http.put<AdminBrand>(`${this.baseUrl}/admin/brands/${id}`, request, {
      withCredentials: true,
    });
  }

  deleteBrand(id: number) {
    return this.http.delete(`${this.baseUrl}/admin/brands/${id}`, { withCredentials: true });
  }
}
