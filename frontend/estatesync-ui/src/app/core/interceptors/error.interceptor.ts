import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, switchMap, throwError, map, finalize, shareReplay } from 'rxjs';
import { AuthService } from '@core/services/auth/auth.service';
import { ErrorHandlerService } from '@core/services/shared/error-handler.service';
import { SKIP_AUTH_HANDLING } from './http-context.tokens';

/**
 * Module-level single-flight refresh. When several requests 401 at once, they all
 * await this one observable instead of each firing its own /refresh-token call —
 * which would rotate the refresh token multiple times and invalidate the session.
 * Reset to null once the refresh settles so the next 401 starts a fresh attempt.
 */
let refresh$: Observable<string> | null = null;

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const errorHandler = inject(ErrorHandlerService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Bootstrap silent-refresh opts out of all handling below.
      if (req.context.get(SKIP_AUTH_HANDLING)) {
        return throwError(() => error);
      }

      if (error.status === 401) {
        // Auth endpoints returning 401 means credentials/refresh are bad — log out directly.
        if (req.url.includes('/auth/')) {
          authService.logout();
          return throwError(() => error);
        }

        // For all other 401s, share a single silent refresh across concurrent requests.
        if (!refresh$) {
          refresh$ = authService.refreshToken().pipe(
            map(res => res.data.accessToken),
            finalize(() => { refresh$ = null; }),
            shareReplay({ bufferSize: 1, refCount: false })
          );
        }

        return refresh$.pipe(
          switchMap(token =>
            next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }))
          ),
          catchError(() => {
            // Refresh also failed — session is truly expired.
            authService.logout();
            return throwError(() => error);
          })
        );
      }

      if (error.status === 403) {
        // RFC 7807 detail means a domain-logic rejection (e.g. KYC not done).
        // Let the calling component handle it — don't redirect.
        if (error.error?.detail) {
          return throwError(() => error);
        }
        router.navigate(['/unauthorized']);
        return throwError(() => error);
      }

      errorHandler.handleError(error);
      return throwError(() => error);
    })
  );
};
