import {
  Component, ChangeDetectionStrategy, inject, signal, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { timer } from 'rxjs';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidatorFn } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RentalApplicationService } from '../../../core/services/rental-application.service';
import { LoadingButtonComponent } from '../../../shared/components/loading-button/loading-button.component';

function futureDateValidator(): ValidatorFn {
  return (control: AbstractControl) => {
    if (!control.value) return null;
    const today = new Date().toISOString().split('T')[0];
    // MatDatepicker sets the value as a Date object, not a string
    const val = control.value instanceof Date
      ? control.value.toISOString().split('T')[0]
      : control.value as string;
    return val >= today ? null : { pastDate: true };
  };
}

export interface ApplyDialogData {
  propertyId: string;
  propertyTitle: string;
  addressLine: string;
}

import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';

@Component({
  selector: 'app-apply-dialog',
  standalone: true,
  providers: [provideNativeDateAdapter()],
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7" style="min-width:440px;max-width:480px">
      @if (!success()) {
        <h2 class="text-[18px] font-bold text-es-text mb-1">Apply for {{ data.propertyTitle }}</h2>
        <p class="text-[13px] text-es-muted mb-5">{{ data.addressLine }}</p>

        <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Message to owner</mat-label>
            <mat-icon matPrefix>chat</mat-icon>
            <textarea matInput formControlName="message" rows="3" maxlength="500"
              placeholder="Briefly introduce yourself…"></textarea>
            <mat-hint align="end">{{ form.value.message?.length ?? 0 }}/500</mat-hint>
            @if (form.controls.message.invalid && form.controls.message.touched) {
              <mat-error>Message is required</mat-error>
            }
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Expected move-in date</mat-label>
            <input matInput [matDatepicker]="picker" formControlName="expectedMoveInDate" [min]="today" />
            <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
            <mat-datepicker #picker></mat-datepicker>
            @if (form.controls.expectedMoveInDate.invalid && form.controls.expectedMoveInDate.touched) {
              <mat-error>
                @if (form.controls.expectedMoveInDate.errors?.['required']) { Date is required }
                @else { Move-in date must be today or later }
              </mat-error>
            }
          </mat-form-field>

          <div class="grid grid-cols-2 gap-4">
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Duration (months)</mat-label>
              <mat-icon matPrefix>schedule</mat-icon>
              <input matInput type="number" formControlName="durationMonths" min="11" />
              @if (form.controls.durationMonths.invalid && form.controls.durationMonths.touched) {
                <mat-error>Min 11 months</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Commute distance (km)</mat-label>
              <mat-icon matPrefix>directions_car</mat-icon>
              <input matInput type="number" formControlName="commuteDistance" min="0" />
              @if (form.controls.commuteDistance.invalid && form.controls.commuteDistance.touched) {
                <mat-error>Enter distance</mat-error>
              }
            </mat-form-field>
          </div>

          @if (error()) {
            <p class="text-sm text-red-600">{{ error() }}</p>
          }

          <div class="flex gap-3 justify-end mt-2">
            <button type="button" mat-stroked-button mat-dialog-close
              class="!rounded-full !px-6">Cancel</button>
            <app-loading-button
              [loading]="submitting()"
              [disabled]="form.invalid"
              type="submit"
              color="primary"
              variant="flat"
              class="!rounded-full">
              Submit application
            </app-loading-button>
          </div>
        </form>
      } @else {
        <div class="flex flex-col items-center py-6 gap-4">
          <mat-icon class="!text-[52px] !w-[52px] !h-[52px]" style="color:#059669">check_circle</mat-icon>
          <h3 class="text-xl font-bold text-es-text">Application sent!</h3>
          <p class="text-sm text-es-muted text-center">The owner will review your request and get back to you soon.</p>
        </div>
      }
    </div>
  `
})
export class ApplyDialogComponent {
  readonly data: ApplyDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef  = inject(MatDialogRef<ApplyDialogComponent>);
  private readonly fb         = inject(FormBuilder);
  private readonly appService = inject(RentalApplicationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly success = signal(false);
  readonly error = signal<string | null>(null);

  readonly today = new Date();

  readonly form = this.fb.nonNullable.group({
    message: ['', [Validators.required, Validators.maxLength(500)]],
    expectedMoveInDate: ['', [Validators.required, futureDateValidator()]],
    durationMonths: [11, [Validators.required, Validators.min(11)]],
    commuteDistance: [0, [Validators.required, Validators.min(0)]],
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();
    
    // Format Date to YYYY-MM-DD safely
    let formattedDate = v.expectedMoveInDate as any;
    if (formattedDate instanceof Date) {
      formattedDate = new Date(formattedDate.getTime() - formattedDate.getTimezoneOffset() * 60000).toISOString().split('T')[0];
    }
    
    this.appService.applyToProperty(this.data.propertyId, { ...v, expectedMoveInDate: formattedDate })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.success.set(true);
        timer(2200).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.dialogRef.close('applied'));
      },
      error: (err) => {
        this.submitting.set(false);
        const body = err?.error;
        if (body?.errors) {
          const msgs = Object.values(body.errors as Record<string, string[]>).flat();
          this.error.set(msgs[0] ?? 'Failed to submit application.');
        } else {
          this.error.set(body?.detail ?? body?.message ?? 'Failed to submit application.');
        }
      }
    });
  }
}
