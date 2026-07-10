import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, switchMap, throwError, map, finalize, shareReplay } from 'rxjs';
import { AuthService } from '@core/services/auth/auth.service';
import { ErrorHandlerService } from '@core/services/shared/error-handler.service';
import { SKIP_AUTH_HANDLING } from './http-context.tokens';

// Shared across concurrent 401s so only one refresh call is made at a time.
let refresh$: Observable<string> | null = null;

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const errorHandler = inject(ErrorHandlerService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (req.context.get(SKIP_AUTH_HANDLING)) {
        return throwError(() => error);
      }

      if (error.status === 401) {
        // Auth endpoints returning 401 = bad credentials/token — log out immediately.
        if (req.url.includes('/auth/')) {
          authService.logout();
          return throwError(() => error);
        }

        // All other 401s: try one silent refresh then retry the original request.
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
            authService.logout();
            return throwError(() => error);
          })
        );
      }

      if (error.status === 403) {
        // domain-logic 403 (e.g. KYC not done) has a detail field — let the component handle it.
        if (error.error?.detail) return throwError(() => error);
        router.navigate(['/unauthorized']);
        return throwError(() => error);
      }

      errorHandler.handleError(error);
      return throwError(() => error);
    })
  );
};
