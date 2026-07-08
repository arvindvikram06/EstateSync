import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminService } from '@core/services/admin/admin.service';

export interface UpdateComplaintStatusDialogData {
  complaintId: string;
  currentStatus: string;
}

const STATUS_TRANSITIONS: Record<string, string[]> = {
  Open:       ['InProgress'],
  InProgress: ['Resolved'],
  Resolved:   ['Closed'],
  Closed:     [],
};

@Component({
  selector: 'app-admin-update-complaint-status-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div mat-dialog-title class="text-[18px] font-bold text-es-text">Update Complaint Status</div>
    <div style="font-size:13px;color:#717171;margin-bottom:4px">Current: <strong>{{ data.currentStatus }}</strong></div>

    <mat-dialog-content class="!pt-4">
      <div class="opt-label">New Status</div>
      <div class="opt-pills">
        @for (s of nextStatuses; track s) {
          <button type="button" class="opt-pill" [class.opt-pill--active]="statusCtrl.value === s"
            (click)="statusCtrl.setValue(s)">{{ s }}</button>
        }
      </div>
      @if (error()) {
        <div style="font-size:12px;color:#DC2626;margin-top:8px">{{ error() }}</div>
      }
    </mat-dialog-content>

    <mat-dialog-actions align="end" style="padding:16px 24px;gap:8px">
      <button mat-stroked-button class="!rounded-full" mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" class="!rounded-full !font-semibold"
        [disabled]="submitting() || statusCtrl.invalid" (click)="submit()">
        @if (submitting()) { <mat-spinner diameter="16" class="inline-block !mr-2" /> }
        Update
      </button>
    </mat-dialog-actions>
  `,
})
export class AdminUpdateComplaintStatusDialogComponent {
  readonly data        = inject<UpdateComplaintStatusDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef    = inject(MatDialogRef<AdminUpdateComplaintStatusDialogComponent>);
  private readonly adminService = inject(AdminService);
  private readonly destroyRef   = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);

  readonly nextStatuses = STATUS_TRANSITIONS[this.data.currentStatus] ?? [];
  readonly statusCtrl   = new FormControl(this.nextStatuses[0] ?? '', { nonNullable: true, validators: Validators.required });

  submit(): void {
    if (this.statusCtrl.invalid) return;
    this.submitting.set(true);
    this.error.set(null);
    this.adminService.updateComplaintStatus(this.data.complaintId, this.statusCtrl.value)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: () => { this.submitting.set(false); this.dialogRef.close(true); },
      error: (e) => { this.submitting.set(false); this.error.set(e?.error?.message ?? 'Something went wrong.'); },
    });
  }
}
