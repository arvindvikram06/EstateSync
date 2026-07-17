import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, finalize } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';

@Injectable({ providedIn: 'root' })
export class ChatbotService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/v1/ai`;

  readonly isLoading = signal(false);

  chat(message: string): Observable<ApiResponse<string>> {
    this.isLoading.set(true);
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/chat`, { message }).pipe(
      finalize(() => this.isLoading.set(false))
    );
  }
}
