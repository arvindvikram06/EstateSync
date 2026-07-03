import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface PayoutConfirmData {
  method: 'standard' | 'instant';
  amount: number;
  currency: string;
}

@Component({
  selector: 'app-payout-confirm-dialog',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, MatDialogModule, MatButtonModule, MatIconModule],
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
          <div style="font-size:16px;font-weight:700;color:#222">
            Confirm {{ data.method === 'instant' ? 'Instant' : 'Standard' }} Payout
          </div>
          <div style="font-size:12px;color:#717171">
            {{ data.method === 'instant' ? 'Arrives in minutes' : '1–2 business days' }}
          </div>
        </div>
      </div>

      <!-- Breakdown -->
      <div class="rounded-xl overflow-hidden mb-5" style="border:1px solid #EBEBEB">
        <div class="flex items-center justify-between px-4 py-3" style="background:#FAFAFA">
          <span style="font-size:13px;color:#717171">Payout amount</span>
          <span style="font-size:13px;font-weight:600;color:#222">
            {{ data.amount | currency:data.currency:'symbol':'1.2-2' }}
          </span>
        </div>

        @if (data.method === 'instant') {
          <div class="flex items-center justify-between px-4 py-3" style="border-top:1px solid #EBEBEB">
            <span style="font-size:13px;color:#717171">Stripe 1% fee</span>
            <span style="font-size:13px;font-weight:600;color:#DC2626">
              − {{ fee | currency:data.currency:'symbol':'1.2-2' }}
            </span>
          </div>
          <div class="flex items-center justify-between px-4 py-3"
               style="border-top:1px solid #EBEBEB;background:color-mix(in srgb,var(--mat-sys-primary) 4%,white)">
            <span style="font-size:13px;font-weight:700;color:#222">You receive</span>
            <span style="font-size:15px;font-weight:800;color:var(--mat-sys-primary)">
              {{ net | currency:data.currency:'symbol':'1.2-2' }}
            </span>
          </div>
        } @else {
          <div class="flex items-center justify-between px-4 py-3" style="border-top:1px solid #EBEBEB">
            <span style="font-size:13px;color:#717171">Fee</span>
            <span class="es-badge es-badge--approved" style="font-size:11px">Free</span>
          </div>
          <div class="flex items-center justify-between px-4 py-3"
               style="border-top:1px solid #EBEBEB;background:color-mix(in srgb,var(--mat-sys-primary) 4%,white)">
            <span style="font-size:13px;font-weight:700;color:#222">You receive</span>
            <span style="font-size:15px;font-weight:800;color:var(--mat-sys-primary)">
              {{ data.amount | currency:data.currency:'symbol':'1.2-2' }}
            </span>
          </div>
        }
      </div>

      <!-- Info note -->
      <div class="flex items-start gap-2 mb-6" style="font-size:12px;color:#717171">
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

  get fee(): number { return this.data.amount * 0.01; }
  get net(): number { return this.data.amount * 0.99; }
}
