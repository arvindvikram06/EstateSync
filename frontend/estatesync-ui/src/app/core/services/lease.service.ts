import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import {
  LeaseSummaryResponse,
  LeaseDetailResponse,
  SignLeaseRequest,
  OwnerLeaseSummaryResponse,
  CreateLeaseRequest,
  VoidLeaseRequest,
  TerminateLeaseRequest,
  AdminLeaseApprovalRequest,
} from '../models/lease.model';

@Injectable({ providedIn: 'root' })
export class LeaseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/leases`;
  private readonly adminUrl = `${environment.apiUrl}/api/v1/admin`;

  getMyLeases(): Observable<ApiResponse<LeaseSummaryResponse[]>> {
    return this.http.get<ApiResponse<LeaseSummaryResponse[]>>(`${this.baseUrl}/tenant`);
  }

  getOwnerLeases(): Observable<ApiResponse<OwnerLeaseSummaryResponse[]>> {
    return this.http.get<ApiResponse<OwnerLeaseSummaryResponse[]>>(`${this.baseUrl}/owner`);
  }

  getAdminLeases(page = 1, limit = 50): Observable<PagedResponse<LeaseDetailResponse>> {
    return this.http.get<PagedResponse<LeaseDetailResponse>>(
      `${this.baseUrl}?page=${page}&limit=${limit}`
    );
  }

  getLeaseDetail(id: string): Observable<ApiResponse<LeaseDetailResponse>> {
    return this.http.get<ApiResponse<LeaseDetailResponse>>(`${this.baseUrl}/${id}`);
  }

  createLease(req: CreateLeaseRequest): Observable<ApiResponse<LeaseDetailResponse>> {
    return this.http.post<ApiResponse<LeaseDetailResponse>>(this.baseUrl, req);
  }

  signLease(id: string, body: SignLeaseRequest): Observable<ApiResponse<LeaseDetailResponse>> {
    return this.http.put<ApiResponse<LeaseDetailResponse>>(`${this.baseUrl}/${id}/sign`, body);
  }

  voidLease(id: string, body: VoidLeaseRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${id}/void`, body);
  }

  terminateLease(id: string, body: TerminateLeaseRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.baseUrl}/${id}/terminate`, body);
  }

  adminApproveLease(id: string, body: AdminLeaseApprovalRequest): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.adminUrl}/leases/${id}/approval`, body);
  }
}
