import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from '../services/auth.service';
import { ErrorHandlerService } from '../services/error-handler.service';

describe('errorInterceptor — single-flight refresh', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let refreshSubject: Subject<{ data: { accessToken: string } }>;
  let refreshSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    refreshSubject = new Subject();
    refreshSpy = vi.fn(() => refreshSubject.asObservable());

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { refreshToken: refreshSpy, logout: vi.fn() } },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: ErrorHandlerService, useValue: { handleError: vi.fn() } },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('triggers one refresh for two concurrent 401s and retries both with the new token', () => {
    const results: string[] = [];
    http.get('/api/a', { responseType: 'text' }).subscribe(r => results.push(r as string));
    http.get('/api/b', { responseType: 'text' }).subscribe(r => results.push(r as string));

    // Both original requests fail with 401 while the (pending) refresh is in flight.
    httpMock.expectOne('/api/a').flush('no', { status: 401, statusText: 'Unauthorized' });
    httpMock.expectOne('/api/b').flush('no', { status: 401, statusText: 'Unauthorized' });

    // Single-flight: only one refresh call despite two 401s.
    expect(refreshSpy).toHaveBeenCalledTimes(1);

    // Resolve the shared refresh → both requests retry with the new bearer.
    refreshSubject.next({ data: { accessToken: 'new-token' } });
    refreshSubject.complete();

    const retryA = httpMock.expectOne('/api/a');
    const retryB = httpMock.expectOne('/api/b');
    expect(retryA.request.headers.get('Authorization')).toBe('Bearer new-token');
    expect(retryB.request.headers.get('Authorization')).toBe('Bearer new-token');

    retryA.flush('A');
    retryB.flush('B');

    expect(results).toContain('A');
    expect(results).toContain('B');
  });
});
