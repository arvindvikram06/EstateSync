import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { PropertyService } from './property.service';

const BASE = '/api/v1/properties';

describe('PropertyService', () => {
  let service: PropertyService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(PropertyService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // ── getProperties ──────────────────────────────────────────────────────────

  describe('getProperties', () => {
    it('sends page and limit as query params by default', () => {
      service.getProperties().subscribe();

      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('limit')).toBe('12');
      req.flush({ success: true, data: [], page: 1, limit: 12, total: 0 });
    });

    it('includes optional filter params when provided', () => {
      service.getProperties({ state: 'Karnataka', bedrooms: 3, minRent: 10000, maxRent: 50000 }).subscribe();

      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.params.get('state')).toBe('Karnataka');
      expect(req.request.params.get('bedrooms')).toBe('3');
      expect(req.request.params.get('minRent')).toBe('10000');
      expect(req.request.params.get('maxRent')).toBe('50000');
      req.flush({ success: true, data: [], page: 1, limit: 12, total: 0 });
    });

    it('omits filter params that are not set', () => {
      service.getProperties({}).subscribe();

      const req = httpMock.expectOne(r => r.url === BASE);
      expect(req.request.params.has('state')).toBe(false);
      expect(req.request.params.has('propertyType')).toBe(false);
      req.flush({ success: true, data: [], page: 1, limit: 12, total: 0 });
    });
  });

  // ── getProperty ────────────────────────────────────────────────────────────

  it('GETs a single property by id', () => {
    let result: unknown;
    service.getProperty('prop-1').subscribe(r => (result = r));

    const req = httpMock.expectOne(`${BASE}/prop-1`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: { propertyId: 'prop-1' } });

    expect((result as any).data.propertyId).toBe('prop-1');
  });

  // ── getMyProperties ────────────────────────────────────────────────────────

  it('GETs properties owned by the current user', () => {
    service.getMyProperties().subscribe();

    const req = httpMock.expectOne(`${BASE}/me`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  // ── createProperty ─────────────────────────────────────────────────────────

  it('POSTs a new property and returns the created detail', () => {
    const payload = { title: 'Sea View Villa', state: 'Goa' } as any;
    let result: unknown;
    service.createProperty(payload).subscribe(r => (result = r));

    const req = httpMock.expectOne(BASE);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ success: true, data: { propertyId: 'new-prop' } });

    expect((result as any).data.propertyId).toBe('new-prop');
  });

  // ── updateProperty ─────────────────────────────────────────────────────────

  it('PUTs updates to an existing property', () => {
    const update = { title: 'Updated Title' } as any;
    service.updateProperty('prop-1', update).subscribe();

    const req = httpMock.expectOne(`${BASE}/prop-1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(update);
    req.flush({ success: true, data: {} });
  });

  // ── deleteProperty ─────────────────────────────────────────────────────────

  it('DELETEs a property by id', () => {
    service.deleteProperty('prop-1').subscribe();

    const req = httpMock.expectOne(`${BASE}/prop-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush({ success: true, data: null });
  });

  // ── submitForApproval ──────────────────────────────────────────────────────

  it('PUTs an empty body to the submit endpoint', () => {
    service.submitForApproval('prop-1').subscribe();

    const req = httpMock.expectOne(`${BASE}/prop-1/submit`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({});
    req.flush({ success: true, data: {} });
  });

  // ── updateAmenities ────────────────────────────────────────────────────────

  it('PUTs amenity names in the request body', () => {
    service.updateAmenities('prop-1', ['Swimming Pool', 'Gym']).subscribe();

    const req = httpMock.expectOne(`${BASE}/prop-1/amenities`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ amenityNames: ['Swimming Pool', 'Gym'] });
    req.flush({ success: true, data: null });
  });

  // ── uploadImages ───────────────────────────────────────────────────────────

  it('POSTs each file as part of a FormData body', () => {
    const files = [new File(['a'], 'a.jpg'), new File(['b'], 'b.jpg')];
    service.uploadImages('prop-1', files).subscribe();

    const req = httpMock.expectOne(`${BASE}/prop-1/images`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeInstanceOf(FormData);
    req.flush({ success: true, data: { imageIds: ['id1', 'id2'] } });
  });

  // ── deleteImage ────────────────────────────────────────────────────────────

  it('DELETEs a specific image from a property', () => {
    service.deleteImage('prop-1', 'img-1').subscribe();

    const req = httpMock.expectOne(`${BASE}/prop-1/images/img-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  // ── uploadDocument ─────────────────────────────────────────────────────────

  it('POSTs a document as FormData with OwnershipCertificate type', () => {
    const file = new File(['doc'], 'deed.pdf');
    service.uploadDocument('prop-1', file).subscribe();

    const req = httpMock.expectOne(`${BASE}/prop-1/documents`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeInstanceOf(FormData);
    req.flush({ success: true, data: {} });
  });

  // ── getDocuments ───────────────────────────────────────────────────────────

  it('GETs the documents for a property', () => {
    service.getDocuments('prop-1').subscribe();

    const req = httpMock.expectOne(`${BASE}/prop-1/documents`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: [] });
  });

  // ── Wizard draft ───────────────────────────────────────────────────────────

  it('POSTs to wizard-draft endpoint when creating a draft', () => {
    const payload = { title: 'Draft Villa' } as any;
    service.createWizardDraft(payload).subscribe();

    const req = httpMock.expectOne(`${BASE}/wizard-draft`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: {} });
  });

  it('PATCHes an existing wizard draft', () => {
    const patch = { bedrooms: 3 } as any;
    service.patchWizardDraft('prop-1', patch).subscribe();

    const req = httpMock.expectOne(`${BASE}/prop-1/wizard-draft`);
    expect(req.request.method).toBe('PATCH');
    req.flush({ success: true, data: {} });
  });

  it('GETs the current user active draft', () => {
    service.getMyActiveDraft().subscribe();

    const req = httpMock.expectOne(`${BASE}/wizard-draft/mine`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: null });
  });

  // ── fileUrl / imageUrl ─────────────────────────────────────────────────────

  describe('fileUrl', () => {
    it('returns empty string for null input', () => {
      expect(service.fileUrl(null)).toBe('');
    });

    it('returns absolute URLs unchanged', () => {
      expect(service.fileUrl('https://cdn.example.com/img.jpg')).toBe('https://cdn.example.com/img.jpg');
    });

    it('prepends the API base URL to relative paths', () => {
      const url = service.fileUrl('/uploads/image.jpg');
      expect(url).toContain('/uploads/image.jpg');
    });
  });

  // ── triggerRefreshMyProperties ─────────────────────────────────────────────

  it('emits on refreshMyProperties$ when triggered', () => {
    let emitted = false;
    service.refreshMyProperties$.subscribe(() => (emitted = true));
    service.triggerRefreshMyProperties();
    expect(emitted).toBe(true);
  });
});
