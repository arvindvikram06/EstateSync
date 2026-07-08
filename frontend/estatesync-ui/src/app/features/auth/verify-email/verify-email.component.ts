import { Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '@core/services/auth/auth.service';

type VerifyState = 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen flex items-center justify-center bg-es-bg px-4">
      <div class="es-card px-8 py-10 max-w-[440px] w-full text-center">

        @switch (state()) {
          @case ('verifying') {
            <mat-spinner diameter="40" class="mx-auto mb-5" aria-label="Verifying email" />
            <h1 class="es-title mb-1">Verifying your email…</h1>
            <p class="es-text-sm">One moment please.</p>
          }
          @case ('success') {
            <div class="w-16 h-16 rounded-full es-bg-green flex items-center justify-center mx-auto mb-5">
              <mat-icon class="!text-[32px] !w-[32px] !h-[32px] es-text-green-deep">check_circle</mat-icon>
            </div>
            <h1 class="es-title mb-1">Email verified!</h1>
            <p class="es-text-sm mb-7">Your account is active — you can sign in now.</p>
            <a mat-flat-button color="primary" routerLink="/auth/login"
              class="!rounded-full !h-12 !px-8 !font-semibold w-full">
              Sign in
            </a>
          }
          @case ('error') {
            <div class="w-16 h-16 rounded-full bg-es-danger-bg flex items-center justify-center mx-auto mb-5">
              <mat-icon class="!text-[32px] !w-[32px] !h-[32px] es-text-danger">error_outline</mat-icon>
            </div>
            <h1 class="es-title mb-1">Verification failed</h1>
            <p class="es-text-sm mb-7">{{ errorMessage() }}</p>
            <a mat-stroked-button routerLink="/auth/login" class="!rounded-full !h-11 !px-8 w-full">
              Back to sign in
            </a>
          }
        }
      </div>
    </div>
  `
})
export class VerifyEmailComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly state = signal<VerifyState>('verifying');
  readonly errorMessage = signal('The verification link is invalid or has expired.');

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.state.set('error');
      return;
    }
    this.authService.verifyEmail(token).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.state.set('success'),
      error: err => {
        this.errorMessage.set(err?.error?.detail ?? err?.error?.message ?? this.errorMessage());
        this.state.set('error');
      },
    });
  }
}
