import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';
import { BecomeOwnerRequest, UserProfileResponse } from '../../models/auth.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/users`;

  getProfile(): Observable<ApiResponse<UserProfileResponse>> {
    return this.http.get<ApiResponse<UserProfileResponse>>(`${this.baseUrl}/me/profile`);
  }

  updateProfile(fullName: string, phoneNumber: string): Observable<ApiResponse<UserProfileResponse>> {
    return this.http.put<ApiResponse<UserProfileResponse>>(`${this.baseUrl}/me/profile`, { fullName, phoneNumber });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.baseUrl}/me/change-password`, { currentPassword, newPassword });
  }

  becomeOwner(request: BecomeOwnerRequest): Observable<ApiResponse<object>> {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/me/become-owner`, request);
  }
}
