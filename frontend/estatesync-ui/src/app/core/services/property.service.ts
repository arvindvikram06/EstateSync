import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResponse } from '../models/api-response.model';
import { PropertySummaryResponse, PropertyDetailResponse, PropertyFilters } from '../models/property.model';

@Injectable({ providedIn: 'root' })
export class PropertyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/properties`;

  getProperties(filters: PropertyFilters = {}, page = 1, limit = 12): Observable<PagedResponse<PropertySummaryResponse>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('limit', limit.toString());

    if (filters.state)        params = params.set('state', filters.state);
    if (filters.propertyType) params = params.set('propertyType', filters.propertyType);
    if (filters.bedrooms)     params = params.set('bedrooms', filters.bedrooms.toString());
    if (filters.minRent)      params = params.set('minRent', filters.minRent.toString());
    if (filters.maxRent)      params = params.set('maxRent', filters.maxRent.toString());

    return this.http.get<PagedResponse<PropertySummaryResponse>>(this.baseUrl, { params });
  }

  getProperty(id: string): Observable<{ success: boolean; data: PropertyDetailResponse }> {
    return this.http.get<{ success: boolean; data: PropertyDetailResponse }>(`${this.baseUrl}/${id}`);
  }

  imageUrl(path: string | null): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    return `${environment.apiUrl}${path}`;
  }
}
