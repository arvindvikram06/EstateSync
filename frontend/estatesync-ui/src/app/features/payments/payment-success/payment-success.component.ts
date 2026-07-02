import { Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-payment-success',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (refreshing()) {
      <div class="flex flex-col items-center justify-center min-h-[60vh] gap-5 px-6">
        <mat-spinner diameter="48" />
        <p class="text-sm text-es-muted">Verifying your Stripe account…</p>
      </div>
    } @else {
      <div class="flex flex-col items-center justify-center min-h-[60vh] gap-5 px-6">
        @if (isOnboarding()) {
          @if (onboardingSuccess()) {
            <mat-icon class="!text-[64px] !w-[64px] !h-[64px]" style="color:#059669">check_circle</mat-icon>
            <h1 class="text-2xl font-bold text-es-text">Stripe Account Connected!</h1>
            <p class="text-sm text-es-muted text-center max-w-sm">
              Your Stripe account is now linked. You can start listing properties and receiving rent payments.
            </p>
            <a mat-flat-button color="primary" class="!rounded-full !h-12 !px-8 !font-semibold"
              routerLink="/owner/dashboard" [queryParams]="{ tab: 'overview' }">
              Go to Owner Dashboard
            </a>
          } @else {
            <mat-icon class="!text-[64px] !w-[64px] !h-[64px]" style="color:#F59E0B">pending_actions</mat-icon>
            <h1 class="text-2xl font-bold text-es-text">Onboarding Incomplete</h1>
            <p class="text-sm text-es-muted text-center max-w-sm">
              It looks like your Stripe account connection isn't finished yet.
            </p>
            <a mat-flat-button color="primary" class="!rounded-full !h-12 !px-8 !font-semibold"
              routerLink="/dashboard" [queryParams]="{ tab: 'overview' }">
              Continue Onboarding
            </a>
          }
        } @else {
          <mat-icon class="!text-[64px] !w-[64px] !h-[64px]" style="color:#059669">check_circle</mat-icon>
          <h1 class="text-2xl font-bold text-es-text">Payment Successful!</h1>
          <p class="text-sm text-es-muted text-center max-w-sm">
            Your payment has been processed. It may take a few minutes to reflect in your account.
          </p>
          <a mat-flat-button color="primary" class="!rounded-full !h-12 !px-8 !font-semibold"
            routerLink="/dashboard">
            Go to Dashboard
          </a>
        }
      </div>
    }
  `
})
export class PaymentSuccessComponent implements OnInit {
  private readonly route       = inject(ActivatedRoute);
  private readonly router      = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly destroyRef  = inject(DestroyRef);

  readonly isOnboarding = signal(false);
  readonly refreshing   = signal(false);
  readonly onboardingSuccess = signal(false);

  ngOnInit(): void {
    const type = this.route.snapshot.queryParamMap.get('type');
    if (type === 'onboarding') {
      this.isOnboarding.set(true);
      this.refreshing.set(true);
      this.authService.refreshToken().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.refreshing.set(false);
          this.onboardingSuccess.set(this.authService.currentUser()?.isOwnerVerified ?? false);
        },
        error: () => {
          this.refreshing.set(false);
          this.onboardingSuccess.set(false);
        },
      });
    }
  }
}
