import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { WishlistResponse, WishlistToggleResponse } from '../models/wishlist.models';

@Injectable({ providedIn: 'root' })
export class WishlistService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  private readonly countSignal = signal(0);
  private readonly productIdsSignal = signal<Set<number>>(new Set());

  readonly count = computed(() => this.countSignal());
  readonly productIds = computed(() => this.productIdsSignal());

  isInWishlist(productId: number): boolean {
    return this.productIdsSignal().has(productId);
  }

  load() {
    return this.http
      .get<WishlistResponse>(`${this.baseUrl}/wishlist`, { withCredentials: true })
      .pipe(
        tap((res) => {
          this.countSignal.set(res.count);
          this.productIdsSignal.set(new Set(res.items.map((i) => i.productId)));
        })
      );
  }

  loadProductIds() {
    return this.http
      .get<{ productIds: number[] }>(`${this.baseUrl}/wishlist/product-ids`, { withCredentials: true })
      .pipe(
        tap((res) => {
          this.productIdsSignal.set(new Set(res.productIds));
          this.countSignal.set(res.productIds.length);
        })
      );
  }

  getWishlist() {
    return this.http.get<WishlistResponse>(`${this.baseUrl}/wishlist`, { withCredentials: true });
  }

  toggle(productId: number) {
    return this.http
      .post<WishlistToggleResponse>(`${this.baseUrl}/wishlist/toggle`, { productId }, { withCredentials: true })
      .pipe(
        tap((res) => {
          this.countSignal.set(res.wishlistCount);
          const next = new Set(this.productIdsSignal());
          if (res.isInWishlist) next.add(productId);
          else next.delete(productId);
          this.productIdsSignal.set(next);
        })
      );
  }

  remove(wishlistItemId: number) {
    return this.http.delete(`${this.baseUrl}/wishlist/${wishlistItemId}`, { withCredentials: true });
  }

  clearLocal() {
    this.countSignal.set(0);
    this.productIdsSignal.set(new Set());
  }
}
