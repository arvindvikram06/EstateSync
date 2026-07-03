import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { LeaseBillResponse, OwnerAvailableBalance, OwnerEarningsSummaryResponse, OwnerSettlementResponse, RequestPayoutResult } from '../models/lease-bill.model';
import { PagedResponse } from '../models/api-response.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/payments`;

  // ── Tenant ────────────────────────────────────────────────────

  getTenantBills(): Observable<ApiResponse<LeaseBillResponse[]>> {
    return this.http.get<ApiResponse<LeaseBillResponse[]>>(`${this.baseUrl}/tenant/bills`);
  }

  createCheckoutSession(leaseBillId: string): Observable<ApiResponse<{ url: string }>> {
    return this.http.post<ApiResponse<{ url: string }>>(`${this.baseUrl}/checkout/${leaseBillId}`, {});
  }

  // ── Owner: Stripe Connect ─────────────────────────────────────

  createOwnerOnboardingLink(): Observable<ApiResponse<{ url: string }>> {
    return this.http.post<ApiResponse<{ url: string }>>(`${this.baseUrl}/owner-onboard`, {});
  }

  getStripeLoginLink(): Observable<ApiResponse<{ url: string }>> {
    return this.http.post<ApiResponse<{ url: string }>>(`${this.baseUrl}/owner/stripe-login-link`, {});
  }

  getOwnerSettlements(status?: string, page = 1, limit = 20): Observable<PagedResponse<OwnerSettlementResponse>> {
    let params = `page=${page}&limit=${limit}`;
    if (status) params += `&status=${status}`;
    return this.http.get<PagedResponse<OwnerSettlementResponse>>(`${this.baseUrl}/owner/settlements?${params}`);
  }

  getOwnerEarningsSummary(): Observable<ApiResponse<OwnerEarningsSummaryResponse>> {
    return this.http.get<ApiResponse<OwnerEarningsSummaryResponse>>(`${this.baseUrl}/owner/earnings-summary`);
  }

  getOwnerAvailableBalance(): Observable<ApiResponse<OwnerAvailableBalance>> {
    return this.http.get<ApiResponse<OwnerAvailableBalance>>(`${this.baseUrl}/owner/available-balance`);
  }

  requestPayout(method: 'standard' | 'instant'): Observable<ApiResponse<RequestPayoutResult>> {
    return this.http.post<ApiResponse<RequestPayoutResult>>(`${this.baseUrl}/owner/request-payout`, { method });
  }
}
