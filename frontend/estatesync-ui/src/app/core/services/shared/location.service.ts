import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';

export interface LocationState {
  code: string;
  name: string;
}

export interface LocationCity {
  name: string;
}

@Injectable({ providedIn: 'root' })
export class LocationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/locations`;
  private readonly states$ = this.http
    .get<ApiResponse<LocationState[]>>(`${this.baseUrl}/states`)
    .pipe(shareReplay({ bufferSize: 1, refCount: false }));

  getStates(): Observable<ApiResponse<LocationState[]>> {
    return this.states$;
  }

  searchCities(stateCode: string, search: string, limit = 20): Observable<ApiResponse<LocationCity[]>> {
    const params = new HttpParams()
      .set('search', search.trim())
      .set('limit', Math.min(Math.max(limit, 1), 50).toString());

    return this.http.get<ApiResponse<LocationCity[]>>(
      `${this.baseUrl}/states/${encodeURIComponent(stateCode)}/cities`,
      { params },
    );
  }
}
