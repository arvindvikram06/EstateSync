import {
  Component, ChangeDetectionStrategy, inject, signal, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { from, of } from 'rxjs';
import { switchMap, concatMap, last, map, catchError } from 'rxjs/operators';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ComplaintService } from '@core/services/complaint/complaint.service';
import { ComplaintResponse } from '../../../../core/models/complaint.model';
import { LoadingButtonComponent } from '../../../../shared/components/loading-button/loading-button.component';

export interface NewComplaintDialogData {
  leaseId: string;
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
    MatButtonModule,
    MatIconModule,
    LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7 min-w-[440px] max-w-[520px]">
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

        <div class="flex flex-col gap-1">
          <div class="opt-label">Type</div>
          <div class="opt-pills">
            @for (t of types; track t) {
              <button type="button" class="opt-pill" [class.opt-pill--active]="form.value.type === t"
                (click)="form.controls.type.setValue(t)">{{ t }}</button>
            }
          </div>
        </div>

        <div class="flex flex-col gap-1">
          <div class="opt-label">Priority</div>
          <div class="opt-pills">
            @for (p of priorities; track p) {
              <button type="button" class="opt-pill" [class.opt-pill--active]="form.value.priority === p"
                (click)="form.controls.priority.setValue(p)">{{ p }}</button>
            }
          </div>
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

        <div class="flex flex-col gap-1">
          <div class="opt-label">Photos (optional)</div>
          <input #fileInput type="file" accept="image/*" multiple class="!hidden" (change)="onFilesSelected($event)" />
          <div class="flex items-center gap-2 flex-wrap">
            @for (f of files(); track $index) {
              <span class="flex items-center gap-1 text-[12px] text-es-soft bg-es-subtle border border-solid border-es-border-light rounded-full pl-3 pr-1.5 py-1">
                <mat-icon class="!text-[13px] !w-[13px] !h-[13px]">image</mat-icon>
                <span class="max-w-[140px] truncate">{{ f.name }}</span>
                <button type="button"
                  class="flex items-center justify-center w-[16px] h-[16px] rounded-full border-none cursor-pointer bg-es-border-light"
                  (click)="removeFile($index)">
                  <mat-icon class="!text-[11px] !w-[11px] !h-[11px]">close</mat-icon>
                </button>
              </span>
            }
            <button type="button" mat-stroked-button class="!rounded-full !text-xs !h-8"
              (click)="fileInput.click()">
              <mat-icon class="!text-[15px]">add_a_photo</mat-icon>
              Add photos
            </button>
          </div>
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
  readonly files      = signal<File[]>([]);

  readonly types      = ['Maintenance', 'Billing', 'Noise', 'Safety', 'Cleanliness', 'Other'] as const;
  readonly priorities = ['Low', 'Medium', 'High', 'Critical'] as const;

  readonly form = this.fb.nonNullable.group({
    title:      ['', [Validators.required, Validators.maxLength(120)]],
    type:       ['Maintenance' as ComplaintResponse['complaintType'], Validators.required],
    priority:   ['Medium' as ComplaintResponse['priority'], Validators.required],
    description:['', [Validators.required, Validators.minLength(20)]],
  });

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const selected = Array.from(input.files ?? []);
    // Cap at 5 photos per complaint
    this.files.update(list => [...list, ...selected].slice(0, 5));
    input.value = '';
  }

  removeFile(index: number): void {
    this.files.update(list => list.filter((_, i) => i !== index));
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.error.set(null);
    const v = this.form.getRawValue();
    this.svc.createComplaint({
      leaseId:       this.data.leaseId,
      title:         v.title,
      description:   v.description,
      complaintType: v.type,
      priority:      v.priority,
    }).pipe(
      switchMap(res => {
        const created = res.data;
        const files = this.files();
        if (files.length === 0) return of(created);
        // Endpoint takes one file per call — upload sequentially, keep the last
        // response (it carries the full images list). If an upload fails, fall
        // back to the created complaint rather than blocking submission.
        return from(files).pipe(
          concatMap(f => this.svc.uploadComplaintImage(created.complaintId, f)),
          last(),
          map(res => res.data),
          catchError(() => of(created)),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: complaint => { this.submitting.set(false); this.dialogRef.close(complaint); },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to submit complaint.');
      }
    });
  }
}
