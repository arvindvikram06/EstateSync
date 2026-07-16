import {
  Component, ChangeDetectionStrategy, inject, signal, DestroyRef
} from '@angular/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { LeaseService } from '@core/services/leasing/lease.service';
import { LoadingButtonComponent } from '../../../../shared/components/loading-button/loading-button.component';

function leaseFormValidator(group: AbstractControl): ValidationErrors | null {
  const errors: ValidationErrors = {};

  const start = group.get('startDate')?.value;
  const end   = group.get('endDate')?.value;
  if (start && end) {
    const s = new Date(start);
    const e = new Date(end);
    if (e <= s) {
      errors['endBeforeStart'] = true;
    } else {
      const minEnd = new Date(s);
      minEnd.setMonth(minEnd.getMonth() + 11);
      if (e < minEnd) errors['tooShort'] = true;
    }
  }

  const rent    = Number(group.get('monthlyRent')?.value ?? 0);
  const deposit = Number(group.get('securityDeposit')?.value ?? 0);
  if (rent > 0 && deposit <= rent) errors['depositTooLow'] = true;

  return Object.keys(errors).length ? errors : null;
}

export interface CreateLeaseDialogData {
  applicationId: string;
  tenantName: string;
  propertyTitle: string;
  monthlyRent: number;
}

