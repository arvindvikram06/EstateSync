import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { describe, it, expect, beforeEach } from 'vitest';
import { ErrorHandlerService } from './error-handler.service';

describe('ErrorHandlerService', () => {
  let service: ErrorHandlerService;
  let snackBarOpen: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    snackBarOpen = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        ErrorHandlerService,
        { provide: MatSnackBar, useValue: { open: snackBarOpen } },
      ],
    });

    service = TestBed.inject(ErrorHandlerService);
  });

  function makeError(status: number, body: object | null): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: body, url: '/api/test' });
  }

  it('shows the RFC 7807 detail field when present', () => {
    service.handleError(makeError(400, { detail: 'Email already taken' }));
    expect(snackBarOpen).toHaveBeenCalledWith('Email already taken', 'Close', expect.any(Object));
  });

  it('falls back to the title field when detail is absent', () => {
    service.handleError(makeError(400, { title: 'Bad Request' }));
    expect(snackBarOpen).toHaveBeenCalledWith('Bad Request', 'Close', expect.any(Object));
  });

  it('falls back to the message field when detail and title are absent', () => {
    service.handleError(makeError(400, { message: 'Invalid input' }));
    expect(snackBarOpen).toHaveBeenCalledWith('Invalid input', 'Close', expect.any(Object));
  });

  it('shows a server error message for 500 status with no body', () => {
    service.handleError(makeError(500, null));
    expect(snackBarOpen).toHaveBeenCalledWith(
      expect.stringContaining('servers'),
      'Close',
      expect.any(Object)
    );
  });

  it('shows a server error message when status is 0 (network error)', () => {
    service.handleError(makeError(0, null));
    expect(snackBarOpen).toHaveBeenCalledWith(
      expect.stringContaining('servers'),
      'Close',
      expect.any(Object)
    );
  });

  it('shows a not-found message for 404 with no body', () => {
    service.handleError(makeError(404, null));
    expect(snackBarOpen).toHaveBeenCalledWith(
      expect.stringContaining('not found'),
      'Close',
      expect.any(Object)
    );
  });

  it('uses the error-snackbar panel class', () => {
    service.handleError(makeError(400, { detail: 'Some error' }));
    const options = snackBarOpen.mock.calls[0][2];
    expect(options.panelClass).toContain('error-snackbar');
  });
});
