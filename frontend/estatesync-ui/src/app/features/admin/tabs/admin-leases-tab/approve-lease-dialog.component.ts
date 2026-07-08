import {
  Component, ChangeDetectionStrategy, inject, signal, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { LeaseService } from '@core/services/leasing/lease.service';

export interface ApproveLeaseDialogData {
  leaseId: string;
  propertyTitle: string | null;
  propertyAddress: string | null;
  tenantName: string | null;
  tenantEmail: string | null;
  ownerName: string | null;
  startDate: string;
  endDate: string;
  monthlyRent: number;
  securityDeposit: number;
  paymentFrequency: string;
  tenantSignedAt: string | null;
  ownerSignedAt: string | null;
}

@Component({
  selector: 'app-approve-lease-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    DatePipe,
    CurrencyPipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div>

      <!-- Header -->
      <div class="flex items-center gap-3 px-6 pt-6 pb-4"
        style="border-bottom:1px solid #EBEBEB">
        <div class="flex items-center justify-center w-10 h-10 rounded-full flex-shrink-0"
          style="background:color-mix(in srgb,var(--mat-sys-primary) 10%,white)">
          <mat-icon style="color:var(--mat-sys-primary);font-size:20px;width:20px;height:20px">gavel</mat-icon>
        </div>
        <div>
          <h2 class="m-0 text-[17px] font-bold text-es-text">Review Lease</h2>
          <p class="m-0 text-xs text-es-muted">Both parties have signed. Review and take action.</p>
        </div>
      </div>

      <!-- Body -->
      <div class="px-6 py-4 flex flex-col gap-4">

        <!-- Property -->
        <div class="rounded-xl p-4" style="background:#F7F7F7;border:1px solid #EBEBEB">
          <div class="flex items-start gap-3">
            <mat-icon class="!text-[18px] !w-[18px] !h-[18px] mt-[2px] flex-shrink-0 es-text-primary"
             >home</mat-icon>
            <div>
              <div class="text-[13px] font-semibold text-es-text">{{ data.propertyTitle ?? '—' }}</div>
              @if (data.propertyAddress) {
                <div class="text-[12px] text-es-muted mt-0.5">{{ data.propertyAddress }}</div>
              }
            </div>
          </div>
        </div>

        <!-- Parties -->
        <div class="grid grid-cols-2 gap-3">
          <div class="rounded-xl p-3" style="background:#F7F7F7;border:1px solid #EBEBEB">
            <div class="text-[11px] font-semibold text-es-muted uppercase tracking-wide mb-1">Tenant</div>
            <div class="text-[13px] font-semibold text-es-text">{{ data.tenantName ?? '—' }}</div>
            @if (data.tenantEmail) {
              <div class="text-[11px] text-es-muted mt-0.5">{{ data.tenantEmail }}</div>
            }
          </div>
          <div class="rounded-xl p-3" style="background:#F7F7F7;border:1px solid #EBEBEB">
            <div class="text-[11px] font-semibold text-es-muted uppercase tracking-wide mb-1">Owner</div>
            <div class="text-[13px] font-semibold text-es-text">{{ data.ownerName ?? '—' }}</div>
          </div>
        </div>

        <!-- Lease terms -->
        <div class="rounded-xl p-4" style="background:#F7F7F7;border:1px solid #EBEBEB">
          <div class="text-[11px] font-semibold text-es-muted uppercase tracking-wide mb-3">Lease Terms</div>
          <div class="grid grid-cols-2 gap-y-2 gap-x-4 text-[13px]">
            <div>
              <span class="text-es-muted">Start date</span>
              <div class="font-semibold text-es-text">{{ data.startDate | date:'d MMM yyyy' }}</div>
            </div>
            <div>
              <span class="text-es-muted">End date</span>
              <div class="font-semibold text-es-text">{{ data.endDate | date:'d MMM yyyy' }}</div>
            </div>
            <div>
              <span class="text-es-muted">Monthly rent</span>
              <div class="font-semibold text-es-text">{{ data.monthlyRent | currency:'INR':'symbol':'1.0-0' }}</div>
            </div>
            <div>
              <span class="text-es-muted">Security deposit</span>
              <div class="font-semibold text-es-text">{{ data.securityDeposit | currency:'INR':'symbol':'1.0-0' }}</div>
            </div>
            <div>
              <span class="text-es-muted">Payment frequency</span>
              <div class="font-semibold text-es-text">{{ data.paymentFrequency }}</div>
            </div>
          </div>
        </div>

        <!-- Signatures -->
        <div class="grid grid-cols-2 gap-3">
          <div class="rounded-xl p-3 flex items-center gap-2"
            style="background:#F0FDF4;border:1px solid #BBF7D0">
            <mat-icon class="!text-[16px] !w-[16px] !h-[16px] flex-shrink-0 es-text-success">
              verified
            </mat-icon>
            <div>
              <div class="text-[11px] font-semibold es-text-success">Owner signed</div>
              <div class="text-[11px] text-es-muted">{{ data.ownerSignedAt | date:'d MMM yyyy, h:mm a' }}</div>
            </div>
          </div>
          <div class="rounded-xl p-3 flex items-center gap-2"
            style="background:#F0FDF4;border:1px solid #BBF7D0">
            <mat-icon class="!text-[16px] !w-[16px] !h-[16px] flex-shrink-0 es-text-success">
              verified
            </mat-icon>
            <div>
              <div class="text-[11px] font-semibold es-text-success">Tenant signed</div>
              <div class="text-[11px] text-es-muted">{{ data.tenantSignedAt | date:'d MMM yyyy, h:mm a' }}</div>
            </div>
          </div>
        </div>

        <!-- Remarks -->
        <form [formGroup]="form">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Remarks (optional when approving, explain if rejecting)</mat-label>
            <mat-icon matPrefix>comment</mat-icon>
            <textarea matInput formControlName="remarks" rows="3"
              placeholder="Add any notes or reason for your decision…"></textarea>
          </mat-form-field>

          @if (error()) {
            <p class="text-sm text-red-600 mb-3">{{ error() }}</p>
          }
        </form>

      </div>

      <!-- Footer -->
      <div class="flex gap-3 justify-end px-6 pb-6">
        <button type="button" mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>
          Cancel
        </button>
        <button type="button" mat-stroked-button class="!rounded-full !px-6 es-btn-danger"
         
          [disabled]="submitting()"
          (click)="decide(false)">
          @if (submitting() && lastAction() === 'reject') {
            <mat-spinner diameter="16" class="inline-block mr-1.5" />
          }
          Reject
        </button>
        <button type="button" mat-flat-button class="!rounded-full !px-6"
          style="background:#059669;color:white"
          [disabled]="submitting()"
          (click)="decide(true)">
          @if (submitting() && lastAction() === 'approve') {
            <mat-spinner diameter="16" class="inline-block mr-1.5" />
          }
          Approve
        </button>
      </div>

    </div>
  `
})
export class ApproveLeaseDialogComponent {
  readonly data: ApproveLeaseDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef  = inject(MatDialogRef<ApproveLeaseDialogComponent>);
  private readonly fb         = inject(FormBuilder);
  private readonly leaseSvc   = inject(LeaseService);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);
  readonly lastAction = signal<'approve' | 'reject' | null>(null);

  readonly form = this.fb.nonNullable.group({
    remarks: [''],
  });

  decide(approve: boolean): void {
    if (this.submitting()) return;
    this.submitting.set(true);
    this.lastAction.set(approve ? 'approve' : 'reject');
    this.error.set(null);
    const remarks = this.form.getRawValue().remarks || undefined;
    this.leaseSvc.adminApproveLease(this.data.leaseId, { approve, remarks })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.dialogRef.close({ approved: approve });
      },
      error: err => {
        this.submitting.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Action failed.');
      },
    });
  }
}
