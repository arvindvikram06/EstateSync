import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { LeaseService } from './lease.service';

const BASE       = '/api/v1/leases';
const ADMIN_BASE = '/api/v1/admin';

describe('LeaseService', () => {
  let service: LeaseService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(LeaseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs leases for the tenant', () => {
    service.getMyLeases().subscribe();

    const req = httpMock.expectOne(`${BASE}/tenant`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  it('GETs leases for the owner', () => {
    service.getOwnerLeases().subscribe();

    const req = httpMock.expectOne(`${BASE}/owner`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  it('GETs admin lease list with page and limit params', () => {
    service.getAdminLeases(2, 25).subscribe();

    const req = httpMock.expectOne(`${BASE}?page=2&limit=25`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [], page: 2, limit: 25, total: 0 });
  });

  it('GETs a single lease by id', () => {
    let result: unknown;
    service.getLeaseDetail('lease-1').subscribe(r => (result = r));

    const req = httpMock.expectOne(`${BASE}/lease-1`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { leaseId: 'lease-1' } });

    expect((result as any).data.leaseId).toBe('lease-1');
  });

  it('POSTs a new lease creation request', () => {
    const body = { applicationId: 'app-1', startDate: '2025-01-01', endDate: '2026-01-01', monthlyRent: 20000, depositAmount: 40000 } as any;
    service.createLease(body).subscribe();

    const req = httpMock.expectOne(BASE);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: {} });
  });

  it('PUTs signature text and image to the sign endpoint', () => {
    const body = { signatureText: 'Jane Doe', signatureImageBase64: 'data-base64' };
    service.signLease('lease-1', body).subscribe();

    const req = httpMock.expectOne(`${BASE}/lease-1/sign`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: {} });
  });

  it('POSTs a void reason to cancel a lease', () => {
    const body = { reason: 'Changed plans' };
    service.voidLease('lease-1', body).subscribe();

    const req = httpMock.expectOne(`${BASE}/lease-1/void`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: null });
  });

  it('POSTs a termination reason to the terminate endpoint', () => {
    const body = { reason: 'Property sold', terminationDate: '2025-06-01' } as any;
    service.terminateLease('lease-1', body).subscribe();

    const req = httpMock.expectOne(`${BASE}/lease-1/terminate`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: null });
  });

  it('PUTs admin approval to the admin leases endpoint', () => {
    const body = { approve: true, remarks: 'All documents verified' };
    service.adminApproveLease('lease-1', body).subscribe();

    const req = httpMock.expectOne(`${ADMIN_BASE}/leases/lease-1/approval`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: null });
  });
});
