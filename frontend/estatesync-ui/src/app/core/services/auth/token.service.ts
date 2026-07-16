import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { AuthResponse } from '../../models/auth.model';

interface JwtPayload {
  sub?: string;
  nameid?: string;
  email?: string;
  name?: string;
  unique_name?: string;
  role?: string | string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly TOKEN_KEY   = 'es_access_token';
  private readonly SESSION_KEY = 'es_has_session';

  getToken(): string | null  { return localStorage.getItem(this.TOKEN_KEY); }
  setToken(token: string)    { localStorage.setItem(this.TOKEN_KEY, token); }
  clearToken()               { localStorage.removeItem(this.TOKEN_KEY); }

  hasSessionHint(): boolean  { return localStorage.getItem(this.SESSION_KEY) === '1'; }
  setSessionHint(): void     { localStorage.setItem(this.SESSION_KEY, '1'); }
  clearSessionHint(): void   { localStorage.removeItem(this.SESSION_KEY); }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const { exp } = jwtDecode<JwtPayload>(token);
      return Date.now() / 1000 < exp;
    } catch { return false; }
  }

  decodeUser(): Omit<AuthResponse, 'accessToken'> | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const p = jwtDecode<JwtPayload>(token);
      const claim = p.role ?? p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const roles = !claim ? [] : Array.isArray(claim) ? claim : [claim];
      return { 
        userId: p.sub ?? p.nameid ?? '', 
        email: p.email ?? '', 
        fullName: p.name ?? p.unique_name ?? '', 
        roles 
      };
    } catch { return null; }
  }

  decodeRoles(): string[] {
    return this.decodeUser()?.roles ?? [];
  }

  hasRole(role: string): boolean { return this.decodeRoles().includes(role); }
}
