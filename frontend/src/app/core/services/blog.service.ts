import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { BlogPostDetail, BlogPostSummary } from '../models/blog.models';

@Injectable({ providedIn: 'root' })
export class BlogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  listPosts() {
    return this.http.get<BlogPostSummary[]>(`${this.baseUrl}/blog`);
  }

  getPost(slug: string) {
    return this.http.get<BlogPostDetail>(`${this.baseUrl}/blog/${encodeURIComponent(slug)}`);
  }
}
