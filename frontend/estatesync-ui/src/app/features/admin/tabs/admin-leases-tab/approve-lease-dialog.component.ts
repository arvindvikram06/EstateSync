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
import { LeaseService } from '../../../../core/services/lease.service';

export interface ApproveLeaseDialogData {
  leaseId: string;
  propertyTitle: string | null;
  tenantName: string | null;
  ownerName: string | null;
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
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7" style="min-width:440px;max-width:520px">
      <div class="flex items-center gap-3 mb-4">
        <div class="flex items-center justify-center w-10 h-10 rounded-full"
          style="background:color-mix(in srgb,var(--mat-sys-primary) 10%,white)">
          <mat-icon style="color:var(--mat-sys-primary)">gavel</mat-icon>
        </div>
        <h2 class="text-[18px] font-bold text-es-text m-0">Review Lease</h2>
      </div>

      <div class="mb-5 p-3 rounded-lg bg-gray-50 text-sm flex flex-col gap-1">
        @if (data.propertyTitle) {
          <div><span class="text-es-muted">Property:</span> <strong>{{ data.propertyTitle }}</strong></div>
        }
        @if (data.tenantName) {
          <div><span class="text-es-muted">Tenant:</span> <strong>{{ data.tenantName }}</strong></div>
        }
        @if (data.ownerName) {
          <div><span class="text-es-muted">Owner:</span> <strong>{{ data.ownerName }}</strong></div>
        }
      </div>

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

        <div class="flex gap-3 justify-end mt-3">
          <button type="button" mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>
            Cancel
          </button>
          <button type="button" mat-stroked-button class="!rounded-full !px-6"
            style="color:#DC2626;border-color:#FECACA"
            [disabled]="submitting()"
            (click)="decide(false)">
            @if (submitting() && lastAction() === 'reject') {
              <mat-spinner diameter="16" style="display:inline-block;margin-right:6px" />
            }
            Reject
          </button>
          <button type="button" mat-flat-button class="!rounded-full !px-6"
            style="background:#059669;color:white"
            [disabled]="submitting()"
            (click)="decide(true)">
            @if (submitting() && lastAction() === 'approve') {
              <mat-spinner diameter="16" style="display:inline-block;margin-right:6px" />
            }
            Approve
          </button>
        </div>
      </form>
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
