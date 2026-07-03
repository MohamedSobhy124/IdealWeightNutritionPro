import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminBlogPostDetail,
  AdminBlogPostListItem,
  UpsertAdminBlogPostRequest,
} from '../models/admin-blog.models';

@Injectable({ providedIn: 'root' })
export class AdminBlogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list() {
    return this.http.get<AdminBlogPostListItem[]>(`${this.baseUrl}/admin/blog-posts`, {
      withCredentials: true,
    });
  }

  get(id: number) {
    return this.http.get<AdminBlogPostDetail>(`${this.baseUrl}/admin/blog-posts/${id}`, {
      withCredentials: true,
    });
  }

  create(request: UpsertAdminBlogPostRequest) {
    return this.http.post<AdminBlogPostDetail>(`${this.baseUrl}/admin/blog-posts`, request, {
      withCredentials: true,
    });
  }

  update(id: number, request: UpsertAdminBlogPostRequest) {
    return this.http.put<AdminBlogPostDetail>(`${this.baseUrl}/admin/blog-posts/${id}`, request, {
      withCredentials: true,
    });
  }

  delete(id: number) {
    return this.http.delete(`${this.baseUrl}/admin/blog-posts/${id}`, { withCredentials: true });
  }

  restore(id: number) {
    return this.http.post(`${this.baseUrl}/admin/blog-posts/${id}/restore`, {}, {
      withCredentials: true,
    });
  }
}
