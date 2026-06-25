import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { LeaseBillResponse } from '../models/lease-bill.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/payments`;

  createOwnerOnboardingLink(): Observable<ApiResponse<{ url: string }>> {
    return this.http.post<ApiResponse<{ url: string }>>(`${this.baseUrl}/owner-onboard`, {});
  }

  getTenantBills(): Observable<ApiResponse<LeaseBillResponse[]>> {
    return this.http.get<ApiResponse<LeaseBillResponse[]>>(`${this.baseUrl}/tenant/bills`);
  }

  createCheckoutSession(leaseBillId: string): Observable<ApiResponse<{ checkoutUrl: string }>> {
    return this.http.post<ApiResponse<{ checkoutUrl: string }>>(`${this.baseUrl}/checkout/${leaseBillId}`, {});
  }
}
