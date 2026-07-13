import { Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AlertBannerComponent } from '../../../shared/components/alert-banner/alert-banner.component';
import { LoadingButtonComponent } from '../../../shared/components/loading-button/loading-button.component';
import { AuthService } from '@core/services/auth/auth.service';

type ResetState = 'form' | 'success' | 'invalid-link';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { passwordMismatch: true } : null;
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule,
    AlertBannerComponent, LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen flex items-center justify-center bg-es-bg px-4">
      <div class="es-card px-8 py-10 max-w-[440px] w-full">

        @switch (state()) {
          @case ('success') {
            <div class="text-center">
              <div class="w-16 h-16 rounded-full es-bg-green flex items-center justify-center mx-auto mb-5">
                <mat-icon class="!text-[32px] !w-[32px] !h-[32px] es-text-green-deep">check_circle</mat-icon>
              </div>
              <h1 class="es-title mb-1">Password reset!</h1>
              <p class="es-text-sm mb-7">Your password has been changed — sign in with your new password.</p>
              <a mat-flat-button color="primary" routerLink="/auth/login"
                class="!rounded-full !h-12 !px-8 !font-semibold w-full">
                Sign in
              </a>
            </div>
          }
          @case ('invalid-link') {
            <div class="text-center">
              <div class="w-16 h-16 rounded-full bg-es-danger-bg flex items-center justify-center mx-auto mb-5">
                <mat-icon class="!text-[32px] !w-[32px] !h-[32px] es-text-danger">link_off</mat-icon>
              </div>
              <h1 class="es-title mb-1">Link invalid or expired</h1>
              <p class="es-text-sm mb-7">{{ errorMessage() }}</p>
              <a mat-flat-button color="primary" routerLink="/auth/forgot-password"
                class="!rounded-full !h-12 !px-8 !font-semibold w-full">
                Request a new link
              </a>
            </div>
          }
          @case ('form') {
            <div class="flex items-center gap-1.5 mb-6">
              <mat-icon class="!text-brand !text-[22px]">home_work</mat-icon>
              <span class="text-[18px] font-bold text-brand tracking-tight">EstateSync</span>
            </div>

            <h1 class="text-[24px] font-bold tracking-tight text-es-text mb-1.5">Choose a new password</h1>
            <p class="text-sm text-es-muted mb-7">
              Must be at least 8 characters with an uppercase letter, a number and a special character.
            </p>

            @if (errorMessage()) {
              <app-alert-banner [message]="errorMessage()!" />
            }

            <form [formGroup]="form" (ngSubmit)="onSubmit()" novalidate class="flex flex-col gap-1">
              <mat-form-field appearance="outline" class="w-full">
                <mat-label>New password</mat-label>
                <mat-icon matPrefix>lock</mat-icon>
                <input matInput [type]="passwordVisible() ? 'text' : 'password'"
                  formControlName="password" placeholder="••••••••" autocomplete="new-password" />
                <button mat-icon-button matSuffix type="button" (click)="togglePasswordVisibility()">
                  <mat-icon>{{ passwordVisible() ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
                @if (passwordControl.hasError('required') && passwordControl.touched) {
                  <mat-error>Password is required</mat-error>
                } @else if (passwordControl.hasError('minlength') && passwordControl.touched) {
                  <mat-error>Password must be at least 8 characters</mat-error>
                } @else if (passwordControl.hasError('pattern') && passwordControl.touched) {
                  <mat-error>Needs an uppercase letter, a number and a special character</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline" class="w-full">
                <mat-label>Confirm new password</mat-label>
                <mat-icon matPrefix>lock_reset</mat-icon>
                <input matInput type="password" formControlName="confirmPassword"
                  placeholder="••••••••" autocomplete="new-password" />
                @if (confirmControl.hasError('required') && confirmControl.touched) {
                  <mat-error>Please confirm your password</mat-error>
                }
              </mat-form-field>

              @if (form.hasError('passwordMismatch') && confirmControl.touched) {
                <p class="text-xs es-text-danger -mt-2 mb-2">Passwords do not match</p>
              }

              <app-loading-button type="submit" [loading]="isLoading()">Reset password</app-loading-button>
            </form>
          }
        }

      </div>
    </div>
  `
})
export class ResetPasswordComponent implements OnInit {
  private readonly route       = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly destroyRef  = inject(DestroyRef);

  readonly state = signal<ResetState>('form');
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly passwordVisible = signal(false);

  private token = '';

  // Mirrors backend ResetPasswordRequestValidator: 8+ chars, uppercase, number, special.
  readonly form = new FormGroup({
    password: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).*$/),
      ]
    }),
    confirmPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  }, { validators: passwordsMatch });

  get passwordControl() { return this.form.controls.password; }
  get confirmControl() { return this.form.controls.confirmPassword; }

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.errorMessage.set('This reset link is missing its token. Please use the link from your email.');
      this.state.set('invalid-link');
      return;
    }
    this.token = token;
  }

  togglePasswordVisibility(): void {
    this.passwordVisible.update(v => !v);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.resetPassword(this.token, this.form.getRawValue().password)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.state.set('success');
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          const detail = err.error?.detail ?? err.error?.message ?? 'Invalid or expired reset link. Please request a new one.';
          if (err.status === 400) {
            this.errorMessage.set(detail);
            this.state.set('invalid-link');
          } else {
            this.errorMessage.set(detail);
          }
        }
      });
  }
}
