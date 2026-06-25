import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenService } from './token.service';
import { ApiResponse } from '../models/api-response.model';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';
import { KycStore } from '../stores/kyc.store';
import { FinancialStore } from '../stores/financial.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenService = inject(TokenService);
  private readonly kycStore = inject(KycStore);
  private readonly finStore = inject(FinancialStore);

  private readonly baseUrl = `${environment.apiUrl}/api/v1/auth`;

  readonly currentUser = signal<AuthResponse | null>(null);
  readonly isLoggedIn  = computed(() => this.currentUser() !== null);
  readonly isOwner     = computed(() => this.currentUser()?.roles.includes('Owner') ?? false);
  readonly isAdmin     = computed(() => this.currentUser()?.roles.includes('Admin') ?? false);

  constructor() {
    // Restore user from localStorage if token is still valid
    if (this.tokenService.isAuthenticated()) {
      const user = this.tokenService.getUser();
      if (user) this.currentUser.set(user);
    }
  }

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

  // Called after role changes (e.g. become-owner) to refresh JWT with new roles
  refreshToken(): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/refresh-token`, {}, { withCredentials: true }).pipe(
      tap(res => { if (res.success) this.persist(res.data); })
    );
  }

  logout(): void {
    this.kycStore.reset();
    this.finStore.reset();
    this.tokenService.clearToken();
    this.tokenService.clearUser();
    this.currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  private persist(data: AuthResponse): void {
    this.tokenService.setToken(data.accessToken);
    this.tokenService.setUser(data);
    this.currentUser.set(data);
  }
}
