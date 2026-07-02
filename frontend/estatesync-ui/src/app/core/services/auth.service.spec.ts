import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { TokenService } from './token.service';
import { UserService } from './user.service';
import { NotificationService } from './notification.service';
import { KycStore } from '../stores/kyc.store';
import { FinancialStore } from '../stores/financial.store';
import { AuthResponse } from '../models/auth.model';

const REFRESH_URL = '/api/v1/auth/refresh-token';
const LOGOUT_URL = '/api/v1/auth/logout';

function authResponse(): AuthResponse {
  return {
    accessToken: 'access-token-123',
    userId: 'u1',
    fullName: 'Jane Doe',
    email: 'jane@example.com',
    roles: ['Tenant'],
    isKycVerified: false,
    isOwnerVerified: false,
  };
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let tokenService: TokenService;
  let router: { navigate: ReturnType<typeof vi.fn> };
  let kycStore: { reset: ReturnType<typeof vi.fn> };
  let finStore: { reset: ReturnType<typeof vi.fn> };
  let notifications: { clear: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    localStorage.clear();
    router = { navigate: vi.fn() };
    kycStore = { reset: vi.fn() };
    finStore = { reset: vi.fn() };
    notifications = { clear: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        TokenService,
        { provide: Router, useValue: router },
        { provide: KycStore, useValue: kycStore },
        { provide: FinancialStore, useValue: finStore },
        { provide: NotificationService, useValue: notifications },
        { provide: UserService, useValue: { getProfile: vi.fn() } },
      ],
    });

    service = TestBed.inject(AuthService);
    tokenService = TestBed.inject(TokenService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('bootstrap', () => {
    it('refreshes and persists when a session hint exists', () => {
      tokenService.setSessionHint();

      service.bootstrap().subscribe();

      const req = httpMock.expectOne(REFRESH_URL);
      expect(req.request.method).toBe('POST');
      expect(req.request.withCredentials).toBe(true);
      req.flush({ success: true, message: '', data: authResponse() });

      expect(tokenService.getToken()).toBe('access-token-123');
      expect(service.currentUser()?.email).toBe('jane@example.com');
      expect(service.isLoggedIn()).toBe(true);
    });

    it('makes no network call when there is no session hint', () => {
      service.bootstrap().subscribe();
      httpMock.expectNone(REFRESH_URL);
      expect(service.isLoggedIn()).toBe(false);
    });
  });

  describe('logout', () => {
    it('posts to the logout endpoint and clears client state', () => {
      // Seed an authenticated state.
      tokenService.setToken('access-token-123');
      tokenService.setSessionHint();

      service.logout();

      const req = httpMock.expectOne(LOGOUT_URL);
      expect(req.request.method).toBe('POST');
      expect(req.request.withCredentials).toBe(true);
      req.flush({ success: true, message: '', data: {} });

      expect(tokenService.getToken()).toBeNull();
      expect(tokenService.hasSessionHint()).toBe(false);
      expect(service.currentUser()).toBeNull();
      expect(kycStore.reset).toHaveBeenCalled();
      expect(finStore.reset).toHaveBeenCalled();
      expect(notifications.clear).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });

    it('still clears client state when the logout call fails', () => {
      tokenService.setToken('access-token-123');

      service.logout();

      httpMock.expectOne(LOGOUT_URL).flush(
        { success: false, message: 'boom', data: {} },
        { status: 500, statusText: 'Server Error' }
      );

      expect(tokenService.getToken()).toBeNull();
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });
  });
});
