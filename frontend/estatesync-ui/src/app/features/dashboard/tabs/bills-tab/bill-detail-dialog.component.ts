import { Component, ChangeDetectionStrategy, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, CurrencyPipe, SlicePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LeaseBillResponse } from '../../../../core/models/lease-bill.model';
import { PaymentService } from '../../../../core/services/payment.service';

const BILL_ICONS: Record<string, string> = {
  Rent: 'home',
  Deposit: 'account_balance_wallet',
  Penalty: 'gavel',
  Maintenance: 'build',
  Other: 'receipt',
};

const BILL_COLORS: Record<string, string> = {
  Rent: '#6750A4',
  Deposit: '#0891B2',
  Penalty: '#DC2626',
  Maintenance: '#D97706',
  Other: '#6B7280',
};

@Component({
  selector: 'app-bill-detail-dialog',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, SlicePipe, MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div style="width:480px;max-width:100%;font-family:Inter,sans-serif">

      <!-- Header -->
      <div class="flex items-center gap-4 px-6 pt-6 pb-5"
        style="border-bottom:1px solid #EBEBEB">
        <div class="flex items-center justify-center w-12 h-12 rounded-2xl flex-shrink-0"
          [style.background]="billBg(bill.billType)">
          <mat-icon class="!text-[22px] !w-[22px] !h-[22px]"
            [style.color]="billColor(bill.billType)">{{ billIcon(bill.billType) }}</mat-icon>
        </div>
        <div class="flex-1 min-w-0">
          <div style="font-size:17px;font-weight:700;color:#222">{{ bill.billType }} Bill</div>
          <div style="font-size:13px;color:#717171;margin-top:1px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{{ bill.propertyTitle }}</div>
        </div>
        <button mat-icon-button mat-dialog-close style="color:#AAAAAA;margin-right:-8px">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <!-- Amount hero -->
      <div class="px-6 py-5" style="background:#F7F7F7;border-bottom:1px solid #EBEBEB">
        <div class="flex items-end justify-between">
          <div>
            <div style="font-size:11px;font-weight:600;color:#717171;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px">
              {{ bill.status === 'Paid' ? 'Amount Paid' : 'Amount Due' }}
            </div>
            <div style="font-size:28px;font-weight:800;color:#222;letter-spacing:-.5px">
              {{ (bill.status === 'Paid' ? bill.amount : bill.pendingAmount) | currency:'INR':'symbol':'1.0-0' }}
            </div>
            @if (bill.status === 'Pending' && bill.amount !== bill.pendingAmount) {
              <div style="font-size:12px;color:#717171;margin-top:2px">
                Total billed: {{ bill.amount | currency:'INR':'symbol':'1.0-0' }}
              </div>
            }
          </div>
          <div class="flex flex-col items-end gap-1">
            <span class="es-badge"
              [class.es-badge--approved]="bill.status === 'Paid'"
              [class.es-badge--pending]="bill.status === 'Pending'"
              [class.es-badge--rejected]="bill.status === 'Failed'"
              style="font-size:13px;padding:4px 14px">
              {{ bill.status }}
            </span>
          </div>
        </div>
      </div>

      <!-- Detail grid -->
      <div class="px-6 py-5">
        <div style="display:grid;grid-template-columns:1fr 1fr;gap:20px">

          <div>
            <div style="font-size:11px;font-weight:600;color:#AAAAAA;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px">Bill Type</div>
            <div class="flex items-center gap-1.5">
              <mat-icon class="!text-[14px] !w-[14px] !h-[14px]" [style.color]="billColor(bill.billType)">{{ billIcon(bill.billType) }}</mat-icon>
              <span style="font-size:13px;font-weight:600;color:#222">{{ bill.billType }}</span>
            </div>
          </div>

          <div>
            <div style="font-size:11px;font-weight:600;color:#AAAAAA;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px">Due Date</div>
            <div class="flex items-center gap-1.5">
              <mat-icon class="!text-[14px] !w-[14px] !h-[14px]" style="color:#717171">calendar_today</mat-icon>
              <span style="font-size:13px;font-weight:600;color:#222">{{ bill.dueDate | date:'d MMM yyyy' }}</span>
            </div>
          </div>

          @if (bill.paidAt) {
            <div>
              <div style="font-size:11px;font-weight:600;color:#AAAAAA;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px">Paid On</div>
              <div class="flex items-center gap-1.5">
                <mat-icon class="!text-[14px] !w-[14px] !h-[14px]" style="color:#059669">check_circle</mat-icon>
                <span style="font-size:13px;font-weight:600;color:#059669">{{ bill.paidAt | date:'d MMM yyyy, h:mm a' }}</span>
              </div>
            </div>
          }

          <div>
            <div style="font-size:11px;font-weight:600;color:#AAAAAA;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px">Property</div>
            <div class="flex items-center gap-1.5">
              <mat-icon class="!text-[14px] !w-[14px] !h-[14px]" style="color:#717171">apartment</mat-icon>
              <span style="font-size:13px;color:#444;overflow:hidden;text-overflow:ellipsis;white-space:nowrap">{{ bill.propertyTitle }}</span>
            </div>
          </div>

          <div>
            <div style="font-size:11px;font-weight:600;color:#AAAAAA;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px">Lease Reference</div>
            <div style="font-size:12px;color:#717171;font-family:'Roboto Mono',monospace;letter-spacing:.3px">
              #{{ bill.leaseId | slice:0:8 }}…
            </div>
          </div>

        </div>

        <!-- Payment note for pending -->
        @if (bill.status === 'Pending') {
          <div class="flex items-start gap-3 mt-5 rounded-xl p-3"
            style="background:#FFFBEB;border:1px solid #FDE68A">
            <mat-icon class="!text-[16px] !w-[16px] !h-[16px] mt-[1px] flex-shrink-0" style="color:#D97706">schedule</mat-icon>
            <div style="font-size:12px;color:#92400E;line-height:1.5">
              Payment is due by <strong>{{ bill.dueDate | date:'d MMM yyyy' }}</strong>. You will be redirected to Stripe to complete the payment securely.
            </div>
          </div>
        }

        @if (bill.status === 'Paid') {
          <div class="flex items-start gap-3 mt-5 rounded-xl p-3"
            style="background:#F0FDF4;border:1px solid #BBF7D0">
            <mat-icon class="!text-[16px] !w-[16px] !h-[16px] mt-[1px] flex-shrink-0" style="color:#059669">verified</mat-icon>
            <div style="font-size:12px;color:#065F46;line-height:1.5">
              Payment received and confirmed. Your receipt will be available through Stripe.
            </div>
          </div>
        }

        @if (error()) {
          <div class="mt-3" style="font-size:12px;color:#DC2626">{{ error() }}</div>
        }
      </div>

      <!-- Footer -->
      <div class="flex gap-3 justify-end px-6 pb-6 pt-0">
        <button mat-stroked-button class="!rounded-full !px-5" mat-dialog-close>Close</button>
        @if (bill.status === 'Pending') {
          <button mat-flat-button color="primary" class="!rounded-full !px-6 !font-semibold"
            [disabled]="paying()"
            (click)="pay()">
            @if (paying()) {
              <mat-spinner diameter="16" style="display:inline-block;margin-right:6px" />
            } @else {
              <mat-icon class="!text-[16px] !w-[16px] !h-[16px] mr-1">payment</mat-icon>
            }
            Pay {{ bill.pendingAmount | currency:'INR':'symbol':'1.0-0' }}
          </button>
        }
      </div>

    </div>
  `,
})
export class BillDetailDialogComponent {
  readonly bill: LeaseBillResponse = inject(MAT_DIALOG_DATA);
  private readonly svc        = inject(PaymentService);
  private readonly dialogRef  = inject(MatDialogRef<BillDetailDialogComponent>);
  private readonly destroyRef = inject(DestroyRef);

  readonly paying = signal(false);
  readonly error  = signal<string | null>(null);

  billIcon(type: string): string { return BILL_ICONS[type] ?? 'receipt'; }
  billColor(type: string): string { return BILL_COLORS[type] ?? '#6B7280'; }
  billBg(type: string): string {
    const c = BILL_COLORS[type] ?? '#6B7280';
    return `color-mix(in srgb, ${c} 12%, white)`;
  }

  pay(): void {
    this.paying.set(true);
    this.error.set(null);
    this.svc.createCheckoutSession(this.bill.leaseBillId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { window.location.href = res.data.url; },
        error: () => {
          this.paying.set(false);
          this.error.set('Failed to start payment. Please try again.');
        },
      });
  }
}
