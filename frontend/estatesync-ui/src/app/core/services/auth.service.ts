import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, of, tap, catchError, finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenService } from './token.service';
import { UserService } from './user.service';
import { NotificationService } from './notification.service';
import { ApiResponse } from '../models/api-response.model';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';
import { KycStore } from '../stores/kyc.store';
import { FinancialStore } from '../stores/financial.store';
import { SKIP_AUTH_HANDLING } from '../interceptors/http-context.tokens';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenService = inject(TokenService);
  private readonly userService = inject(UserService);
  private readonly notificationService = inject(NotificationService);
  private readonly kycStore = inject(KycStore);
  private readonly finStore = inject(FinancialStore);

  private readonly baseUrl = `${environment.apiUrl}/api/v1/auth`;

  readonly currentUser = signal<AuthResponse | null>(
    this.tokenService.isAuthenticated() ? this.tokenService.getUser() : null
  );
  readonly isLoggedIn  = computed(() => this.currentUser() !== null);
  readonly isOwner     = computed(() => this.currentUser()?.roles?.includes('Owner') ?? false);
  readonly isAdmin     = computed(() => this.currentUser()?.roles?.includes('Admin') ?? false);
  readonly isVerifier  = computed(() => this.currentUser()?.roles?.includes('Verifier') ?? false);

  login(request: LoginRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/login`, request).pipe(
      tap(res => { if (res.success) this.persist(res.data); })
    );
  }

  register(request: RegisterRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/register`, request).pipe(
      tap(res => { if (res.success) this.persist(res.data); })
    );
  }

  /**
   * Runs once at app startup (APP_INITIALIZER). If a previous session existed,
   * silently exchanges the httpOnly refresh cookie for a fresh in-memory access
   * token so a hard reload keeps the user logged in. Failures resolve quietly to
   * a logged-out state and never block bootstrap.
   */
  bootstrap(): Observable<unknown> {
    if (!this.tokenService.hasSessionHint()) return of(null);
    return this.http.post<ApiResponse<AuthResponse>>(
      `${this.baseUrl}/refresh-token`, {},
      { withCredentials: true, context: new HttpContext().set(SKIP_AUTH_HANDLING, true) }
    ).pipe(
      tap(res => { if (res.success) this.persist(res.data); }),
      catchError(() => { this.tokenService.clearSessionHint(); return of(null); })
    );
  }

  // Called after role changes (e.g. become-owner) to refresh JWT with new roles
  refreshToken(): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/refresh-token`, {}, { withCredentials: true }).pipe(
      tap(res => { if (res.success) this.persist(res.data); })
    );
  }

  // Syncs isKycVerified / isOwnerVerified from DB without needing the refresh-token cookie
  syncProfile(): Observable<ApiResponse<{ isKycVerified: boolean; isOwnerVerified: boolean }>> {
    return this.userService.getProfile().pipe(
      tap(res => {
        if (res.success && res.data) {
          const current = this.currentUser();
          if (current) {
            const updated = { ...current, isKycVerified: res.data.isKycVerified, isOwnerVerified: res.data.isOwnerVerified };
            this.tokenService.setUser(updated);
            this.currentUser.set(updated);
          }
        }
      })
    );
  }

  logout(): void {
    // Revoke the refresh-token family server-side and clear the cookie. SKIP_AUTH_HANDLING
    // stops a logout 401 from looping through the error interceptor. Local teardown runs on
    // both success and failure so logout always completes even if the network call fails.
    this.http.post<ApiResponse<object>>(
      `${this.baseUrl}/logout`, {},
      { withCredentials: true, context: new HttpContext().set(SKIP_AUTH_HANDLING, true) }
    ).pipe(
      finalize(() => this.clearClientState())
    ).subscribe({ error: () => { /* teardown handled in finalize */ } });
  }

  private clearClientState(): void {
    this.kycStore.reset();
    this.finStore.reset();
    this.notificationService.clear();
    this.tokenService.clearToken();
    this.tokenService.clearUser();
    this.tokenService.clearSessionHint();
    this.currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  private persist(data: AuthResponse): void {
    this.tokenService.setToken(data.accessToken);
    this.tokenService.setSessionHint();
    // Roles are authoritative from the freshly-issued JWT, not from the response body
    // (and never from the localStorage cache) — guards against client-side tampering.
    const roles = this.tokenService.decodeRoles();
    const user: AuthResponse = { ...data, roles: roles.length ? roles : data.roles };
    this.tokenService.setUser(user);
    this.currentUser.set(user);
  }
}
