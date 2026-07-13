import { Component, ChangeDetectionStrategy, inject, computed, signal, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '@core/services/auth/auth.service';
import { UserService } from '@core/services/admin/user.service';
import { UserProfileResponse } from '../../core/models/auth.model';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { LoadingButtonComponent } from '../../shared/components/loading-button/loading-button.component';
import { formatDate, getInitials } from '../../core/utils/format.utils';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule,
    MatProgressSpinnerModule,
    StatusBadgeComponent, LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile.component.html',
})
export class ProfileComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly snackBar = inject(MatSnackBar);

  readonly profile = signal<UserProfileResponse | null>(null);
  readonly roles = computed(() => this.authService.currentUser()?.roles ?? []);
  readonly initials = computed(() => getInitials(this.profile()?.fullName ?? ''));

  readonly isEditing = signal(false);
  readonly isSavingProfile = signal(false);
  readonly isChangingPassword = signal(false);
  readonly passwordError = signal<string | null>(null);
  readonly currentPasswordVisible = signal(false);
  readonly newPasswordVisible = signal(false);

  readonly profileForm = new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(3), Validators.maxLength(250)]
    }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50)]
    }),
  });

  readonly passwordForm = new FormGroup({
    currentPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    newPassword: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).*$/),
      ]
    }),
  });

  readonly formatDate = formatDate;

  ngOnInit(): void {
    this.userService.getProfile().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { if (res.success && res.data) this.profile.set(res.data); },
    });
  }

  startEdit(): void {
    const p = this.profile();
    if (!p) return;
    this.profileForm.setValue({ fullName: p.fullName, phoneNumber: p.phoneNumber });
    this.isEditing.set(true);
  }

  cancelEdit(): void {
    this.isEditing.set(false);
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    const { fullName, phoneNumber } = this.profileForm.getRawValue();
    this.isSavingProfile.set(true);
    this.userService.updateProfile(fullName, phoneNumber)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.isSavingProfile.set(false);
          if (res.success && res.data) {
            this.profile.set(res.data);
            this.isEditing.set(false);
            this.snackBar.open('Profile updated.', 'OK', { duration: 3000 });
          }
        },
        error: () => this.isSavingProfile.set(false),
      });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    const { currentPassword, newPassword } = this.passwordForm.getRawValue();
    this.isChangingPassword.set(true);
    this.passwordError.set(null);
    this.userService.changePassword(currentPassword, newPassword)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isChangingPassword.set(false);
          this.passwordForm.reset();
          this.snackBar.open('Password changed successfully.', 'OK', { duration: 3000 });
        },
        error: (err: HttpErrorResponse) => {
          this.isChangingPassword.set(false);
          this.passwordError.set(err.error?.detail ?? err.error?.message ?? 'Could not change password.');
        },
      });
  }
}
