import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { LeaseSummaryResponse, LeaseDetailResponse, SignLeaseRequest } from '../models/lease.model';

@Injectable({ providedIn: 'root' })
export class LeaseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/leases`;

  getMyLeases(): Observable<ApiResponse<LeaseSummaryResponse[]>> {
    return this.http.get<ApiResponse<LeaseSummaryResponse[]>>(`${this.baseUrl}/tenant`);
  }

  getLeaseDetail(id: string): Observable<ApiResponse<LeaseDetailResponse>> {
    return this.http.get<ApiResponse<LeaseDetailResponse>>(`${this.baseUrl}/${id}`);
  }

  signLease(id: string, body: SignLeaseRequest): Observable<ApiResponse<LeaseDetailResponse>> {
    return this.http.put<ApiResponse<LeaseDetailResponse>>(`${this.baseUrl}/${id}/sign`, body);
  }
}
