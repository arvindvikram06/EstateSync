import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { AuthResponse } from '../models/auth.model';

interface JwtPayload {
  sub: string;
  email: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly TOKEN_KEY = 'es_access_token';
  private readonly USER_KEY  = 'es_user_profile';

  // ── Token ──────────────────────────────────────────────────
  getToken(): string | null  { return localStorage.getItem(this.TOKEN_KEY); }
  setToken(token: string)    { localStorage.setItem(this.TOKEN_KEY, token); }
  clearToken()               { localStorage.removeItem(this.TOKEN_KEY); }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const { exp } = jwtDecode<JwtPayload>(token);
      return Date.now() / 1000 < exp;
    } catch { return false; }
  }

  // ── User profile ───────────────────────────────────────────
  getUser(): AuthResponse | null {
    const raw = localStorage.getItem(this.USER_KEY);
    if (!raw) return null;
    try { return JSON.parse(raw) as AuthResponse; } catch { return null; }
  }

  setUser(user: AuthResponse) { localStorage.setItem(this.USER_KEY, JSON.stringify(user)); }
  clearUser()                 { localStorage.removeItem(this.USER_KEY); }

  // ── Role helpers ───────────────────────────────────────────
  decodeRoles(): string[] {
    const token = this.getToken();
    if (!token) return [];
    try {
      const payload = jwtDecode<JwtPayload>(token);
      const claim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (!claim) return [];
      return Array.isArray(claim) ? claim : [claim];
    } catch { return []; }
  }

  hasRole(role: string): boolean { return this.decodeRoles().includes(role); }
}
