import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { TokenService } from '../services/token.service';
import { ErrorHandlerService } from '../services/error-handler.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const tokenService = inject(TokenService);
  const errorHandler = inject(ErrorHandlerService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        tokenService.clearToken();
        router.navigate(['/auth/login']);
      }
      if (error.status === 403) {
        // RFC 7807 `detail` means the server gave a domain-logic reason (e.g. income too low).
        // Let the calling component handle it — don't redirect to /unauthorized.
        if (error.error?.detail) {
          return throwError(() => error);
        }
        router.navigate(['/unauthorized']);
        return throwError(() => error);
      }

      // Global Snackbar Error Display
      if (error.status !== 401) {
        errorHandler.handleError(error);
      }
      
      return throwError(() => error);
    })
  );
};
