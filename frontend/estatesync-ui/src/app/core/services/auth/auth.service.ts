import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, of, tap, catchError, finalize, take } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { TokenService } from './token.service';
import { UserService } from '@core/services/admin/user.service';
import { NotificationService } from '@core/services/shared/notification.service';
import { ApiResponse } from '../../models/api-response.model';
import { AuthResponse, LoginRequest, RegisterRequest, UserProfileResponse } from '../../models/auth.model';
import { KycStore } from '../../stores/kyc.store';
import { FinancialStore } from '../../stores/financial.store';
import { SKIP_AUTH_HANDLING } from '../../interceptors/http-context.tokens';

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

  // Identity — held in memory only; repopulated by bootstrap()'s silent
  // refresh before first render, so no localStorage cache is needed.
  readonly currentUser = signal<AuthResponse | null>(null);

  // Verification state — fetched from profile endpoint after every auth event
  private readonly _verificationState = signal<UserProfileResponse | null>(null);

  readonly isLoggedIn      = computed(() => this.currentUser() !== null);
  readonly isOwner         = computed(() => this.currentUser()?.roles?.includes('Owner') ?? false);
  readonly isAdmin         = computed(() => this.currentUser()?.roles?.includes('Admin') ?? false);
  readonly isVerifier      = computed(() => this.currentUser()?.roles?.includes('Verifier') ?? false);
  readonly isKycVerified   = computed(() => this._verificationState()?.isKycVerified   ?? false);
  readonly isOwnerVerified = computed(() => this._verificationState()?.isOwnerVerified ?? false);

  login(request: LoginRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/login`, request).pipe(
      tap(res => { if (res.success) this.persist(res.data); })
    );
  }

  // Registration no longer signs the user in — they must verify their email first.
  register(request: RegisterRequest): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/register`, request);
  }

  verifyEmail(token: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/verify-email`, { token });
  }

  resendVerification(email: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/resend-verification`, { email });
  }

  /**
   * Runs once at app startup (APP_INITIALIZER). If a previous session existed,
   * silently exchanges the httpOnly refresh cookie for a fresh in-memory access
   * token so a hard reload keeps the user logged in. Failures resolve quietly to
   * a logged-out state and never block bootstrap.
   */
  bootstrap(): Observable<unknown> {
    // Legacy cleanup — the profile cache was removed (held PII and, at one
    // point, the access token). Safe to delete this line after a few releases.
    localStorage.removeItem('es_user_profile');
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

  // Syncs verification state from DB — called by SignalR on approval events and by Stripe callback
  syncProfile(): Observable<ApiResponse<UserProfileResponse>> {
    return this.userService.getProfile().pipe(
      tap(res => { if (res.success && res.data) this._verificationState.set(res.data); })
    );
  }

  /** @param navigateToLogin false when the caller stays on (or handles) its own page */
  logout(navigateToLogin = true): void {
    this.http.post<ApiResponse<object>>(
      `${this.baseUrl}/logout`, {},
      { withCredentials: true, context: new HttpContext().set(SKIP_AUTH_HANDLING, true) }
    ).pipe(
      finalize(() => this.clearClientState(navigateToLogin))
    ).subscribe({ error: () => { /* teardown handled in finalize */ } });
  }

  private clearClientState(navigateToLogin = true): void {
    this.kycStore.reset();
    this.finStore.reset();
    this.notificationService.clear();
    this.tokenService.clearToken();
    this.tokenService.clearSessionHint();
    this.currentUser.set(null);
    this._verificationState.set(null);
    if (navigateToLogin) this.router.navigate(['/auth/login']);
  }

  private persist(data: AuthResponse): void {
    this.tokenService.setToken(data.accessToken);
    this.tokenService.setSessionHint();
    // Roles are authoritative from the freshly-issued JWT, not from the response body.
    // Keep the raw token out of the signal too — TokenService owns it.
    const roles = this.tokenService.decodeRoles();
    const user: AuthResponse = { ...data, accessToken: '', roles: roles.length ? roles : data.roles };
    this.currentUser.set(user);
    // Fire-and-forget — fetch verification state after every auth event
    this.userService.getProfile().pipe(take(1)).subscribe({
      next: (res) => { if (res.success && res.data) this._verificationState.set(res.data); },
      error: () => {}
    });
  }
}
