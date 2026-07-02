import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { NotificationResponse } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/notifications`;

  readonly notifications = signal<NotificationResponse[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  fetchAll(): Observable<ApiResponse<NotificationResponse[]>> {
    return this.http.get<ApiResponse<NotificationResponse[]>>(this.baseUrl).pipe(
      tap(res => { if (res.success) this.notifications.set(res.data); })
    );
  }

  markAllRead(): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.baseUrl}/read`, {}).pipe(
      tap(res => {
        if (res.success) {
          this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
        }
      })
    );
  }

  markOneRead(id: string): Observable<ApiResponse<object>> {
    return this.http.put<ApiResponse<object>>(`${this.baseUrl}/${id}/read`, {}).pipe(
      tap(res => {
        if (res.success) {
          this.notifications.update(list =>
            list.map(n => n.notificationId === id ? { ...n, isRead: true } : n)
          );
        }
      })
    );
  }

  addNotification(notification: NotificationResponse): void {
    this.notifications.update(list => [notification, ...list]);
  }

  /** Clears all in-memory notifications — called on logout to prevent cross-user leakage. */
  clear(): void {
    this.notifications.set([]);
  }
}
