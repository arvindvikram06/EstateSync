import { TokenService } from './token.service';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

/**
 * Build a real (unsigned) JWT so the actual jwt-decode library parses it.
 * Avoids vi.mock('jwt-decode'), which was flaky: on a cold Vite dep-optimizer
 * cache the module mock intermittently failed to intercept the real module.
 */
function makeToken(payload: Record<string, unknown>): string {
  const b64url = (obj: object) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${b64url({ alg: 'none', typ: 'JWT' })}.${b64url(payload)}.sig`;
}

describe('TokenService', () => {
  let service: TokenService;

  beforeEach(() => {
    localStorage.clear();
    service = new TokenService();
  });

  describe('access token (in-memory)', () => {
    it('stores and returns the token in memory, never in localStorage', () => {
      service.setToken('abc');
      expect(service.getToken()).toBe('abc');
      expect(localStorage.getItem('es_access_token')).toBeNull();
    });

    it('clears the token', () => {
      service.setToken('abc');
      service.clearToken();
      expect(service.getToken()).toBeNull();
    });
  });

  describe('isAuthenticated', () => {
    it('is false when no token is present', () => {
      expect(service.isAuthenticated()).toBe(false);
    });

    it('is true for a non-expired token', () => {
      service.setToken(makeToken({ exp: Date.now() / 1000 + 3600 }));
      expect(service.isAuthenticated()).toBe(true);
    });

    it('is false for an expired token', () => {
      service.setToken(makeToken({ exp: Date.now() / 1000 - 10 }));
      expect(service.isAuthenticated()).toBe(false);
    });

    it('is false when the token cannot be decoded', () => {
      service.setToken('garbage');
      expect(service.isAuthenticated()).toBe(false);
    });
  });

  describe('decodeRoles', () => {
    it('returns [] when no token is present', () => {
      expect(service.decodeRoles()).toEqual([]);
    });

    it('reads the short "role" claim that the JWT actually carries', () => {
      service.setToken(makeToken({ role: 'Owner' }));
      expect(service.decodeRoles()).toEqual(['Owner']);
    });

    it('returns an array claim as-is', () => {
      service.setToken(makeToken({ [ROLE_CLAIM]: ['Admin', 'Owner'] }));
      expect(service.decodeRoles()).toEqual(['Admin', 'Owner']);
    });

    it('wraps a single-string claim in an array', () => {
      service.setToken(makeToken({ [ROLE_CLAIM]: 'Tenant' }));
      expect(service.decodeRoles()).toEqual(['Tenant']);
    });

    it('returns [] when the role claim is absent', () => {
      service.setToken(makeToken({ sub: '123' }));
      expect(service.decodeRoles()).toEqual([]);
    });
  });

  describe('session hint', () => {
    it('round-trips and clears', () => {
      expect(service.hasSessionHint()).toBe(false);
      service.setSessionHint();
      expect(service.hasSessionHint()).toBe(true);
      service.clearSessionHint();
      expect(service.hasSessionHint()).toBe(false);
    });
  });
});
