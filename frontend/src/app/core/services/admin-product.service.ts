import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  AdminProductDetail,
  AdminProductImage,
  AdminProductListResponse,
  AdminProductFilter,
  RegenerateProductSlugsResponse,
  AddAdminProductOptionRequest,
  AddAdminProductOptionValueRequest,
  AdminProductOption,
  CreateAdminProductRequest,
  GenerateVariantsResponse,
  SetProductTypeRequest,
  UpdateAdminProductRequest,
  UpdateAdminProductVariantDetailRequest,
} from '../models/admin-product.models';

@Injectable({ providedIn: 'root' })
export class AdminProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  list(page = 1, pageSize = 50, search?: string, filter: AdminProductFilter = 'active') {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('filter', filter);
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<AdminProductListResponse>(`${this.baseUrl}/admin/products`, {
      params,
      withCredentials: true,
    });
  }

  get(id: number) {
    return this.http.get<AdminProductDetail>(`${this.baseUrl}/admin/products/${id}`, {
      withCredentials: true,
    });
  }

  update(id: number, request: UpdateAdminProductRequest) {
    return this.http.put<AdminProductDetail>(`${this.baseUrl}/admin/products/${id}`, request, {
      withCredentials: true,
    });
  }

  create(request: CreateAdminProductRequest) {
    return this.http.post<AdminProductDetail>(`${this.baseUrl}/admin/products`, request, {
      withCredentials: true,
    });
  }

  uploadImage(productId: number, file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<AdminProductImage>(`${this.baseUrl}/admin/products/${productId}/images`, form, {
      withCredentials: true,
    });
  }

  uploadInfoImage(productId: number, file: File, imageInfo?: string) {
    const form = new FormData();
    form.append('file', file);
    if (imageInfo?.trim()) {
      form.append('imageInfo', imageInfo.trim());
    }
    return this.http.post<AdminProductImage>(`${this.baseUrl}/admin/products/${productId}/info-images`, form, {
      withCredentials: true,
    });
  }

  deleteImage(productId: number, imageId: number) {
    return this.http.delete<void>(`${this.baseUrl}/admin/products/${productId}/images/${imageId}`, {
      withCredentials: true,
    });
  }

  updateImageInfo(productId: number, imageId: number, imageInfo?: string) {
    return this.http.put<void>(
      `${this.baseUrl}/admin/products/${productId}/images/${imageId}/info`,
      { imageInfo },
      { withCredentials: true }
    );
  }

  getOptions(productId: number) {
    return this.http.get<AdminProductOption[]>(`${this.baseUrl}/admin/products/${productId}/options`, {
      withCredentials: true,
    });
  }

  addOption(productId: number, request: AddAdminProductOptionRequest) {
    return this.http.post<AdminProductOption>(`${this.baseUrl}/admin/products/${productId}/options`, request, {
      withCredentials: true,
    });
  }

  deleteOption(productId: number, optionId: number) {
    return this.http.delete<void>(`${this.baseUrl}/admin/products/${productId}/options/${optionId}`, {
      withCredentials: true,
    });
  }

  addOptionValue(productId: number, optionId: number, request: AddAdminProductOptionValueRequest) {
    return this.http.post(`${this.baseUrl}/admin/products/${productId}/options/${optionId}/values`, request, {
      withCredentials: true,
    });
  }

  deleteOptionValue(productId: number, valueId: number) {
    return this.http.delete<void>(`${this.baseUrl}/admin/products/${productId}/option-values/${valueId}`, {
      withCredentials: true,
    });
  }

  generateVariants(productId: number) {
    return this.http.post<GenerateVariantsResponse>(
      `${this.baseUrl}/admin/products/${productId}/variants/generate`,
      {},
      { withCredentials: true }
    );
  }

  updateVariant(productId: number, variantId: number, request: UpdateAdminProductVariantDetailRequest) {
    return this.http.put(`${this.baseUrl}/admin/products/${productId}/variants/${variantId}`, request, {
      withCredentials: true,
    });
  }

  uploadVariantImage(productId: number, variantId: number, file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ imageUrl: string }>(
      `${this.baseUrl}/admin/products/${productId}/variants/${variantId}/image`,
      form,
      { withCredentials: true }
    );
  }

  setProductType(productId: number, request: SetProductTypeRequest) {
    return this.http.put(`${this.baseUrl}/admin/products/${productId}/product-type`, request, {
      withCredentials: true,
    });
  }

  exportCsv(filter: AdminProductFilter = 'active', search?: string) {
    let params = new HttpParams().set('filter', filter);
    if (search?.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get(`${this.baseUrl}/admin/products/export-csv`, {
      params,
      withCredentials: true,
      responseType: 'blob',
    });
  }

  regenerateSlugs() {
    return this.http.post<RegenerateProductSlugsResponse>(
      `${this.baseUrl}/admin/products/regenerate-slugs`,
      {},
      { withCredentials: true }
    );
  }
}
