import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { interval, switchMap, takeWhile, tap, catchError, of } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { PaymentService } from '../../../core/services/payment.service';

@Component({
  selector: 'app-payment-success',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (verifying()) {
      <div class="flex flex-col items-center justify-center min-h-[60vh] gap-5 px-6">
        <mat-spinner diameter="48" />
        <p class="text-sm text-es-muted text-center">
          {{ isOnboarding() ? 'Verifying your Stripe account…' : 'Confirming your payment…' }}
        </p>
        <p class="text-xs text-es-muted">This usually takes a few seconds.</p>
      </div>
    } @else {
      <div class="flex flex-col items-center justify-center min-h-[60vh] gap-5 px-6">

        @if (isOnboarding()) {
          @if (confirmed()) {
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
          <!-- Rent payment result -->
          @if (confirmed()) {
            <mat-icon class="!text-[64px] !w-[64px] !h-[64px]" style="color:#059669">check_circle</mat-icon>
            <h1 class="text-2xl font-bold text-es-text">Payment Confirmed!</h1>
            <p class="text-sm text-es-muted text-center max-w-sm">
              Your payment has been processed and your bill is now marked as paid.
            </p>
          } @else {
            <!-- Webhook not yet processed after polling timeout — give soft reassurance -->
            <mat-icon class="!text-[64px] !w-[64px] !h-[64px]" style="color:#059669">check_circle</mat-icon>
            <h1 class="text-2xl font-bold text-es-text">Payment Successful!</h1>
            <p class="text-sm text-es-muted text-center max-w-sm">
              Your payment was accepted by Stripe. It may take a minute or two to reflect in your bills.
            </p>
          }
          <a mat-flat-button color="primary" class="!rounded-full !h-12 !px-8 !font-semibold"
            routerLink="/dashboard" [queryParams]="{ tab: 'bills' }">
            Go to Bills
          </a>
        }

      </div>
    }
  `
})
export class PaymentSuccessComponent implements OnInit {
  private readonly route          = inject(ActivatedRoute);
  private readonly authService    = inject(AuthService);
  private readonly paymentService = inject(PaymentService);
  private readonly destroyRef     = inject(DestroyRef);

  readonly isOnboarding = signal(false);
  readonly verifying    = signal(false);
  readonly confirmed    = signal(false);

  // Max polls × interval = 30s total wait before showing the "may take a minute" fallback.
  private readonly POLL_INTERVAL_MS = 3_000;
  private readonly MAX_POLLS        = 10;

  ngOnInit(): void {
    const type   = this.route.snapshot.queryParamMap.get('type');
    const billId = this.route.snapshot.queryParamMap.get('billId');

    if (type === 'onboarding') {
      this.isOnboarding.set(true);
      this.verifying.set(true);
      this.authService.refreshToken()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.confirmed.set(this.authService.currentUser()?.isOwnerVerified ?? false);
            this.verifying.set(false);
          },
          error: () => this.verifying.set(false),
        });
      return;
    }

    // Rent payment: poll bills until the specific bill shows pendingAmount === 0.
    // This bridges the gap between Stripe's redirect (instant) and our webhook processing
    // (asynchronous, typically 2–10s) so tenants don't see "Pending" on return.
    if (type === 'rent' && billId) {
      this.verifying.set(true);
      let pollCount = 0;

      interval(this.POLL_INTERVAL_MS).pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => this.paymentService.getTenantBills().pipe(catchError(() => of(null)))),
        tap(res => {
          pollCount++;
          if (res) {
            const bill = res.data?.find(b => b.leaseBillId === billId);
            if (bill && bill.pendingAmount === 0) {
              this.confirmed.set(true);
            }
          }
        }),
        takeWhile(() => !this.confirmed() && pollCount < this.MAX_POLLS),
      ).subscribe({
        complete: () => this.verifying.set(false),
        error:    () => this.verifying.set(false),
      });
    }
  }
}
