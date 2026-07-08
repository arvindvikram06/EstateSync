import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { AuthResponse } from '../../models/auth.model';

interface JwtPayload {
  sub: string;
  email: string;
  role?: string | string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class TokenService {
  private readonly SESSION_KEY = 'es_has_session';

  /**
   * The access token lives in memory only — never in localStorage — so it cannot
   * be exfiltrated by injected scripts (XSS). It is repopulated on a hard reload
   * by the app-bootstrap silent refresh (which uses the httpOnly refresh cookie).
   */
  private accessToken: string | null = null;

  // ── Token ──────────────────────────────────────────────────
  getToken(): string | null { return this.accessToken; }
  setToken(token: string) { this.accessToken = token; }
  clearToken() { this.accessToken = null; }

  isAuthenticated(): boolean {
    const token = this.accessToken;
    if (!token) return false;
    try {
      const { exp } = jwtDecode<JwtPayload>(token);
      return Date.now() / 1000 < exp;
    } catch { return false; }
  }

  // ── Session hint ───────────────────────────────────────────
  // Non-sensitive boolean marker so bootstrap only attempts a silent refresh
  // when a session previously existed (avoids a 401 round-trip for anonymous visitors).
  hasSessionHint(): boolean { return localStorage.getItem(this.SESSION_KEY) === '1'; }
  setSessionHint() { localStorage.setItem(this.SESSION_KEY, '1'); }
  clearSessionHint() { localStorage.removeItem(this.SESSION_KEY); }

  // ── Role helpers ───────────────────────────────────────────
  // Roles are always decoded from the in-memory JWT — never trusted from
  // client-side storage, which a user could tamper with.
  decodeRoles(): string[] {
    const token = this.accessToken;
    if (!token) return [];
    try {
      const payload = jwtDecode<JwtPayload>(token);
      // The JWT serializes ClaimTypes.Role to the short "role" key; fall back to the
      // long Microsoft URI for safety.
      const claim = payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (!claim) return [];
      return Array.isArray(claim) ? claim : [claim];
    } catch { return []; }
  }

  hasRole(role: string): boolean { return this.decodeRoles().includes(role); }
}
