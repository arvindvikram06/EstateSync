import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-payment-cancel',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center min-h-[60vh] gap-5 px-6">
      <mat-icon class="!text-[64px] !w-[64px] !h-[64px] text-red-400">cancel</mat-icon>
      <h1 class="text-2xl font-bold text-es-text">Payment Cancelled</h1>
      <p class="text-sm text-es-muted text-center max-w-sm">
        Your payment was not completed. You can try again from the Bills tab in your dashboard.
      </p>
      <a mat-flat-button color="primary" class="!rounded-full !h-12 !px-8 !font-semibold" routerLink="/dashboard">
        Go to Dashboard
      </a>
    </div>
  `
})
export class PaymentCancelComponent {}