@Component({
  selector: 'app-create-lease-dialog',
  standalone: true,
  providers: [provideNativeDateAdapter()],
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7">
      <h2 class="text-[18px] font-bold text-es-text mb-2">Create Lease Agreement</h2>

      <div class="mb-5 p-3 rounded-lg bg-gray-50 grid grid-cols-2 gap-3 text-sm">
        <div>
          <div class="text-xs text-es-muted mb-0.5">Tenant</div>
          <div class="font-semibold text-es-text leading-tight">{{ data.tenantName }}</div>
        </div>
        <div>
          <div class="text-xs text-es-muted mb-0.5">Property</div>
          <div class="font-semibold text-es-text leading-tight">{{ data.propertyTitle }}</div>
        </div>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">

        <div class="grid grid-cols-2 gap-4">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Start Date</mat-label>
            <input matInput [matDatepicker]="startPicker" formControlName="startDate" [min]="today" />
            <mat-datepicker-toggle matIconSuffix [for]="startPicker"></mat-datepicker-toggle>
            <mat-datepicker #startPicker></mat-datepicker>
            @if (form.controls.startDate.invalid && form.controls.startDate.touched) {
              <mat-error>Required</mat-error>
            }
          </mat-form-field>
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>End Date</mat-label>
            <input matInput [matDatepicker]="endPicker" formControlName="endDate" [min]="today" />
            <mat-datepicker-toggle matIconSuffix [for]="endPicker"></mat-datepicker-toggle>
            <mat-datepicker #endPicker></mat-datepicker>
            @if (form.controls.endDate.invalid && form.controls.endDate.touched) {
              <mat-error>Required</mat-error>
            }
          </mat-form-field>
        </div>
        @if (form.errors?.['endBeforeStart'] && form.controls.endDate.touched) {
          <p class="text-xs text-red-600 -mt-2">End date must be after start date.</p>
        }
        @if (form.errors?.['tooShort'] && form.controls.endDate.touched) {
          <p class="text-xs text-red-600 -mt-2">Lease must be at least 11 months.</p>
        }

        <div class="grid grid-cols-2 gap-4">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Monthly Rent (₹)</mat-label>
            <mat-icon matPrefix>currency_rupee</mat-icon>
            <input matInput type="number" formControlName="monthlyRent" min="1" />
            @if (form.controls.monthlyRent.touched) {
              @if (form.controls.monthlyRent.hasError('required')) {
                <mat-error>Monthly rent is required</mat-error>
              } @else if (form.controls.monthlyRent.hasError('min')) {
                <mat-error>Must be greater than 0</mat-error>
              }
            }
          </mat-form-field>
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Security Deposit (₹)</mat-label>
            <mat-icon matPrefix>savings</mat-icon>
            <input matInput type="number" formControlName="securityDeposit" min="0" />
            @if (form.controls.securityDeposit.touched) {
              @if (form.controls.securityDeposit.hasError('required')) {
                <mat-error>Security deposit is required</mat-error>
              } @else if (form.errors?.['depositTooLow']) {
                <mat-error>Must be greater than monthly rent</mat-error>
              }
            }
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Payment Frequency</mat-label>
          <mat-icon matPrefix>repeat</mat-icon>
          <mat-select formControlName="paymentFrequency">
            <mat-option value="Monthly">Monthly</mat-option>
            <mat-option value="Quarterly">Quarterly</mat-option>
            <mat-option value="HalfYearly">Half Yearly</mat-option>
            <mat-option value="Yearly">Yearly</mat-option>
          </mat-select>
          @if (form.controls.paymentFrequency.touched && form.controls.paymentFrequency.hasError('required')) {
            <mat-error>Payment frequency is required</mat-error>
          }
        </mat-form-field>

        <div class="grid grid-cols-2 gap-4">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Penalty % (late payment)</mat-label>
            <mat-icon matPrefix>percent</mat-icon>
            <input matInput type="number" formControlName="penaltyPercentage" min="0" max="10" />
            @if (form.controls.penaltyPercentage.touched) {
              @if (form.controls.penaltyPercentage.hasError('min')) {
                <mat-error>Cannot be negative</mat-error>
              } @else if (form.controls.penaltyPercentage.hasError('max')) {
                <mat-error>Max 10%</mat-error>
              }
            }
          </mat-form-field>
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Notice Period (days)</mat-label>
            <mat-icon matPrefix>lock</mat-icon>
            <input matInput type="number" formControlName="noticePeriodDays" min="0" />
            @if (form.controls.noticePeriodDays.touched) {
              @if (form.controls.noticePeriodDays.hasError('required')) {
                <mat-error>Notice period is required</mat-error>
              } @else if (form.controls.noticePeriodDays.hasError('min')) {
                <mat-error>Cannot be negative</mat-error>
              }
            }
          </mat-form-field>
        </div>

        @if (error()) {
          <p class="text-sm text-red-600">{{ error() }}</p>
        }

        <div class="flex gap-3 justify-end mt-1">
          <button type="button" mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>Cancel</button>
          <app-loading-button
            [loading]="submitting()"
            [disabled]="form.invalid"
            type="submit"
            color="primary"
            variant="flat">
            Create Lease
          </app-loading-button>
        </div>
      </form>
    </div>
  `
})
export class CreateLeaseDialogComponent {
  readonly data: CreateLeaseDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef  = inject(MatDialogRef<CreateLeaseDialogComponent>);
  private readonly fb         = inject(FormBuilder);
  private readonly leaseSvc   = inject(LeaseService);
  private readonly router     = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);
  readonly today      = new Date();

  readonly form = this.fb.nonNullable.group({
    startDate:         ['', Validators.required],
    endDate:           ['', Validators.required],
    monthlyRent:       [this.data.monthlyRent, [Validators.required, Validators.min(1)]],
    securityDeposit:   [0, [Validators.required, Validators.min(0)]],
    paymentFrequency:  ['Monthly', Validators.required],
    penaltyPercentage: [2, [Validators.min(0), Validators.max(10)]],
    noticePeriodDays:  [30, [Validators.required, Validators.min(0)]],
  }, { validators: leaseFormValidator });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();
    
    const fmtDate = (d: string | Date): string => {
      if (d instanceof Date) {
        return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().split('T')[0];
      }
      return d;
    };

    this.leaseSvc.createLease({
      applicationId:     this.data.applicationId,
      startDate:         fmtDate(v.startDate),
      endDate:           fmtDate(v.endDate),
      monthlyRent:       v.monthlyRent,
      securityDeposit:   v.securityDeposit,
      paymentFrequency:  v.paymentFrequency,
      penaltyPercentage: v.penaltyPercentage,
      noticePeriodDays:  v.noticePeriodDays,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.dialogRef.close(true);
        this.router.navigate(['/owner/dashboard'], { queryParams: { tab: 'leases' } });
      },
      error: err => {
        this.submitting.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to create lease.');
      },
    });
  }
}
