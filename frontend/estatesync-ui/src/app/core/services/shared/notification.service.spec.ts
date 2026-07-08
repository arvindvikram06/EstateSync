import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { NotificationService } from './notification.service';
import { NotificationResponse } from '../../models/notification.model';

const BASE = '/api/v1/notifications';

function makeNotification(id: string, isRead: boolean): NotificationResponse {
  return { notificationId: id, isRead, title: 'Test', message: 'Msg', createdAt: new Date().toISOString() } as any;
}

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  // ── fetchAll ───────────────────────────────────────────────────────────────

  it('GETs all notifications and writes them to the notifications signal', () => {
    const data = [makeNotification('n-1', false), makeNotification('n-2', true)];
    service.fetchAll().subscribe();

    const req = httpMock.expectOne(BASE);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data });

    expect(service.notifications()).toEqual(data);
  });

  it('does not update the signal when the response is not successful', () => {
    service.fetchAll().subscribe();

    httpMock.expectOne(BASE).flush({ success: false, data: [] });

    expect(service.notifications()).toEqual([]);
  });

  // ── fetchAllIfStale ────────────────────────────────────────────────────────

  it('fires a new HTTP request when the last fetch is older than maxAgeMs', () => {
    service.fetchAllIfStale(0); // maxAgeMs = 0 → always stale

    httpMock.expectOne(BASE).flush({ success: true, data: [] });
  });

  it('makes no request when the data was fetched recently', () => {
    // Perform an initial fetch to reset the timer.
    service.fetchAll().subscribe();
    httpMock.expectOne(BASE).flush({ success: true, data: [] });

    // Second call within the default 30 s window should be suppressed.
    service.fetchAllIfStale(30_000);
    httpMock.expectNone(BASE);
  });

  // ── markAllRead ────────────────────────────────────────────────────────────

  it('PUTs to the read endpoint and marks all notifications as read', () => {
    service.notifications.set([makeNotification('n-1', false), makeNotification('n-2', false)]);
    service.markAllRead().subscribe();

    const req = httpMock.expectOne(`${BASE}/read`);
    expect(req.request.method).toBe('PUT');
    req.flush({ success: true, data: {} });

    expect(service.notifications().every(n => n.isRead)).toBe(true);
  });

  // ── markOneRead ────────────────────────────────────────────────────────────

  it('PUTs to the individual read endpoint and marks only that notification as read', () => {
    service.notifications.set([makeNotification('n-1', false), makeNotification('n-2', false)]);
    service.markOneRead('n-1').subscribe();

    const req = httpMock.expectOne(`${BASE}/n-1/read`);
    expect(req.request.method).toBe('PUT');
    req.flush({ success: true, data: {} });

    const updated = service.notifications();
    expect(updated.find(n => n.notificationId === 'n-1')!.isRead).toBe(true);
    expect(updated.find(n => n.notificationId === 'n-2')!.isRead).toBe(false);
  });

  // ── addNotification ────────────────────────────────────────────────────────

  it('prepends a new notification to the top of the list', () => {
    service.notifications.set([makeNotification('old', false)]);
    service.addNotification(makeNotification('new', false));

    expect(service.notifications()[0].notificationId).toBe('new');
    expect(service.notifications()).toHaveLength(2);
  });

  // ── clear ──────────────────────────────────────────────────────────────────

  it('clears all notifications from the signal on logout', () => {
    service.notifications.set([makeNotification('n-1', false)]);
    service.clear();

    expect(service.notifications()).toEqual([]);
  });

  // ── unreadCount ────────────────────────────────────────────────────────────

  it('computes the count of unread notifications only', () => {
    service.notifications.set([
      makeNotification('n-1', false),
      makeNotification('n-2', true),
      makeNotification('n-3', false),
    ]);

    expect(service.unreadCount()).toBe(2);
  });

  it('returns 0 when all notifications are read', () => {
    service.notifications.set([makeNotification('n-1', true), makeNotification('n-2', true)]);
    expect(service.unreadCount()).toBe(0);
  });
});
