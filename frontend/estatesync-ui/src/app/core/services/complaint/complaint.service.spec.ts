import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { ComplaintService } from './complaint.service';

const BASE = '/api/v1/complaints';

describe('ComplaintService', () => {
  let service: ComplaintService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(ComplaintService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs tenant complaints from the /tenant route', () => {
    service.getTenantComplaints().subscribe();

    const req = httpMock.expectOne(`${BASE}/tenant`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  it('GETs owner complaints from the /owner route', () => {
    service.getOwnerComplaints().subscribe();

    const req = httpMock.expectOne(`${BASE}/owner`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  it('POSTs a new complaint with the full request body', () => {
    const body = {
      propertyId: 'prop-1',
      type: 'Maintenance',
      priority: 'High',
      description: 'Broken water pipe',
    } as any;
    service.createComplaint(body).subscribe();

    const req = httpMock.expectOne(BASE);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ success: true, data: { complaintId: 'c-1' } });
  });

  it('PUTs a status update to the specific complaint', () => {
    service.updateComplaintStatus('c-1', { status: 'InProgress' }).subscribe();

    const req = httpMock.expectOne(`${BASE}/c-1/status`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ status: 'InProgress' });
    req.flush({ success: true, data: {} });
  });
});
