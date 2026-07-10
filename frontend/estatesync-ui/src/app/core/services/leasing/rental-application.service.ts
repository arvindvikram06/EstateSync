import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';
import { RentalApplicationResponse, CreateRentalApplicationRequest, OwnerApplicationResponse, ReviewApplicationRequest } from '../../models/rental-application.model';

@Injectable({ providedIn: 'root' })
export class RentalApplicationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/RentalApplications`;

  getMyApplications(): Observable<ApiResponse<RentalApplicationResponse[]>> {
    return this.http.get<ApiResponse<RentalApplicationResponse[]>>(`${this.baseUrl}/tenant`);
  }

  applyToProperty(propertyId: string, body: CreateRentalApplicationRequest): Observable<ApiResponse<RentalApplicationResponse>> {
    return this.http.post<ApiResponse<RentalApplicationResponse>>(`${this.baseUrl}/properties/${propertyId}`, body);
  }

  getOwnerApplications(): Observable<ApiResponse<OwnerApplicationResponse[]>> {
    return this.http.get<ApiResponse<OwnerApplicationResponse[]>>(`${this.baseUrl}/owner`);
  }

  reviewApplication(id: string, body: ReviewApplicationRequest): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.baseUrl}/${id}/review`, body);
  }

  getApplicationById(id: string): Observable<ApiResponse<OwnerApplicationResponse>> {
    return this.http.get<ApiResponse<OwnerApplicationResponse>>(`${this.baseUrl}/${id}`);
  }

  cancelApplication(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
