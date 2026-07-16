import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse, PagedResponse } from '../../models/api-response.model';
import {
  PropertySummaryResponse, PropertyDetailResponse, PropertyFilters,
  CreatePropertyRequest, UpdatePropertyRequest, DocumentResponse,
  CreatePropertyDraftRequest, PatchPropertyDraftRequest,
} from '../../models/property.model';

@Injectable({ providedIn: 'root' })
export class PropertyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/properties`;

  getProperties(filters: PropertyFilters = {}, page = 1, limit = 12): Observable<PagedResponse<PropertySummaryResponse>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('limit', limit.toString());

    if (filters.search)       params = params.set('search', filters.search);
    if (filters.state)        params = params.set('state', filters.state);
    if (filters.propertyType) params = params.set('propertyType', filters.propertyType);
    if (filters.bedrooms)     params = params.set('bedrooms', filters.bedrooms.toString());
    if (filters.minRent)      params = params.set('minRent', filters.minRent.toString());
    if (filters.maxRent)      params = params.set('maxRent', filters.maxRent.toString());

    return this.http.get<PagedResponse<PropertySummaryResponse>>(this.baseUrl, { params });
  }

  getProperty(id: string): Observable<ApiResponse<PropertyDetailResponse>> {
    return this.http.get<ApiResponse<PropertyDetailResponse>>(`${this.baseUrl}/${id}`);
  }

  imageUrl(path: string | null): string {
    return this.fileUrl(path);
  }

  fileUrl(path: string | null): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    const base = environment.apiUrl || 'http://localhost:5118';
    return path.startsWith('/') ? `${base}${path}` : `${base}/${path}`;
  }

  getMyProperties(): Observable<ApiResponse<PropertySummaryResponse[]>> {
    return this.http.get<ApiResponse<PropertySummaryResponse[]>>(`${this.baseUrl}/me`);
  }

  createProperty(req: CreatePropertyRequest): Observable<ApiResponse<PropertyDetailResponse>> {
    return this.http.post<ApiResponse<PropertyDetailResponse>>(this.baseUrl, req);
  }

  updateProperty(id: string, req: UpdatePropertyRequest): Observable<ApiResponse<PropertyDetailResponse>> {
    return this.http.put<ApiResponse<PropertyDetailResponse>>(`${this.baseUrl}/${id}`, req);
  }

  deleteProperty(id: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.baseUrl}/${id}`);
  }

  submitForApproval(id: string): Observable<ApiResponse<PropertyDetailResponse>> {
    return this.http.put<ApiResponse<PropertyDetailResponse>>(`${this.baseUrl}/${id}/submit`, {});
  }

  updateAmenities(id: string, amenities: string[]): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.baseUrl}/${id}/amenities`, { amenityNames: amenities });
  }

  uploadImages(id: string, files: File[]): Observable<ApiResponse<{ imageIds: string[] }>> {
    const form = new FormData();
    files.forEach(f => form.append('files', f));
    return this.http.post<ApiResponse<{ imageIds: string[] }>>(`${this.baseUrl}/${id}/images`, form);
  }

  deleteImage(id: string, imageId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/images/${imageId}`);
  }

  uploadDocument(id: string, file: File): Observable<ApiResponse<DocumentResponse>> {
    const form = new FormData();
    form.append('file', file);
    form.append('documentType', 'OwnershipCertificate');
    return this.http.post<ApiResponse<DocumentResponse>>(`${this.baseUrl}/${id}/documents`, form);
  }

  getDocuments(id: string): Observable<ApiResponse<DocumentResponse[]>> {
    return this.http.get<ApiResponse<DocumentResponse[]>>(`${this.baseUrl}/${id}/documents`);
  }

  // ── Wizard draft ──────────────────────────────────────────────────────────

  createWizardDraft(req: CreatePropertyDraftRequest): Observable<ApiResponse<PropertyDetailResponse>> {
    return this.http.post<ApiResponse<PropertyDetailResponse>>(`${this.baseUrl}/wizard-draft`, req);
  }

  patchWizardDraft(id: string, req: PatchPropertyDraftRequest): Observable<ApiResponse<PropertyDetailResponse>> {
    return this.http.patch<ApiResponse<PropertyDetailResponse>>(`${this.baseUrl}/${id}/wizard-draft`, req);
  }

  getMyActiveDraft(): Observable<ApiResponse<PropertyDetailResponse | null>> {
    return this.http.get<ApiResponse<PropertyDetailResponse | null>>(`${this.baseUrl}/wizard-draft/mine`);
  }
}
