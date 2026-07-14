import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { AdminService } from '@core/services/admin/admin.service';

@Component({
  selector: 'app-admin-create-verifier-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule, MatDialogModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatIconModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div mat-dialog-title class="text-[18px] font-bold text-es-text">Add Verifier Account</div>
    <div style="font-size:13px;color:#717171;margin-bottom:4px">Create a staff account with Verifier role.</div>

    <mat-dialog-content class="!pt-4">
      <form [formGroup]="form" class="flex flex-col gap-4">
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Full Name</mat-label>
          <mat-icon matPrefix class="mr-2">person</mat-icon>
          <input matInput formControlName="fullName" placeholder="Jane Smith" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Email</mat-label>
          <mat-icon matPrefix class="mr-2">email</mat-icon>
          <input matInput type="email" formControlName="email" placeholder="jane@example.com" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Phone Number</mat-label>
          <mat-icon matPrefix class="mr-2">phone</mat-icon>
          <input matInput formControlName="phoneNumber" placeholder="+91 98765 43210" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="w-full">
          <mat-label>Password</mat-label>
          <mat-icon matPrefix class="mr-2">lock</mat-icon>
          <input matInput type="password" formControlName="password" />
          <mat-hint>Minimum 8 characters</mat-hint>
        </mat-form-field>
        @if (error()) {
          <div class="es-text-xs es-text-danger">{{ error() }}</div>
        }
      </form>
    </mat-dialog-content>

    <mat-dialog-actions align="end" style="padding:16px 24px;gap:8px">
      <button mat-stroked-button class="!rounded-full" mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" class="!rounded-full !font-semibold"
        [disabled]="submitting() || form.invalid" (click)="submit()">
        @if (submitting()) { <mat-spinner diameter="16" class="inline-block !mr-2" /> }
        Create Account
      </button>
    </mat-dialog-actions>
  `,
})
export class AdminCreateVerifierDialogComponent {
  private readonly dialogRef    = inject(MatDialogRef<AdminCreateVerifierDialogComponent>);
  private readonly adminService = inject(AdminService);
  private readonly destroyRef   = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly error      = signal<string | null>(null);

  readonly form = new FormGroup({
    fullName:    new FormControl('', { nonNullable: true, validators: Validators.required }),
    email:       new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    phoneNumber: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^(\+91|0)?[6-9]\d{9}$/)] }),
    password:    new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] }),
  });

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.error.set(null);
    this.adminService.createVerifier(this.form.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: () => { this.submitting.set(false); this.dialogRef.close(true); },
      error: (e) => { this.submitting.set(false); this.error.set(e?.error?.message ?? 'Something went wrong.'); },
    });
  }
}
