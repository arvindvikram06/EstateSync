import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { UserService } from './user.service';

const BASE = '/api/v1/users';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs the current user profile', () => {
    let result: unknown;
    service.getProfile().subscribe(r => (result = r));

    const req = httpMock.expectOne(`${BASE}/me/profile`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { userId: 'u-1', email: 'jane@example.com' } });

    expect((result as any).data.email).toBe('jane@example.com');
  });

  it('POSTs to the become-owner endpoint', () => {
    const body = { stripeAccountId: 'acct_123' } as any;
    service.becomeOwner(body).subscribe();

    const req = httpMock.expectOne(`${BASE}/me/become-owner`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: {} });
  });
});
