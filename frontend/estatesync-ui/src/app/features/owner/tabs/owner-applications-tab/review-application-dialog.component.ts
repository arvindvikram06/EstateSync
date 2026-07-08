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
import { RentalApplicationService } from '@core/services/leasing/rental-application.service';
import { OwnerApplicationResponse } from '../../../../core/models/rental-application.model';
import { LoadingButtonComponent } from '../../../../shared/components/loading-button/loading-button.component';

export interface ReviewApplicationDialogData {
  application: OwnerApplicationResponse;
  action: 'approve' | 'reject';
}

@Component({
  selector: 'app-review-application-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7">
      <div class="flex items-center gap-3 mb-5">
        <div class="flex items-center justify-center w-10 h-10 rounded-full"
          [style.background]="data.action === 'approve' ? '#D1FAE5' : '#FEE2E2'">
          <mat-icon class="!text-[20px]" [style.color]="data.action === 'approve' ? '#065F46' : '#991B1B'">
            {{ data.action === 'approve' ? 'check_circle' : 'cancel' }}
          </mat-icon>
        </div>
        <h2 class="text-[18px] font-bold text-es-text m-0">
          {{ data.action === 'approve' ? 'Approve Application' : 'Reject Application' }}
        </h2>
      </div>

      <div class="mb-4 p-3 rounded-lg bg-gray-50 flex items-start gap-2 text-sm">
        <mat-icon class="!text-[16px] text-es-muted mt-0.5">person</mat-icon>
        <div>
          <div class="font-semibold text-es-text">{{ data.application.tenantName }}</div>
          <div class="text-es-muted">{{ data.application.propertyTitle }}</div>
        </div>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>{{ data.action === 'reject' ? 'Reason for rejection' : 'Remarks (optional)' }}</mat-label>
          <mat-icon matPrefix>comment</mat-icon>
          <textarea matInput formControlName="remarks" rows="3"
            [placeholder]="data.action === 'reject' ? 'Briefly explain why the application is being rejected…' : 'Optional notes for the tenant…'">
          </textarea>
        </mat-form-field>

        @if (error()) {
          <p class="text-sm text-red-600 mb-3">{{ error() }}</p>
        }

        <div class="flex gap-3 justify-end mt-2">
          <button type="button" mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>Cancel</button>
          <app-loading-button
            [loading]="submitting()"
            type="submit"
            color="primary"
            variant="flat"
            [class]="data.action === 'reject' ? 'reject-btn' : ''">
            {{ data.action === 'approve' ? 'Approve' : 'Reject' }}
          </app-loading-button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .reject-btn ::ng-deep button {
      background: #DC2626 !important;
    }
  `]
})
export class ReviewApplicationDialogComponent {
  readonly data: ReviewApplicationDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef  = inject(MatDialogRef<ReviewApplicationDialogComponent>);
  private readonly fb         = inject(FormBuilder);
  private readonly svc        = inject(RentalApplicationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({ remarks: [''] });

  submit(): void {
    this.submitting.set(true);
    this.error.set(null);
    const { remarks } = this.form.getRawValue();
    this.svc.reviewApplication(this.data.application.applicationId, {
      approve: this.data.action === 'approve',
      remarks: remarks || undefined,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        const updated: OwnerApplicationResponse = {
          ...this.data.application,
          status: this.data.action === 'approve' ? 'Approved' : 'Rejected',
          reviewedAt: new Date().toISOString(),
          reviewRemarks: remarks || null,
        };
        this.dialogRef.close(updated);
      },
      error: err => {
        this.submitting.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to process application.');
      },
    });
  }
}
