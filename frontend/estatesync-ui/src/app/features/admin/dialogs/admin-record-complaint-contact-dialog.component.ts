import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminService } from '@core/services/admin/admin.service';

export interface RecordComplaintContactDialogData {
  complaintId: string;
  complaintTitle: string;
  ownerName: string;
}

@Component({
  selector: 'app-admin-record-complaint-contact-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title class="!font-bold">Record owner contact</h2>
    <mat-dialog-content>
      <p class="m-0 mb-1 text-sm text-es-text">
        Record the outcome of contacting <strong>{{ data.ownerName }}</strong>.
      </p>
      <p class="m-0 mb-5 text-xs text-es-muted">{{ data.complaintTitle }}</p>

      <mat-form-field appearance="outline" class="w-full">
        <mat-label>Why was the complaint not opened?</mat-label>
        <textarea matInput rows="5" maxlength="1000" [formControl]="reason"
          placeholder="Example: Owner was travelling and confirmed they will review it today."></textarea>
        <mat-hint align="end">{{ reason.value.length }}/1000</mat-hint>
        @if (reason.touched && reason.hasError('required')) {
          <mat-error>A reason is required</mat-error>
        }
      </mat-form-field>

      @if (error()) {
        <p class="m-0 mt-2 text-xs text-red-600">{{ error() }}</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end" class="!px-6 !pb-5">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" class="!rounded-full"
        [disabled]="reason.invalid || submitting()" (click)="submit()">
        @if (submitting()) {
          <mat-spinner diameter="16" class="inline-block !mr-2" />
        }
        Save contact entry
      </button>
    </mat-dialog-actions>
  `,
})
export class AdminRecordComplaintContactDialogComponent {
  readonly data = inject<RecordComplaintContactDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<AdminRecordComplaintContactDialogComponent>);
  private readonly adminService = inject(AdminService);
  private readonly destroyRef = inject(DestroyRef);

  readonly reason = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(1000)],
  });
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  submit(): void {
    this.reason.markAsTouched();
    if (this.reason.invalid) return;

    this.submitting.set(true);
    this.error.set(null);
    this.adminService.recordComplaintContact(this.data.complaintId, this.reason.value.trim())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.dialogRef.close(this.reason.value.trim()),
        error: error => {
          this.submitting.set(false);
          this.error.set(error?.error?.message ?? 'Could not save the contact entry.');
        },
      });
  }
}
