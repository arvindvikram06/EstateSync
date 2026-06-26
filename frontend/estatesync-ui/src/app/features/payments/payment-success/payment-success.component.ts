import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-payment-success',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center min-h-[60vh] gap-5 px-6">
      <mat-icon class="!text-[64px] !w-[64px] !h-[64px]" style="color:#059669">check_circle</mat-icon>
      <h1 class="text-2xl font-bold text-es-text">Payment Successful!</h1>
      <p class="text-sm text-es-muted text-center max-w-sm">
        Your payment has been processed. It may take a few minutes to reflect in your account.
      </p>
      <a mat-flat-button color="primary" class="!rounded-full !h-12 !px-8 !font-semibold" routerLink="/dashboard">
        Go to Dashboard
      </a>
    </div>
  `
})
export class PaymentSuccessComponent {}
