import {
  Component, ChangeDetectionStrategy, inject, signal, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LeaseService } from '@core/services/leasing/lease.service';

export interface VoidLeaseDialogData {
  leaseId: string;
  propertyTitle: string;
}

@Component({
  selector: 'app-void-lease-dialog',
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
    <div class="p-7">
      <div class="flex items-center gap-3 mb-4">
        <div class="flex items-center justify-center w-10 h-10 rounded-full bg-red-50">
          <mat-icon class="!text-[20px] es-text-danger">block</mat-icon>
        </div>
        <h2 class="text-[18px] font-bold text-es-text m-0">Void Lease</h2>
      </div>

      <p class="text-sm text-es-muted mb-5">
        Voiding a lease is permanent. The tenant will be notified. This action applies to
        <strong class="text-es-text">{{ data.propertyTitle }}</strong>.
      </p>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Reason for voiding</mat-label>
          <mat-icon matPrefix>comment</mat-icon>
          <textarea matInput formControlName="reason" rows="3"
            placeholder="Explain why the lease is being voided…"></textarea>
          @if (form.controls.reason.invalid && form.controls.reason.touched) {
            <mat-error>Please provide a reason</mat-error>
          }
        </mat-form-field>

        @if (error()) {
          <p class="text-sm text-red-600 mb-3">{{ error() }}</p>
        }

        <div class="flex gap-3 justify-end mt-3">
          <button type="button" mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>Cancel</button>
          <button type="submit" mat-flat-button
            class="!rounded-full !px-6 !font-semibold"
            style="background:#DC2626;color:white"
            [disabled]="form.invalid || submitting()">
            @if (submitting()) {
              <mat-spinner diameter="16" class="inline-block mr-1.5" />
            }
            Void Lease
          </button>
        </div>
      </form>
    </div>
  `
})
export class VoidLeaseDialogComponent {
  readonly data: VoidLeaseDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef  = inject(MatDialogRef<VoidLeaseDialogComponent>);
  private readonly fb         = inject(FormBuilder);
  private readonly leaseSvc   = inject(LeaseService);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    reason: ['', Validators.required],
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.error.set(null);
    this.leaseSvc.voidLease(this.data.leaseId, { reason: this.form.getRawValue().reason })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.submitting.set(false); this.dialogRef.close(true); },
      error: err => {
        this.submitting.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to void lease.');
      },
    });
  }
}
