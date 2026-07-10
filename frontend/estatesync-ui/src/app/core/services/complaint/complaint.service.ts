import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';
import { ComplaintResponse, CreateComplaintRequest } from '../../models/complaint.model';

@Injectable({ providedIn: 'root' })
export class ComplaintService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/complaints`;

  /** Complaints the current user RAISED (tenant dashboard). */
  getTenantComplaints(): Observable<ApiResponse<ComplaintResponse[]>> {
    return this.http.get<ApiResponse<ComplaintResponse[]>>(`${this.baseUrl}/tenant`);
  }

  /** Complaints AGAINST the current user's properties (owner dashboard). */
  getOwnerComplaints(): Observable<ApiResponse<ComplaintResponse[]>> {
    return this.http.get<ApiResponse<ComplaintResponse[]>>(`${this.baseUrl}/owner`);
  }

  createComplaint(body: CreateComplaintRequest): Observable<ApiResponse<ComplaintResponse>> {
    return this.http.post<ApiResponse<ComplaintResponse>>(this.baseUrl, body);
  }

  updateComplaintStatus(id: string, body: { status: string }): Observable<ApiResponse<ComplaintResponse>> {
    return this.http.put<ApiResponse<ComplaintResponse>>(`${this.baseUrl}/${id}/status`, body);
  }

  /** Backend accepts one file per call (`file` form field). */
  uploadComplaintImage(id: string, file: File): Observable<ApiResponse<ComplaintResponse>> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ApiResponse<ComplaintResponse>>(`${this.baseUrl}/${id}/images`, form);
  }

  /** Resolve a relative upload path (e.g. /uploads/complaints/...) against the API host. */
  fileUrl(path: string | null): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    const base = environment.apiUrl;
    return path.startsWith('/') ? `${base}${path}` : `${base}/${path}`;
  }
}
