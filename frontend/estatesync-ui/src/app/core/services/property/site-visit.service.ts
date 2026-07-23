import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';
import {
  CreateSiteVisitRequest,
  PropertyVisitSlot,
  RescheduleSiteVisitRequest,
  SiteVisitResponse,
  UpdateSiteVisitStatusRequest
} from '../../models/site-visit.model';

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

  getAvailableSlots(propertyId: string, from: string, to: string): Observable<ApiResponse<PropertyVisitSlot[]>> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<ApiResponse<PropertyVisitSlot[]>>(
      `${environment.apiUrl}/api/v1/properties/${propertyId}/visit-slots/available`, { params });
  }

  getManageSlots(propertyId: string, from: string, to: string): Observable<ApiResponse<PropertyVisitSlot[]>> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<ApiResponse<PropertyVisitSlot[]>>(
      `${environment.apiUrl}/api/v1/properties/${propertyId}/visit-slots/manage`, { params });
  }

  createSlots(propertyId: string, date: string, startTimes: string[]): Observable<ApiResponse<PropertyVisitSlot[]>> {
    return this.http.post<ApiResponse<PropertyVisitSlot[]>>(
      `${environment.apiUrl}/api/v1/properties/${propertyId}/visit-slots/bulk`, { date, startTimes });
  }

  setSlotBlocked(propertyId: string, slotId: string, blocked: boolean): Observable<ApiResponse<PropertyVisitSlot>> {
    const action = blocked ? 'block' : 'unblock';
    return this.http.put<ApiResponse<PropertyVisitSlot>>(
      `${environment.apiUrl}/api/v1/properties/${propertyId}/visit-slots/${slotId}/${action}`, {});
  }

  deleteSlot(propertyId: string, slotId: string): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/api/v1/properties/${propertyId}/visit-slots/${slotId}`);
  }

  reschedule(id: string, request: RescheduleSiteVisitRequest): Observable<ApiResponse<SiteVisitResponse>> {
    return this.http.put<ApiResponse<SiteVisitResponse>>(`${this.baseUrl}/${id}/reschedule`, request);
  }

  confirmReschedule(id: string): Observable<ApiResponse<SiteVisitResponse>> {
    return this.http.put<ApiResponse<SiteVisitResponse>>(`${this.baseUrl}/${id}/confirm-reschedule`, {});
  }

  rejectReschedule(id: string): Observable<ApiResponse<SiteVisitResponse>> {
    return this.http.put<ApiResponse<SiteVisitResponse>>(`${this.baseUrl}/${id}/reject-reschedule`, {});
  }
}
