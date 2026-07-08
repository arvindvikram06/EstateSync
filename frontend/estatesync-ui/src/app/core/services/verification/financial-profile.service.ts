import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';
import { FinancialDocumentResponse, TenantFinancialProfileResponse, SubmitTenantFinancialProfileRequest } from '../../models/financial-profile.model';

@Injectable({ providedIn: 'root' })
export class FinancialProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/tenants/me/financial-profile`;

  getMyProfile(): Observable<ApiResponse<TenantFinancialProfileResponse[]>> {
    return this.http.get<ApiResponse<TenantFinancialProfileResponse[]>>(this.baseUrl);
  }

  createDraft(): Observable<ApiResponse<TenantFinancialProfileResponse>> {
    return this.http.post<ApiResponse<TenantFinancialProfileResponse>>(`${this.baseUrl}/draft`, {});
  }

  uploadDocument(profileId: string, file: File, documentType: string): Observable<ApiResponse<FinancialDocumentResponse>> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('documentType', documentType);
    return this.http.put<ApiResponse<FinancialDocumentResponse>>(`${this.baseUrl}/${profileId}/documents`, form);
  }

  submit(profileId: string, request: SubmitTenantFinancialProfileRequest): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/${profileId}/submit`, request);
  }
}
