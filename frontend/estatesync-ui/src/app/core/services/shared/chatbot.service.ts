import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, finalize } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';

export interface AiChatSource {
  title: string;
  chunkId: string;
  documentVersion: string;
}

export interface AiNavigationAction {
  label: string;
  route: string;
}

export interface AiChatResponse {
  reply: string;
  sources: AiChatSource[];
  actions: AiNavigationAction[];
}

export interface AiChatHistoryTurn {
  role: 'user' | 'assistant';
  content: string;
}

@Injectable({ providedIn: 'root' })
export class ChatbotService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/v1/ai`;

  readonly isLoading = signal(false);

  chat(
    message: string,
    history: AiChatHistoryTurn[] = [],
  ): Observable<ApiResponse<AiChatResponse>> {
    this.isLoading.set(true);
    return this.http.post<ApiResponse<AiChatResponse>>(
      `${this.apiUrl}/chat`,
      { message, history: history.slice(-10) },
    ).pipe(
      finalize(() => this.isLoading.set(false))
    );
  }
}
