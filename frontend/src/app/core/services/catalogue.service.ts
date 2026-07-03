import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  Brand,
  Category,
  CategoryProductSection,
  PagedResult,
  ProductDetail,
  ProductListItem,
  ProductQuery,
} from '../models/catalogue.models';

@Injectable({ providedIn: 'root' })
export class CatalogueService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  listProducts(query: ProductQuery = {}) {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    if (query.categoryId) params = params.set('categoryId', query.categoryId);
    if (query.brandId) params = params.set('brandId', query.brandId);
    if (query.availability) params = params.set('availability', query.availability);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    params = params.set('page', String(query.page ?? 1));
    params = params.set('pageSize', String(query.pageSize ?? 20));

    return this.http.get<PagedResult<ProductListItem>>(`${this.baseUrl}/products`, { params });
  }

  getProductBySlug(slug: string) {
    return this.http.get<ProductDetail>(`${this.baseUrl}/products/${encodeURIComponent(slug)}`);
  }

  listCategories() {
    return this.http.get<Category[]>(`${this.baseUrl}/categories`);
  }

  listBrands() {
    return this.http.get<Brand[]>(`${this.baseUrl}/brands`);
  }

  listDiscountedProducts(limit = 20) {
    return this.http.get<ProductListItem[]>(`${this.baseUrl}/catalogue/discounted-products`, {
      params: new HttpParams().set('limit', String(limit)),
    });
  }

  getCategoryProductSections(maxCategories = 6, productsPerCategory = 4) {
    return this.http.get<CategoryProductSection[]>(`${this.baseUrl}/catalogue/category-sections`, {
      params: new HttpParams()
        .set('maxCategories', String(maxCategories))
        .set('productsPerCategory', String(productsPerCategory)),
    });
  }

  resolveImageUrl(url: string): string {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) return url;
    const base = environment.legacyAssetsBaseUrl?.replace(/\/$/, '') ?? '';
    return `${base}${url.startsWith('/') ? url : '/' + url}`;
  }
}
