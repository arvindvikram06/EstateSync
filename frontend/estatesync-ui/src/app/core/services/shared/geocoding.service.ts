import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

interface NominatimSearchResult {
  lat: string;
  lon: string;
}

interface NominatimReverseResult {
  display_name?: string;
  address?: {
    road?: string;
    suburb?: string;
    city?: string;
    town?: string;
    state_district?: string;
    state?: string;
    postcode?: string;
  };
}

export interface GeocodedAddress {
  displayName: string;
  city?: string;
  state?: string;
  postcode?: string;
}

@Injectable({ providedIn: 'root' })
export class GeocodingService {
  private readonly http = inject(HttpClient);
  private readonly base = 'https://nominatim.openstreetmap.org';
  private readonly headers = { 'X-Requested-With': 'EstateSync/1.0' };

  searchLocation(city: string, state: string): Observable<{ lat: number; lon: number } | null> {
    const q = new URLSearchParams({ format: 'json', city, state, country: 'India', limit: '1' });
    return this.http
      .get<NominatimSearchResult[]>(`${this.base}/search?${q}`, { headers: this.headers })
      .pipe(
        map(r => r.length > 0 ? { lat: parseFloat(r[0].lat), lon: parseFloat(r[0].lon) } : null),
        catchError(() => of(null)),
      );
  }

  reverseGeocode(lat: number, lon: number): Observable<GeocodedAddress | null> {
    const q = new URLSearchParams({ format: 'json', lat: lat.toString(), lon: lon.toString() });
    return this.http
      .get<NominatimReverseResult>(`${this.base}/reverse?${q}`, { headers: this.headers })
      .pipe(
        map(r => ({
          displayName: r.display_name ?? '',
          city: r.address?.city ?? r.address?.town ?? r.address?.state_district,
          state: r.address?.state,
          postcode: r.address?.postcode,
        })),
        catchError(() => of(null)),
      );
  }
}
