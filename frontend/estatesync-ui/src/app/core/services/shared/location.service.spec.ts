import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { environment } from '../../../../environments/environment';
import { LocationService } from './location.service';

describe('LocationService', () => {
  let service: LocationService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [LocationService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LocationService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads and caches Indian states', () => {
    let firstName = '';
    let secondName = '';

    service.getStates().subscribe(response => firstName = response.data?.[0].name ?? '');
    const request = http.expectOne(`${environment.apiUrl}/api/v1/locations/states`);
    request.flush({ success: true, data: [{ code: 'KA', name: 'Karnataka' }] });

    service.getStates().subscribe(response => secondName = response.data?.[0].name ?? '');
    http.expectNone(`${environment.apiUrl}/api/v1/locations/states`);

    expect(firstName).toBe('Karnataka');
    expect(secondName).toBe('Karnataka');
  });

  it('encodes the state code and sends trimmed city search parameters', () => {
    service.searchCities('TN', '  chen  ', 20).subscribe();

    const request = http.expectOne(req =>
      req.url === `${environment.apiUrl}/api/v1/locations/states/TN/cities` &&
      req.params.get('search') === 'chen' &&
      req.params.get('limit') === '20');
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, data: [{ name: 'Chennai' }] });
  });

  it('clamps the requested result limit', () => {
    service.searchCities('KA', 'be', 500).subscribe();
    const request = http.expectOne(req => req.params.get('limit') === '50');
    request.flush({ success: true, data: [] });
  });
});
