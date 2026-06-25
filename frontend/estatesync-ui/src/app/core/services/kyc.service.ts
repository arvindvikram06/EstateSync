import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { KycVerificationResponse, DocumentResponse, SubmitKycRequest, KycDocumentType } from '../models/kyc.model';

@Injectable({ providedIn: 'root' })
export class KycService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/kyc`;

  getMyKyc(): Observable<ApiResponse<KycVerificationResponse[]>> {
    return this.http.get<ApiResponse<KycVerificationResponse[]>>(`${this.baseUrl}/me`);
  }

  createDraft(): Observable<ApiResponse<KycVerificationResponse>> {
    return this.http.post<ApiResponse<KycVerificationResponse>>(`${this.baseUrl}/draft`, {});
  }

  uploadDocument(kycId: string, file: File, documentType: KycDocumentType): Observable<ApiResponse<DocumentResponse>> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('documentType', documentType);
    return this.http.put<ApiResponse<DocumentResponse>>(`${this.baseUrl}/${kycId}/documents`, form);
  }

  submit(kycId: string, request: SubmitKycRequest): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/${kycId}/submit`, request);
  }
}
