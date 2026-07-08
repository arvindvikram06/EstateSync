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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { LeaseService } from '@core/services/leasing/lease.service';
import { formatDate } from '../../../../core/utils/format.utils';

export interface ApproveRenewalDialogData {
  leaseId: string;
  propertyTitle: string;
  startDate: string;
  endDate: string;
}

@Component({
  selector: 'app-approve-renewal-dialog',
  standalone: true,
  providers: [provideNativeDateAdapter()],
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDatepickerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7 min-w-[400px] max-w-[460px]">
      <div class="flex items-center gap-3 mb-4">
        <div class="flex items-center justify-center w-10 h-10 rounded-full es-bg-primary-soft">
          <mat-icon class="!text-[20px] es-text-primary">autorenew</mat-icon>
        </div>
        <h2 class="text-[18px] font-bold text-es-text">Approve Renewal</h2>
      </div>

      <p class="text-sm text-es-muted mb-5">
        Renewing the lease for <strong class="text-es-text">{{ data.propertyTitle }}</strong>.
        The current lease ends on <strong class="text-es-text">{{ currentEndLabel }}</strong> —
        choose the new end date. The tenant will be notified.
      </p>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>New end date</mat-label>
          <mat-icon matPrefix>event</mat-icon>
          <input matInput [matDatepicker]="picker" formControlName="newEndDate" [min]="minDate" />
          <mat-datepicker-toggle matSuffix [for]="picker" />
          <mat-datepicker #picker />
          <mat-hint>Defaults to one full term after the current end date</mat-hint>
          @if (form.controls.newEndDate.invalid && form.controls.newEndDate.touched) {
            <mat-error>A date after {{ currentEndLabel }} is required</mat-error>
          }
        </mat-form-field>

        @if (error()) {
          <p class="text-sm text-red-600 mb-3">{{ error() }}</p>
        }

        <div class="flex gap-3 justify-end mt-4">
          <button type="button" mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>Cancel</button>
          <button type="submit" mat-flat-button color="primary" class="!rounded-full !px-6"
            [disabled]="form.invalid || submitting()">
            @if (submitting()) {
              <mat-spinner diameter="16" class="inline-block mr-1" />
            }
            {{ submitting() ? 'Approving…' : 'Approve renewal' }}
          </button>
        </div>
      </form>
    </div>
  `
})
export class ApproveRenewalDialogComponent {
  readonly data: ApproveRenewalDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef  = inject(MatDialogRef<ApproveRenewalDialogComponent>);
  private readonly fb         = inject(FormBuilder);
  private readonly leaseSvc   = inject(LeaseService);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);

  // New end date must be strictly after the current end
  readonly minDate = new Date(new Date(this.data.endDate).getTime() + 86400000);
  readonly currentEndLabel = formatDate(this.data.endDate);

  // Default: extend by the original lease term
  private readonly defaultEnd = (() => {
    const start = new Date(this.data.startDate).getTime();
    const end   = new Date(this.data.endDate).getTime();
    return new Date(end + (end - start));
  })();

  readonly form = this.fb.nonNullable.group({
    newEndDate: [this.defaultEnd, Validators.required],
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.error.set(null);

    const d = this.form.getRawValue().newEndDate;
    const newEndDate = new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().split('T')[0];

    this.leaseSvc.approveRenewal(this.data.leaseId, { newEndDate })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => { this.submitting.set(false); this.dialogRef.close(true); },
        error: (err) => {
          this.submitting.set(false);
          this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to approve renewal.');
        }
      });
  }
}
