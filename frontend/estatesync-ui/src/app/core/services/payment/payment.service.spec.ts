import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { PaymentService } from './payment.service';

const BASE = '/api/v1/payments';

describe('PaymentService', () => {
  let service: PaymentService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(PaymentService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // ── Tenant ─────────────────────────────────────────────────────────────────

  it('GETs all outstanding bills for the tenant', () => {
    service.getTenantBills().subscribe();

    const req = httpMock.expectOne(`${BASE}/tenant/bills`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  it('POSTs to create a Stripe checkout session and returns the URL', () => {
    let result: unknown;
    service.createCheckoutSession('bill-1').subscribe(r => (result = r));

    const req = httpMock.expectOne(`${BASE}/checkout/bill-1`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: { url: 'https://checkout.stripe.com/pay/abc' } });

    expect((result as any).data.url).toContain('stripe.com');
  });

  // ── Owner ──────────────────────────────────────────────────────────────────

  it('POSTs to create a Stripe Connect onboarding link for the owner', () => {
    let result: unknown;
    service.createOwnerOnboardingLink().subscribe(r => (result = r));

    const req = httpMock.expectOne(`${BASE}/owner-onboard`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: { url: 'https://connect.stripe.com/setup' } });

    expect((result as any).data.url).toContain('stripe.com');
  });

  it('POSTs to retrieve the Stripe login link for an owner', () => {
    service.getStripeLoginLink().subscribe();

    const req = httpMock.expectOne(`${BASE}/owner/stripe-login-link`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: { url: 'https://dashboard.stripe.com/login' } });
  });

  it('GETs owner settlements with default page and limit params', () => {
    service.getOwnerSettlements().subscribe();

    const req = httpMock.expectOne(r => r.url.startsWith(`${BASE}/owner/settlements`));
    expect(req.request.method).toBe('GET');
    expect(req.request.urlWithParams).toContain('page=1');
    expect(req.request.urlWithParams).toContain('limit=20');
    req.flush({ success: true, data: [], page: 1, limit: 20, total: 0 });
  });

  it('includes status filter param in settlements query when provided', () => {
    service.getOwnerSettlements('Paid').subscribe();

    const req = httpMock.expectOne(r => r.url.startsWith(`${BASE}/owner/settlements`));
    expect(req.request.urlWithParams).toContain('status=Paid');
    req.flush({ success: true, data: [], page: 1, limit: 20, total: 0 });
  });

  it('GETs the owner earnings summary', () => {
    service.getOwnerEarningsSummary().subscribe();

    const req = httpMock.expectOne(`${BASE}/owner/earnings-summary`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: {} });
  });

  it('GETs the owner available balance', () => {
    service.getOwnerAvailableBalance().subscribe();

    const req = httpMock.expectOne(`${BASE}/owner/available-balance`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { amount: 5000 } });
  });

  it('POSTs a standard payout request with the method in the body', () => {
    service.requestPayout('standard').subscribe();

    const req = httpMock.expectOne(`${BASE}/owner/request-payout`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ method: 'standard' });
    req.flush({ success: true, data: {} });
  });

  it('POSTs an instant payout request with the method in the body', () => {
    service.requestPayout('instant').subscribe();

    const req = httpMock.expectOne(`${BASE}/owner/request-payout`);
    expect(req.request.body).toEqual({ method: 'instant' });
    req.flush({ success: true, data: {} });
  });
});
