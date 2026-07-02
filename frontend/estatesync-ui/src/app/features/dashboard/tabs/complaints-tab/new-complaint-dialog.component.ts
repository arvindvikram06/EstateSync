import {
  Component, ChangeDetectionStrategy, inject, signal, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ComplaintService } from '../../../../core/services/complaint.service';
import { ComplaintResponse } from '../../../../core/models/complaint.model';
import { LoadingButtonComponent } from '../../../../shared/components/loading-button/loading-button.component';

export interface NewComplaintDialogData {
  propertyId: string;
  propertyTitle: string;
}

@Component({
  selector: 'app-new-complaint-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7" style="min-width:440px;max-width:520px">
      <h2 class="text-[18px] font-bold text-es-text mb-5">New Complaint</h2>

      <div class="flex items-center gap-2 bg-gray-50 rounded-lg px-4 py-3 mb-5 text-sm text-es-muted">
        <mat-icon class="!text-[16px]">home</mat-icon>
        Filing for: <strong class="text-es-text ml-1">{{ data.propertyTitle }}</strong>
      </div>

      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Title</mat-label>
          <mat-icon matPrefix>edit</mat-icon>
          <input matInput formControlName="title" maxlength="120" placeholder="Brief summary of the issue" />
          <mat-hint align="end">{{ form.value.title?.length ?? 0 }}/120</mat-hint>
          @if (form.controls.title.invalid && form.controls.title.touched) {
            <mat-error>Title is required</mat-error>
          }
        </mat-form-field>

        <div class="grid grid-cols-2 gap-4">
          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Type</mat-label>
            <mat-icon matPrefix>category</mat-icon>
            <mat-select formControlName="type">
              @for (t of types; track t) {
                <mat-option [value]="t">{{ t }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline" class="w-full">
            <mat-label>Priority</mat-label>
            <mat-icon matPrefix>priority_high</mat-icon>
            <mat-select formControlName="priority">
              @for (p of priorities; track p) {
                <mat-option [value]="p">{{ p }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
        </div>

        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Description</mat-label>
          <mat-icon matPrefix>description</mat-icon>
          <textarea matInput formControlName="description" rows="4"
            placeholder="Describe the issue in detail…"></textarea>
          @if (form.controls.description.invalid && form.controls.description.touched) {
            <mat-error>At least 20 characters required</mat-error>
          }
        </mat-form-field>

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
            variant="flat"
            class="!rounded-full">
            Submit complaint
          </app-loading-button>
        </div>
      </form>
    </div>
  `
})
export class NewComplaintDialogComponent {
  readonly data: NewComplaintDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef  = inject(MatDialogRef<NewComplaintDialogComponent>);
  private readonly fb         = inject(FormBuilder);
  private readonly svc        = inject(ComplaintService);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);

  readonly types      = ['Maintenance', 'Billing', 'Noise', 'Safety', 'Cleanliness', 'Other'] as const;
  readonly priorities = ['Low', 'Medium', 'High', 'Critical'] as const;

  readonly form = this.fb.nonNullable.group({
    title:      ['', [Validators.required, Validators.maxLength(120)]],
    type:       ['Maintenance' as ComplaintResponse['type'], Validators.required],
    priority:   ['Medium' as ComplaintResponse['priority'], Validators.required],
    description:['', [Validators.required, Validators.minLength(20)]],
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();
    this.svc.createComplaint({
      propertyId:  this.data.propertyId,
      title:       v.title,
      description: v.description,
      type:        v.type,
      priority:    v.priority,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.submitting.set(false); this.dialogRef.close(res.data); },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to submit complaint.');
      }
    });
  }
}
