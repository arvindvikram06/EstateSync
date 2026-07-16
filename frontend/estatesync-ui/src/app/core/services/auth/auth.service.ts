import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, of, tap, catchError, finalize, take, switchMap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { TokenService } from './token.service';
import { UserService } from '@core/services/admin/user.service';
import { NotificationService } from '@core/services/shared/notification.service';
import { ApiResponse } from '../../models/api-response.model';
import { AuthResponse, LoginRequest, RegisterRequest, UserProfileResponse } from '../../models/auth.model';
import { KycStore } from '../../stores/kyc.store';
import { SavedPropertyService } from '@core/services/property/saved-property.service';
import { SKIP_AUTH_HANDLING } from '../../interceptors/http-context.tokens';
import { DataRefreshService } from '@core/services/shared/data-refresh.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenService = inject(TokenService);
  private readonly userService = inject(UserService);
  private readonly notificationService = inject(NotificationService);
  private readonly kycStore = inject(KycStore);
  private readonly savedPropertyService = inject(SavedPropertyService);
  private readonly dataRefresh = inject(DataRefreshService);

  private readonly baseUrl = `${environment.apiUrl}/api/v1/auth`;

  constructor() {
    this.dataRefresh.on('profile').pipe(switchMap(() => this.syncProfile())).subscribe();
  }

  readonly currentUser = signal<AuthResponse | null>(null);
  private readonly _verificationState = signal<UserProfileResponse | null>(null);

  readonly isLoggedIn      = computed(() => this.currentUser() !== null);
  readonly isOwner         = computed(() => this.currentUser()?.roles?.includes('Owner') ?? false);
  readonly isAdmin         = computed(() => this.currentUser()?.roles?.includes('Admin') ?? false);
  readonly isVerifier      = computed(() => this.currentUser()?.roles?.includes('Verifier') ?? false);
  readonly isKycVerified   = computed(() => this._verificationState()?.isKycVerified   ?? false);
  readonly isOwnerVerified = computed(() => this._verificationState()?.isOwnerVerified ?? false);

  login(request: LoginRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/login`, request, { withCredentials: true }).pipe(
      tap(res => { if (res.success) { this.tokenService.setSessionHint(); this.persist(res.data); } })
    );
  }

  register(request: RegisterRequest): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/register`, request);
  }

  verifyEmail(token: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/verify-email`, { token });
  }

  resendVerification(email: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/resend-verification`, { email });
  }

  forgotPassword(email: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/reset-password`, { token, newPassword });
  }

  // Runs once at app startup. If a valid token is in localStorage, restores
  // session instantly from the JWT. Falls back to a cookie refresh otherwise.
  bootstrap(): Observable<unknown> {
    if (this.tokenService.isAuthenticated()) {
      const user = this.tokenService.decodeUser();
      if (user) {
        this.currentUser.set({ ...user, accessToken: '' });
        this.syncProfile().pipe(take(1)).subscribe();
        return of(null);
      }
    }
    if (!this.tokenService.hasSessionHint()) {
      return of(null);
    }
    return this.http.post<ApiResponse<AuthResponse>>(
      `${this.baseUrl}/refresh-token`, {},
      { withCredentials: true, context: new HttpContext().set(SKIP_AUTH_HANDLING, true) }
    ).pipe(
      tap(res => { if (res.success) this.persist(res.data); }),
      catchError(() => { this.tokenService.clearToken(); this.tokenService.clearSessionHint(); return of(null); })
    );
  }

  // Used by errorInterceptor on 401 and after role changes (e.g. become-owner).
  refreshToken(): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/refresh-token`, {}, { withCredentials: true }).pipe(
      tap(res => { if (res.success) this.persist(res.data); })
    );
  }

  // Re-fetches KYC/owner verification state — called after approval events.
  syncProfile(): Observable<ApiResponse<UserProfileResponse>> {
    return this.userService.getProfile().pipe(
      tap(res => { if (res.success && res.data) this._verificationState.set(res.data); })
    );
  }

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
    this.notificationService.clear();
    this.savedPropertyService.clear();
    this.tokenService.clearToken();
    this.tokenService.clearSessionHint();
    this.currentUser.set(null);
    this._verificationState.set(null);
    if (navigateToLogin) this.router.navigate(['/auth/login']);
  }

  private persist(data: AuthResponse): void {
    const rawData = data as any;
    const token = data.accessToken || '';
    const userId = data.userId || '';
    const email = data.email || rawData.Email || '';
    const fullName = data.fullName || rawData.FullName || '';
    const roles = data.roles || rawData.Roles || [];

    this.tokenService.setToken(token);
    const decoded = this.tokenService.decodeUser();
    
    const user: AuthResponse = {
      accessToken: '',
      userId: userId,
      email: email,
      roles: decoded?.roles?.length ? decoded.roles : roles,
      fullName: decoded?.fullName || fullName,
    };
    
    this.currentUser.set(user);
    this.userService.getProfile().pipe(take(1)).subscribe({
      next: (res) => { if (res.success && res.data) this._verificationState.set(res.data); },
      error: () => {}
    });
  }
}
