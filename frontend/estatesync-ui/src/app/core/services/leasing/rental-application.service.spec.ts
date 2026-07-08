import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { RentalApplicationService } from './rental-application.service';

const BASE = '/api/v1/RentalApplications';

describe('RentalApplicationService', () => {
  let service: RentalApplicationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(RentalApplicationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs all applications for the current tenant', () => {
    service.getMyApplications().subscribe();

    const req = httpMock.expectOne(`${BASE}/tenant`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  it('POSTs an application body for a specific property', () => {
    const body = { message: 'I would like to rent this.' } as any;
    service.applyToProperty('prop-1', body).subscribe();

    const req = httpMock.expectOne(`${BASE}/properties/prop-1`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: { applicationId: 'app-1' } });
  });

  it('GETs all applications received by the owner', () => {
    service.getOwnerApplications().subscribe();

    const req = httpMock.expectOne(`${BASE}/owner`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  it('PUTs an approved review decision', () => {
    const body = { status: 'Approved', remarks: 'Good applicant' } as any;
    service.reviewApplication('app-1', body).subscribe();

    const req = httpMock.expectOne(`${BASE}/app-1/review`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: null });
  });

  it('GETs an application detail by id', () => {
    let result: unknown;
    service.getApplicationById('app-1').subscribe(r => (result = r));

    const req = httpMock.expectOne(`${BASE}/app-1`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { applicationId: 'app-1' } });

    expect((result as any).data.applicationId).toBe('app-1');
  });
});
