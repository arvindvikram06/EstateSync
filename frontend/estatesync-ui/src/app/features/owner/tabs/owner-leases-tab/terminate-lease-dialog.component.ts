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
import { MatCheckboxModule } from '@angular/material/checkbox';
import { LeaseService } from '@core/services/leasing/lease.service';

export interface TerminateLeaseDialogData {
  leaseId: string;
  propertyTitle: string;
}

@Component({
  selector: 'app-terminate-lease-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7">
      <div class="flex items-center gap-3 mb-4">
        <div class="flex items-center justify-center w-10 h-10 rounded-full bg-red-50">
          <mat-icon class="!text-[20px] es-text-danger">gavel</mat-icon>
        </div>
        <h2 class="text-[18px] font-bold text-es-text m-0">Terminate Lease</h2>
      </div>

      <p class="text-sm text-es-muted mb-5">
        Terminating an active lease is permanent. Both parties will be notified. This applies to
        <strong class="text-es-text">{{ data.propertyTitle }}</strong>.
      </p>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Reason for termination</mat-label>
          <mat-icon matPrefix>comment</mat-icon>
          <textarea matInput formControlName="reason" rows="3"
            placeholder="Explain why the lease is being terminated…"></textarea>
          @if (form.controls.reason.hasError('required') && form.controls.reason.touched) {
            <mat-error>Please provide a reason</mat-error>
          } @else if (form.controls.reason.hasError('minlength') && form.controls.reason.touched) {
            <mat-error>Reason must be at least 10 characters</mat-error>
          } @else if (form.controls.reason.hasError('maxlength') && form.controls.reason.touched) {
            <mat-error>Reason must not exceed 1000 characters</mat-error>
          }
        </mat-form-field>

        <mat-checkbox formControlName="forfeitDeposit" class="mb-2">
          Forfeit the security deposit
        </mat-checkbox>
        <p class="text-[12px] text-es-muted mb-3 mt-0.5">
          Untick if the deposit will be returned to the tenant — the notification wording changes accordingly.
        </p>

        @if (error()) {
          <p class="text-sm text-red-600 mb-3">{{ error() }}</p>
        }

        <div class="flex gap-3 justify-end mt-3">
          <button type="button" mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>Cancel</button>
          <button type="submit" mat-flat-button
            class="!rounded-full !px-6 !font-semibold es-btn-danger-solid"
            [disabled]="form.invalid || submitting()">
            @if (submitting()) {
              <mat-spinner diameter="16" class="inline-block mr-1.5" />
            }
            Terminate Lease
          </button>
        </div>
      </form>
    </div>
  `
})
export class TerminateLeaseDialogComponent {
  readonly data: TerminateLeaseDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef  = inject(MatDialogRef<TerminateLeaseDialogComponent>);
  private readonly fb         = inject(FormBuilder);
  private readonly leaseSvc   = inject(LeaseService);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(1000)]],
    forfeitDeposit: [true],
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const request = this.form.getRawValue();
    request.reason = request.reason.trim();
    if (request.reason.length < 10) {
      this.form.controls.reason.setErrors({
        minlength: { requiredLength: 10, actualLength: request.reason.length }
      });
      this.form.controls.reason.markAsTouched();
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.leaseSvc.terminateLease(this.data.leaseId, request)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.submitting.set(false); this.dialogRef.close(true); },
      error: err => {
        this.submitting.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to terminate lease.');
      },
    });
  }
}
