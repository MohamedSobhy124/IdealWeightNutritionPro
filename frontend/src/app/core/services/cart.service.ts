import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AddCartItemRequest, CartResponse } from '../models/cart.models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly httpOptions = { withCredentials: true };

  private readonly cartSignal = signal<CartResponse | null>(null);
  readonly cart = this.cartSignal.asReadonly();
  readonly itemCount = () => this.cartSignal()?.itemCount ?? 0;

  load() {
    return this.http
      .get<CartResponse>(`${this.baseUrl}/cart`, this.httpOptions)
      .pipe(tap((cart) => this.cartSignal.set(cart)));
  }

  addItem(request: AddCartItemRequest) {
    return this.http
      .post<CartResponse>(`${this.baseUrl}/cart/items`, request, this.httpOptions)
      .pipe(tap((cart) => this.cartSignal.set(cart)));
  }

  updateQuantity(lineId: string, quantity: number) {
    return this.http
      .put<CartResponse>(`${this.baseUrl}/cart/items/${lineId}`, { quantity }, this.httpOptions)
      .pipe(tap((cart) => this.cartSignal.set(cart)));
  }

  removeItem(lineId: string) {
    return this.http
      .delete<CartResponse>(`${this.baseUrl}/cart/items/${lineId}`, this.httpOptions)
      .pipe(tap((cart) => this.cartSignal.set(cart)));
  }

  clear() {
    return this.http
      .delete<CartResponse>(`${this.baseUrl}/cart`, this.httpOptions)
      .pipe(tap((cart) => this.cartSignal.set(cart)));
  }

  applyPromo(code: string) {
    return this.http
      .post<CartResponse>(`${this.baseUrl}/cart/promo`, { code }, this.httpOptions)
      .pipe(tap((cart) => this.cartSignal.set(cart)));
  }

  removePromo() {
    return this.http
      .delete<CartResponse>(`${this.baseUrl}/cart/promo`, this.httpOptions)
      .pipe(tap((cart) => this.cartSignal.set(cart)));
  }
}
