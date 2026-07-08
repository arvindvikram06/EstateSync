import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { KycService } from './kyc.service';
const BASE = '/api/v1/kyc';

describe('KycService', () => {
  let service: KycService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(KycService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs the current user KYC records', () => {
    let result: unknown;
    service.getMyKyc().subscribe(r => (result = r));

    const req = httpMock.expectOne(`${BASE}/me`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });

    expect((result as any).data).toEqual([]);
  });

  it('POSTs an empty body to create a draft', () => {
    service.createDraft().subscribe();

    const req = httpMock.expectOne(`${BASE}/draft`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({});
    req.flush({ success: true, data: { kycVerificationId: 'kyc-1' } });
  });

  it('PUTs the document file and documentType for a given KYC id', () => {
    const file = new File(['content'], 'aadhaar.jpg');
    service.uploadDocument('kyc-1', file, 'AadhaarFront').subscribe();

    const req = httpMock.expectOne(`${BASE}/kyc-1/documents`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toBeInstanceOf(FormData);
    req.flush({ success: true, data: { documentType: 'AadhaarFront' } });
  });

  it('POSTs aadhaar and pan details to submit a KYC', () => {
    const submitBody = { aadhaarNumber: '1234-5678-9012', panNumber: 'ABCDE1234F' };
    service.submit('kyc-1', submitBody).subscribe();

    const req = httpMock.expectOne(`${BASE}/kyc-1/submit`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(submitBody);
    req.flush({ success: true, data: {} });
  });
});
