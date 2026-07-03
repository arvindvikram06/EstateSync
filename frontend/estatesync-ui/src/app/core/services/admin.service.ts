import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import {
  AdminDashboardStats,
  KycVerificationResponse,
  ReviewVerificationRequest,
  FinancialProfileResponse,
  PropertySummaryResponse,
  ApprovePropertyRequest,
  StaffResponse,
  CreateStaffRequest,
  OwnerSettlementResponse,
  SettlementSummaryResponse,
  ConnectedAccountResponse,
  AdminPaymentResponse,
  AdminComplaintResponse,
  AuditLogResponse,
  AdminLeaseApprovalRequest,
  AdminLeaseSummary,
} from '../models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/admin`;

  // ── Stats ──────────────────────────────────────────────────────────────────

  getDashboardStats(): Observable<ApiResponse<AdminDashboardStats>> {
    return this.http.get<ApiResponse<AdminDashboardStats>>(`${this.base}/dashboard/stats`);
  }

  // ── KYC ────────────────────────────────────────────────────────────────────

  getKycRequests(status?: string, page = 1, limit = 20): Observable<PagedResponse<KycVerificationResponse>> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResponse<KycVerificationResponse>>(`${this.base}/kyc-requests`, { params });
  }

  getKycRequestById(id: string): Observable<ApiResponse<KycVerificationResponse>> {
    return this.http.get<ApiResponse<KycVerificationResponse>>(`${this.base}/kyc-requests/${id}`);
  }

  reviewKyc(id: string, req: ReviewVerificationRequest): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/kyc-requests/${id}/review`, req);
  }

  // ── Financial Profiles ─────────────────────────────────────────────────────

  getFinancialProfiles(status?: string, page = 1, limit = 20): Observable<PagedResponse<FinancialProfileResponse>> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResponse<FinancialProfileResponse>>(`${this.base}/financial-profiles`, { params });
  }

  getFinancialProfileById(id: string): Observable<ApiResponse<FinancialProfileResponse>> {
    return this.http.get<ApiResponse<FinancialProfileResponse>>(`${this.base}/financial-profiles/${id}`);
  }

  reviewFinancialProfile(id: string, req: ReviewVerificationRequest): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/financial-profiles/${id}/review`, req);
  }

  // ── Properties ─────────────────────────────────────────────────────────────

  getPendingProperties(page = 1, limit = 20): Observable<PagedResponse<PropertySummaryResponse>> {
    const params = new HttpParams().set('page', page).set('limit', limit);
    return this.http.get<PagedResponse<PropertySummaryResponse>>(`${this.base}/properties/pending-approval`, { params });
  }

  approveProperty(id: string, req: ApprovePropertyRequest): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/properties/${id}/approval`, req);
  }

  // ── Lease Approval ─────────────────────────────────────────────────────────

  getLeasesPendingApproval(page = 1, limit = 20): Observable<PagedResponse<AdminLeaseSummary>> {
    const params = new HttpParams().set('page', page).set('limit', limit).set('status', 'PendingAdminApproval');
    return this.http.get<PagedResponse<AdminLeaseSummary>>(`${environment.apiUrl}/api/v1/leases`, { params });
  }

  approveLease(leaseId: string, req: AdminLeaseApprovalRequest): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/leases/${leaseId}/approval`, req);
  }

  // ── Staff ──────────────────────────────────────────────────────────────────

  getStaff(): Observable<ApiResponse<StaffResponse[]>> {
    return this.http.get<ApiResponse<StaffResponse[]>>(`${this.base}/staff`);
  }

  createVerifier(req: CreateStaffRequest): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.base}/staff`, req);
  }

  deactivateVerifier(userId: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.base}/staff/${userId}/deactivate`, {});
  }

  // ── Financial ─────────────────────────────────────────────────────────────

  getFinancialSummary(): Observable<ApiResponse<SettlementSummaryResponse>> {
    return this.http.get<ApiResponse<SettlementSummaryResponse>>(`${this.base}/financial/summary`);
  }

  getSettlements(filters?: { ownerId?: string; status?: string; from?: string; to?: string }, page = 1, limit = 20): Observable<PagedResponse<OwnerSettlementResponse>> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (filters?.ownerId) params = params.set('ownerId', filters.ownerId);
    if (filters?.status)  params = params.set('status', filters.status);
    if (filters?.from)    params = params.set('from', filters.from);
    if (filters?.to)      params = params.set('to', filters.to);
    return this.http.get<PagedResponse<OwnerSettlementResponse>>(`${this.base}/settlements`, { params });
  }

  getConnectedAccounts(page = 1, limit = 20): Observable<PagedResponse<ConnectedAccountResponse>> {
    const params = new HttpParams().set('page', page).set('limit', limit);
    return this.http.get<PagedResponse<ConnectedAccountResponse>>(`${this.base}/connected-accounts`, { params });
  }

  syncConnectedAccount(id: string): Observable<ApiResponse<ConnectedAccountResponse>> {
    return this.http.post<ApiResponse<ConnectedAccountResponse>>(`${this.base}/connected-accounts/${id}/sync`, {});
  }

  getPayments(filters?: { status?: string; from?: string; to?: string }, page = 1, limit = 20): Observable<PagedResponse<AdminPaymentResponse>> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.from)   params = params.set('from', filters.from);
    if (filters?.to)     params = params.set('to', filters.to);
    return this.http.get<PagedResponse<AdminPaymentResponse>>(`${this.base}/payments`, { params });
  }

  // ── Complaints ─────────────────────────────────────────────────────────────

  getAdminComplaints(status?: string, priority?: string, page = 1, limit = 20): Observable<PagedResponse<AdminComplaintResponse>> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (status)   params = params.set('status', status);
    if (priority) params = params.set('priority', priority);
    return this.http.get<PagedResponse<AdminComplaintResponse>>(`${this.base}/complaints`, { params });
  }

  updateComplaintStatus(complaintId: string, status: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${environment.apiUrl}/api/v1/complaints/${complaintId}/status`, { status });
  }

  // ── Audit Logs ─────────────────────────────────────────────────────────────

  getAuditLogs(filters?: { entityType?: string; entityId?: string; action?: string }, page = 1, limit = 20): Observable<PagedResponse<AuditLogResponse>> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (filters?.entityType) params = params.set('entityType', filters.entityType);
    if (filters?.entityId)   params = params.set('entityId', filters.entityId);
    if (filters?.action)     params = params.set('action', filters.action);
    return this.http.get<PagedResponse<AuditLogResponse>>(`${environment.apiUrl}/api/v1/AuditLogs`, { params });
  }
}
