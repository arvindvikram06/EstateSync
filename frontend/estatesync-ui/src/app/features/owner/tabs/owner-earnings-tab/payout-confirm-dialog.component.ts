import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { formatUsd } from '@core/utils/format.utils';

export interface PayoutConfirmData {
  method: 'standard' | 'instant';
  amount: number;
  currency: string;
}

@Component({
  selector: 'app-payout-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div style="padding:28px 28px 20px">

      <!-- Header -->
      <div class="flex items-center gap-3 mb-5">
        <div class="w-10 h-10 rounded-xl flex items-center justify-center shrink-0"
             style="background:color-mix(in srgb,var(--mat-sys-primary) 10%,white)">
          <mat-icon style="color:var(--mat-sys-primary);font-size:20px;width:20px;height:20px">
            {{ data.method === 'instant' ? 'bolt' : 'account_balance' }}
          </mat-icon>
        </div>
        <div>
          <div class="text-[16px] font-bold text-es-text">
            Confirm {{ data.method === 'instant' ? 'Instant' : 'Standard' }} Payout
          </div>
          <div class="es-text-xs">
            {{ data.method === 'instant' ? 'Arrives in minutes' : '1–2 business days' }}
          </div>
        </div>
      </div>

      <!-- Breakdown -->
      <div class="rounded-xl overflow-hidden mb-5" style="border:1px solid #EBEBEB">
        <div class="flex items-center justify-between px-4 py-3" style="background:#FAFAFA">
          <span class="es-text-sm">Payout amount</span>
          <span class="es-text-strong">
            {{ usd(data.amount) }}
          </span>
        </div>

        @if (data.method === 'instant') {
          <div class="flex items-center justify-between px-4 py-3" style="border-top:1px solid #EBEBEB">
            <span class="es-text-sm">Stripe 1% fee</span>
            <span class="es-text-strong es-text-danger">
              − {{ usd(fee) }}
            </span>
          </div>
          <div class="flex items-center justify-between px-4 py-3"
               style="border-top:1px solid #EBEBEB;background:color-mix(in srgb,var(--mat-sys-primary) 4%,white)">
            <span class="text-[13px] font-bold text-es-text">You receive</span>
            <span style="font-size:15px;font-weight:800;color:var(--mat-sys-primary)">
              {{ usd(net) }}
            </span>
          </div>
        } @else {
          <div class="flex items-center justify-between px-4 py-3" style="border-top:1px solid #EBEBEB">
            <span class="es-text-sm">Fee</span>
            <span class="es-badge es-badge--approved text-[11px]">Free</span>
          </div>
          <div class="flex items-center justify-between px-4 py-3"
               style="border-top:1px solid #EBEBEB;background:color-mix(in srgb,var(--mat-sys-primary) 4%,white)">
            <span class="text-[13px] font-bold text-es-text">You receive</span>
            <span style="font-size:15px;font-weight:800;color:var(--mat-sys-primary)">
              {{ usd(data.amount) }}
            </span>
          </div>
        }
      </div>

      <!-- Info note -->
      <div class="flex items-start gap-2 mb-6 es-text-xs">
        <mat-icon style="font-size:14px;width:14px;height:14px;margin-top:1px;flex-shrink:0;color:#717171">info</mat-icon>
        @if (data.method === 'instant') {
          Funds will arrive in your bank within minutes. The 1% fee is charged by Stripe and deducted from the payout amount.
        } @else {
          Funds will arrive in 1–2 business days via standard bank transfer. No fee is charged.
        }
      </div>

      <!-- Actions -->
      <div class="flex gap-3">
        <button mat-stroked-button class="!rounded-full flex-1 !h-11"
                (click)="dialogRef.close(false)">
          Cancel
        </button>
        <button mat-flat-button color="primary" class="!rounded-full flex-1 !h-11 !font-semibold"
                (click)="dialogRef.close(true)">
          Confirm Payout
        </button>
      </div>

    </div>
  `,
})
export class PayoutConfirmDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PayoutConfirmDialogComponent>);
  readonly data: PayoutConfirmData = inject(MAT_DIALOG_DATA);
  readonly usd = formatUsd;

  get fee(): number { return this.data.amount * 0.01; }
  get net(): number { return this.data.amount * 0.99; }
}
