import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/users`;

  getProfile(): Observable<ApiResponse<{ isKycVerified: boolean; isOwnerVerified: boolean }>> {
    return this.http.get<ApiResponse<{ isKycVerified: boolean; isOwnerVerified: boolean }>>(`${this.baseUrl}/me/profile`);
  }

  becomeOwner(): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/me/become-owner`, {});
  }
}
