import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../models/api-response.model';
import { NotificationResponse } from '../../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/v1/notifications`;

  readonly notifications = signal<NotificationResponse[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  private lastFetchedAt = 0;

  fetchAll(): Observable<ApiResponse<NotificationResponse[]>> {
    this.lastFetchedAt = Date.now();
    return this.http.get<ApiResponse<NotificationResponse[]>>(this.baseUrl).pipe(
      tap(res => { if (res.success) this.notifications.set(res.data); })
    );
  }

  /**
   * Refetch only when the last fetch is older than maxAgeMs — guards
   * against refetch storms when SignalR reconnects on a flaky network.
   */
  fetchAllIfStale(maxAgeMs = 30_000): void {
    if (Date.now() - this.lastFetchedAt < maxAgeMs) return;
    this.fetchAll().subscribe();
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
    this.lastFetchedAt = 0; // next login must fetch fresh data immediately
  }
}
