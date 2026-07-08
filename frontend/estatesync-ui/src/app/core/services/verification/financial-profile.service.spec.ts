import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { FinancialProfileService } from './financial-profile.service';

const BASE = '/api/v1/tenants/me/financial-profile';

describe('FinancialProfileService', () => {
  let service: FinancialProfileService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(FinancialProfileService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs the financial profile list for the current tenant', () => {
    service.getMyProfile().subscribe();

    const req = httpMock.expectOne(BASE);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  it('POSTs an empty body to create a draft profile', () => {
    service.createDraft().subscribe();

    const req = httpMock.expectOne(`${BASE}/draft`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({ success: true, data: { financialProfileId: 'fp-1' } });
  });

  it('PUTs the document file and documentType for a given profile id', () => {
    const file = new File(['slip'], 'salary.pdf');
    service.uploadDocument('fp-1', file, 'SalarySlip').subscribe();

    const req = httpMock.expectOne(`${BASE}/fp-1/documents`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toBeInstanceOf(FormData);
    req.flush({ success: true, data: { documentType: 'SalarySlip' } });
  });

  it('POSTs employment and income details to submit the profile', () => {
    const submitBody = { employmentType: 'Salaried', monthlyIncome: 75000 } as any;
    service.submit('fp-1', submitBody).subscribe();

    const req = httpMock.expectOne(`${BASE}/fp-1/submit`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(submitBody);
    req.flush({ success: true, data: {} });
  });
});
