import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { LeaseBillResponse } from '../../../../core/models/lease-bill.model';
import { formatCurrency } from '../../../../core/utils/format.utils';

@Component({
  selector: 'app-pay-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-6 flex flex-col items-center gap-4" style="max-width:380px">
      <mat-icon class="!text-[36px] !w-[36px] !h-[36px] es-text-primary">payment</mat-icon>
      <h3 class="text-lg font-bold text-es-text text-center">Confirm payment</h3>
      <p class="text-sm text-es-muted text-center">
        You'll be redirected to Stripe to pay
        <strong>{{ formatCurrency(data.bill.pendingAmount) }}</strong>
        for {{ data.bill.billType }} — {{ data.bill.propertyTitle }}.
      </p>
      <div class="flex gap-3 w-full justify-end mt-2">
        <button mat-stroked-button class="!rounded-full" mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" class="!rounded-full" [mat-dialog-close]="true">
          Proceed to Stripe
        </button>
      </div>
    </div>
  `
})
export class PayConfirmDialogComponent {
  readonly data: { bill: LeaseBillResponse } = inject(MAT_DIALOG_DATA);
  readonly formatCurrency = formatCurrency;
}
