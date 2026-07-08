import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ErrorHandlerService {
  private snackBar = inject(MatSnackBar);

  handleError(error: HttpErrorResponse): void {
    let message = 'An unexpected error occurred. Please try again later.';

    if (error.error) {
      // Handle RFC 7807 Problem Details
      if (error.error.detail) {
        message = error.error.detail;
      } else if (error.error.title) {
        message = error.error.title;
      } else if (error.error.message) {
        // Fallback for simple error objects
        message = error.error.message;
      }
    } else if (error.status === 0 || error.status >= 500) {
      message = 'Our servers are currently experiencing issues. Please try again later.';
    } else if (error.status === 404) {
      message = 'The requested resource was not found.';
    } else {
      message = 'An unexpected error occurred. Please try again later.';
    }

    // Dev-only logging — never exposes raw errors in production
    if (!environment.production) {
      console.error('[HTTP Error]', { status: error.status, url: error.url, body: error.error });
    }

    // Show the snackbar
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['error-snackbar']
    });
  }
}
