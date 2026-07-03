import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  AuthTokenResponse,
  ForgotPasswordRequest,
  LoginRequest,
  MessageResponse,
  PersonalDataExportResponse,
  RegisterRequest,
  ResetPasswordRequest,
  ChangePasswordRequest,
  UserProfile,
} from '../models/auth.models';

const ACCESS_TOKEN_KEY = 'iwn_access_token';
const REFRESH_TOKEN_KEY = 'iwn_refresh_token';
const EXPIRES_AT_KEY = 'iwn_expires_at';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly baseUrl = environment.apiBaseUrl;
  private readonly authHttpOptions = { withCredentials: true };

  private readonly accessTokenSignal = signal<string | null>(this.read(ACCESS_TOKEN_KEY));
  private readonly refreshTokenSignal = signal<string | null>(this.read(REFRESH_TOKEN_KEY));
  private readonly profileSignal = signal<UserProfile | null>(null);

  readonly accessToken = computed(() => this.accessTokenSignal());
  readonly refreshToken = computed(() => this.refreshTokenSignal());
  readonly profile = computed(() => this.profileSignal());
  readonly isAuthenticated = computed(() => !!this.accessTokenSignal());

  login(request: LoginRequest) {
    return this.http
      .post<AuthTokenResponse>(`${this.baseUrl}/auth/login`, request, this.authHttpOptions)
      .pipe(tap((tokens) => this.persistTokens(tokens)));
  }

  register(request: RegisterRequest) {
    return this.http
      .post<AuthTokenResponse>(`${this.baseUrl}/auth/register`, request, this.authHttpOptions)
      .pipe(tap((tokens) => this.persistTokens(tokens)));
  }

  sendRegistrationOtp(email: string) {
    return this.http.post<MessageResponse>(`${this.baseUrl}/auth/register/send-otp`, { email });
  }

  verifyRegistrationOtp(email: string, otp: string) {
    return this.http.post<MessageResponse>(`${this.baseUrl}/auth/register/verify-otp`, { email, otp });
  }

  refresh() {
    const refreshToken = this.refreshTokenSignal();
    if (!refreshToken) {
      throw new Error('No refresh token');
    }
    return this.http
      .post<AuthTokenResponse>(`${this.baseUrl}/auth/refresh`, { refreshToken })
      .pipe(tap((tokens) => this.persistTokens(tokens)));
  }

  loadProfile() {
    return this.http.get<UserProfile>(`${this.baseUrl}/auth/me`).pipe(
      tap((profile) => this.profileSignal.set(profile))
    );
  }

  forgotPassword(request: ForgotPasswordRequest) {
    return this.http.post<MessageResponse>(`${this.baseUrl}/auth/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest) {
    return this.http.post<MessageResponse>(`${this.baseUrl}/auth/reset-password`, request);
  }

  changePassword(request: ChangePasswordRequest) {
    return this.http.post<MessageResponse>(`${this.baseUrl}/auth/change-password`, request, this.authHttpOptions);
  }

  exportPersonalData() {
    return this.http.get<PersonalDataExportResponse>(`${this.baseUrl}/auth/personal-data`, this.authHttpOptions);
  }

  deletePersonalData() {
    return this.http.delete<MessageResponse>(`${this.baseUrl}/auth/personal-data`, this.authHttpOptions);
  }

  logout() {
    const refreshToken = this.refreshTokenSignal();
    if (refreshToken) {
      this.http.post(`${this.baseUrl}/auth/logout`, { refreshToken }).subscribe();
    }
    this.clear();
    this.router.navigate(['/auth/login']);
  }

  persistTokens(tokens: AuthTokenResponse): void {
    this.accessTokenSignal.set(tokens.accessToken);
    this.refreshTokenSignal.set(tokens.refreshToken);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
      localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
      localStorage.setItem(EXPIRES_AT_KEY, tokens.expiresAt);
    }
    this.loadProfile().subscribe();
  }

  loginWithGoogle(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const returnUrl = `${window.location.origin}/auth/oauth-callback`;
    const startUrl = `${environment.apiBaseUrl}/auth/external/google?returnUrl=${encodeURIComponent(returnUrl)}`;
    window.location.href = startUrl;
  }

  clear(): void {
    this.accessTokenSignal.set(null);
    this.refreshTokenSignal.set(null);
    this.profileSignal.set(null);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(ACCESS_TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      localStorage.removeItem(EXPIRES_AT_KEY);
    }
  }

  private read(key: string): string | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }
    return localStorage.getItem(key);
  }
}
