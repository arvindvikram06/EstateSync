import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { ComplaintResponse, CreateComplaintRequest } from '../models/complaint.model';

@Injectable({ providedIn: 'root' })
export class ComplaintService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/complaints`;

  getMyComplaints(): Observable<ApiResponse<ComplaintResponse[]>> {
    return this.http.get<ApiResponse<ComplaintResponse[]>>(this.baseUrl);
  }

  createComplaint(body: CreateComplaintRequest): Observable<ApiResponse<ComplaintResponse>> {
    return this.http.post<ApiResponse<ComplaintResponse>>(this.baseUrl, body);
  }
}
