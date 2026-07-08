import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { AdminService } from './admin.service';

const BASE = '/api/v1/admin';

describe('AdminService', () => {
  let service: AdminService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(AdminService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // ── Stats ──────────────────────────────────────────────────────────────────

  it('GETs dashboard statistics', () => {
    service.getDashboardStats().subscribe();

    const req = httpMock.expectOne(`${BASE}/dashboard/stats`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { totalUsers: 10 } });
  });

  // ── KYC ────────────────────────────────────────────────────────────────────

  it('GETs KYC requests with default page and limit', () => {
    service.getKycRequests().subscribe();

    const req = httpMock.expectOne(r => r.url === `${BASE}/kyc-requests`);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('limit')).toBe('20');
    req.flush({ success: true, data: [], page: 1, limit: 20, total: 0 });
  });

  it('includes status filter param in KYC requests when provided', () => {
    service.getKycRequests('Pending').subscribe();

    const req = httpMock.expectOne(r => r.url === `${BASE}/kyc-requests`);
    expect(req.request.params.get('status')).toBe('Pending');
    req.flush({ success: true, data: [], page: 1, limit: 20, total: 0 });
  });

  it('GETs a single KYC request by id', () => {
    service.getKycRequestById('kyc-1').subscribe();

    const req = httpMock.expectOne(`${BASE}/kyc-requests/kyc-1`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: {} });
  });

  it('PUTs a KYC review decision', () => {
    const body = { approved: true, remarks: 'Documents look good' } as any;
    service.reviewKyc('kyc-1', body).subscribe();

    const req = httpMock.expectOne(`${BASE}/kyc-requests/kyc-1/review`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: {} });
  });

  // ── Financial Profiles ─────────────────────────────────────────────────────

  it('GETs financial profiles with optional status filter', () => {
    service.getFinancialProfiles('Approved').subscribe();

    const req = httpMock.expectOne(r => r.url === `${BASE}/financial-profiles`);
    expect(req.request.params.get('status')).toBe('Approved');
    req.flush({ success: true, data: [], page: 1, limit: 20, total: 0 });
  });

  it('PUTs a financial profile review decision', () => {
    const body = { approved: false, remarks: 'Insufficient income proof' } as any;
    service.reviewFinancialProfile('fp-1', body).subscribe();

    const req = httpMock.expectOne(`${BASE}/financial-profiles/fp-1/review`);
    expect(req.request.method).toBe('PUT');
    req.flush({ success: true, data: {} });
  });

  // ── Properties ─────────────────────────────────────────────────────────────

  it('GETs properties pending admin approval', () => {
    service.getPendingProperties().subscribe();

    const req = httpMock.expectOne(r => r.url === `${BASE}/properties/pending-approval`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [], page: 1, limit: 20, total: 0 });
  });

  it('PUTs a property approval decision', () => {
    const body = { approved: true, remarks: 'Verified ownership docs' } as any;
    service.approveProperty('prop-1', body).subscribe();

    const req = httpMock.expectOne(`${BASE}/properties/prop-1/approval`);
    expect(req.request.method).toBe('PUT');
    req.flush({ success: true, data: {} });
  });

  // ── Lease Approval ─────────────────────────────────────────────────────────

  it('PUTs a lease approval to the admin endpoint', () => {
    const body = { approve: true, remarks: 'Signatures verified' };
    service.approveLease('lease-1', body).subscribe();

    const req = httpMock.expectOne(`${BASE}/leases/lease-1/approval`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: {} });
  });

  // ── Staff ──────────────────────────────────────────────────────────────────

  it('GETs the list of staff members', () => {
    service.getStaff().subscribe();

    const req = httpMock.expectOne(`${BASE}/staff`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  it('POSTs to create a new verifier staff account', () => {
    const body = { email: 'verifier@estatesync.com', fullName: 'Verifier One', password: 'Pass123!' } as any;
    service.createVerifier(body).subscribe();

    const req = httpMock.expectOne(`${BASE}/staff`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: {} });
  });

  it('PUTs to deactivate a staff member account', () => {
    service.deactivateVerifier('user-1').subscribe();

    const req = httpMock.expectOne(`${BASE}/staff/user-1/deactivate`);
    expect(req.request.method).toBe('PUT');
    req.flush({ success: true, data: {} });
  });

  // ── Audit Logs ─────────────────────────────────────────────────────────────

  it('GETs audit logs with entityType filter when provided', () => {
    service.getAuditLogs({ entityType: 'Lease' }).subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/AuditLogs');
    expect(req.request.params.get('entityType')).toBe('Lease');
    req.flush({ success: true, data: [], page: 1, limit: 20, total: 0 });
  });

  it('GETs audit logs without filters when none are provided', () => {
    service.getAuditLogs().subscribe();

    const req = httpMock.expectOne(r => r.url === '/api/v1/AuditLogs');
    expect(req.request.params.has('entityType')).toBe(false);
    req.flush({ success: true, data: [], page: 1, limit: 20, total: 0 });
  });
});
