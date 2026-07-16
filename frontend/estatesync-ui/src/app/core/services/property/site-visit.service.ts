import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';
import { CreateSiteVisitRequest, SiteVisitResponse, UpdateSiteVisitStatusRequest } from '../../models/site-visit.model';

@Injectable({ providedIn: 'root' })
export class SiteVisitService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/site-visits`;

  requestVisit(request: CreateSiteVisitRequest): Observable<ApiResponse<SiteVisitResponse>> {
    return this.http.post<ApiResponse<SiteVisitResponse>>(this.baseUrl, request);
  }

  updateStatus(id: string, request: UpdateSiteVisitStatusRequest): Observable<ApiResponse<SiteVisitResponse>> {
    return this.http.put<ApiResponse<SiteVisitResponse>>(`${this.baseUrl}/${id}/status`, request);
  }

  getTenantVisits(): Observable<ApiResponse<SiteVisitResponse[]>> {
    return this.http.get<ApiResponse<SiteVisitResponse[]>>(`${this.baseUrl}/tenant`);
  }

  getOwnerVisits(): Observable<ApiResponse<SiteVisitResponse[]>> {
    return this.http.get<ApiResponse<SiteVisitResponse[]>>(`${this.baseUrl}/owner`);
  }

  getById(id: string): Observable<ApiResponse<SiteVisitResponse>> {
    return this.http.get<ApiResponse<SiteVisitResponse>>(`${this.baseUrl}/${id}`);
  }
}
