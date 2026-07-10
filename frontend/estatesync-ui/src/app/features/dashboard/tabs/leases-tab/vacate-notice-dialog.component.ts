import { DecimalPipe } from '@angular/common';
import {
  Component, ChangeDetectionStrategy, inject, signal, computed, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LeaseService } from '@core/services/leasing/lease.service';

export interface VacateNoticeDialogData {
  leaseId: string;
  propertyTitle: string;
  noticePeriodDays: number | null;
  penaltyPercentage: number | null;
  securityDeposit: number;
}

@Component({
  selector: 'app-vacate-notice-dialog',
  standalone: true,
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatProgressSpinnerModule,
  ],
  providers: [provideNativeDateAdapter()],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7" style="min-width:360px">
      <div class="flex items-center gap-3 mb-4">
        <div class="flex items-center justify-center w-10 h-10 rounded-full bg-amber-50">
          <mat-icon class="!text-[20px]" style="color:var(--mat-sys-primary)">exit_to_app</mat-icon>
        </div>
        <h2 class="text-[18px] font-bold text-es-text m-0">Notice to Vacate</h2>
      </div>

      <p class="text-sm text-es-muted mb-1">
        Submitting a vacate notice for <strong class="text-es-text">{{ data.propertyTitle }}</strong>.
        The owner will be notified and must accept your request.
      </p>

      @if (data.noticePeriodDays) {
        <p class="text-sm text-es-muted mb-4">
          Notice period: <strong class="text-es-text">{{ data.noticePeriodDays }} days</strong>.
          Exit on or after <strong class="text-es-text">{{ earliestNoPenaltyDate() }}</strong> to avoid penalties.
        </p>
      }

      <form [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Requested exit date</mat-label>
          <mat-icon matPrefix>calendar_today</mat-icon>
          <input matInput [matDatepicker]="picker" formControlName="exitDate" [min]="tomorrow" />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
          @if (form.controls.exitDate.invalid && form.controls.exitDate.touched) {
            <mat-error>Please select a valid future date</mat-error>
          }
        </mat-form-field>

        @if (penaltyWarning()) {
          <div class="rounded-xl p-4 mb-4" style="background:#FEF3C7;border:1px solid #FDE68A">
            <div class="flex items-start gap-2">
              <mat-icon class="!text-[18px] shrink-0" style="color:#92400E">warning</mat-icon>
              <p class="text-sm m-0" style="color:#92400E">
                This exit date does not meet the {{ data.noticePeriodDays }}-day notice requirement.
                A penalty of <strong>₹{{ penaltyAmount() | number:'1.0-0' }}</strong>
                ({{ data.penaltyPercentage }}% of deposit) will apply if the owner accepts.
              </p>
            </div>
          </div>
        }

        @if (error()) {
          <p class="text-sm mb-3" style="color:#DC2626">{{ error() }}</p>
        }

        <div class="flex gap-3 justify-end mt-3">
          <button type="button" mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>Cancel</button>
          <button type="submit" mat-flat-button color="primary"
            class="!rounded-full !px-6 !font-semibold"
            [disabled]="form.invalid || submitting()">
            @if (submitting()) {
              <mat-spinner diameter="16" class="inline-block mr-1.5" />
            }
            Submit Notice
          </button>
        </div>
      </form>
    </div>
  `
})
export class VacateNoticeDialogComponent {
  readonly data: VacateNoticeDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef  = inject(MatDialogRef<VacateNoticeDialogComponent>);
  private readonly fb         = inject(FormBuilder);
  private readonly leaseSvc   = inject(LeaseService);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);

  readonly tomorrow = (() => { const d = new Date(); d.setDate(d.getDate() + 1); return d; })();

  readonly form = this.fb.nonNullable.group({
    exitDate: [null as Date | null, Validators.required],
  });

  readonly selectedDate = toSignal(this.form.controls.exitDate.valueChanges, { initialValue: null as Date | null });

  readonly earliestNoPenaltyDate = computed(() => {
    const days = this.data.noticePeriodDays ?? 0;
    const d = new Date();
    d.setDate(d.getDate() + days);
    return d.toLocaleDateString('en-IN', { day: 'numeric', month: 'short', year: 'numeric' });
  });

  readonly penaltyWarning = computed(() => {
    const exit = this.selectedDate();
    if (!exit || !this.data.noticePeriodDays) return false;
    const earliest = new Date();
    earliest.setDate(earliest.getDate() + this.data.noticePeriodDays);
    return exit < earliest;
  });

  readonly penaltyAmount = computed(() => {
    if (!this.data.penaltyPercentage) return 0;
    return Math.round(this.data.securityDeposit * this.data.penaltyPercentage / 100);
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const exitDate = this.selectedDate() ?? this.form.controls.exitDate.value as Date | null;
    if (!exitDate) return;

    this.submitting.set(true);
    this.error.set(null);
    const iso = exitDate.toISOString().split('T')[0];
    this.leaseSvc.submitVacateNotice(this.data.leaseId, { requestedExitDate: iso })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.submitting.set(false); this.dialogRef.close(res.data); },
        error: err => {
          this.submitting.set(false);
          this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to submit notice.');
        },
      });
  }
}
