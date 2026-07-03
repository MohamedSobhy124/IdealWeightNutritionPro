import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminImageUploadResult,
  AdminServiceDetail,
  AdminServiceImage,
  AdminServiceListItem,
  UpsertAdminServiceRequest,
} from '../models/admin-service.models';

@Injectable({ providedIn: 'root' })
export class AdminServiceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list(includeInactive = true) {
    const params = new HttpParams().set('includeInactive', includeInactive);
    return this.http.get<AdminServiceListItem[]>(`${this.baseUrl}/admin/services`, {
      params,
      withCredentials: true,
    });
  }

  get(id: number) {
    return this.http.get<AdminServiceDetail>(`${this.baseUrl}/admin/services/${id}`, {
      withCredentials: true,
    });
  }

  create(request: UpsertAdminServiceRequest) {
    return this.http.post<AdminServiceDetail>(`${this.baseUrl}/admin/services`, request, {
      withCredentials: true,
    });
  }

  update(id: number, request: UpsertAdminServiceRequest) {
    return this.http.put<AdminServiceDetail>(`${this.baseUrl}/admin/services/${id}`, request, {
      withCredentials: true,
    });
  }

  toggle(id: number) {
    return this.http.post(`${this.baseUrl}/admin/services/${id}/toggle`, {}, {
      withCredentials: true,
    });
  }

  delete(id: number) {
    return this.http.delete(`${this.baseUrl}/admin/services/${id}`, { withCredentials: true });
  }

  uploadImage(serviceId: number, file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<AdminServiceImage>(`${this.baseUrl}/admin/services/${serviceId}/images`, form, {
      withCredentials: true,
    });
  }

  deleteImage(serviceId: number, imageId: number) {
    return this.http.delete<void>(`${this.baseUrl}/admin/services/${serviceId}/images/${imageId}`, {
      withCredentials: true,
    });
  }
}

@Injectable({ providedIn: 'root' })
export class AdminMediaService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  uploadFlashSaleImage(flashSaleId: number, file: File) {
    return this.upload(`${this.baseUrl}/admin/flash-sales/${flashSaleId}/image`, file);
  }

  uploadComboOfferImage(comboOfferId: number, file: File) {
    return this.upload(`${this.baseUrl}/admin/combo-offers/${comboOfferId}/image`, file);
  }

  uploadBlogPostImage(blogPostId: number, file: File) {
    return this.upload(`${this.baseUrl}/admin/blog-posts/${blogPostId}/image`, file);
  }

  private upload(url: string, file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<AdminImageUploadResult>(url, form, { withCredentials: true });
  }
}
