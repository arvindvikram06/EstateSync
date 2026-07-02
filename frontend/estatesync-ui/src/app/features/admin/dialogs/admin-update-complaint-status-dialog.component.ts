import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminService } from '../../../core/services/admin.service';

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
    MatFormFieldModule, MatSelectModule, MatIconModule, MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div mat-dialog-title style="font-size:18px;font-weight:700;color:#222">Update Complaint Status</div>
    <div style="font-size:13px;color:#717171;margin-bottom:4px">Current: <strong>{{ data.currentStatus }}</strong></div>

    <mat-dialog-content style="padding-top:16px;min-width:360px">
      <mat-form-field appearance="outline" class="w-full">
        <mat-label>New Status</mat-label>
        <mat-icon matPrefix class="mr-2">update</mat-icon>
        <mat-select [formControl]="statusCtrl">
          @for (s of nextStatuses; track s) {
            <mat-option [value]="s">{{ s }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      @if (error()) {
        <div style="font-size:12px;color:#DC2626;margin-top:4px">{{ error() }}</div>
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

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);

  readonly nextStatuses = STATUS_TRANSITIONS[this.data.currentStatus] ?? [];
  readonly statusCtrl   = new FormControl(this.nextStatuses[0] ?? '', { nonNullable: true, validators: Validators.required });

  submit(): void {
    if (this.statusCtrl.invalid) return;
    this.submitting.set(true);
    this.error.set(null);
    this.adminService.updateComplaintStatus(this.data.complaintId, this.statusCtrl.value).subscribe({
      next: () => { this.submitting.set(false); this.dialogRef.close(true); },
      error: (e) => { this.submitting.set(false); this.error.set(e?.error?.message ?? 'Something went wrong.'); },
    });
  }
}
