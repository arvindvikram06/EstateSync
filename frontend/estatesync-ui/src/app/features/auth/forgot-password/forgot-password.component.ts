import { Component, ChangeDetectionStrategy, inject, signal, DestroyRef } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { LoadingButtonComponent } from '../../../shared/components/loading-button/loading-button.component';
import { AuthService } from '@core/services/auth/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule,
    LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen flex items-center justify-center bg-es-bg px-4">
      <div class="es-card px-8 py-10 max-w-[440px] w-full">

        @if (sent()) {
          <div class="text-center">
            <div class="w-16 h-16 rounded-full es-bg-green flex items-center justify-center mx-auto mb-5">
              <mat-icon class="!text-[32px] !w-[32px] !h-[32px] es-text-green-deep">mark_email_read</mat-icon>
            </div>
            <h1 class="es-title mb-1">Check your inbox</h1>
            <p class="es-text-sm mb-7">
              If <span class="font-semibold">{{ emailControl.value }}</span> is registered,
              a password reset link has been sent. The link is valid for 1 hour.
            </p>
            <a mat-stroked-button routerLink="/auth/login" class="!rounded-full !h-11 !px-8 w-full">
              Back to sign in
            </a>
          </div>
        } @else {
          <div class="flex items-center gap-1.5 mb-6">
            <mat-icon class="!text-brand !text-[22px]">home_work</mat-icon>
            <span class="text-[18px] font-bold text-brand tracking-tight">EstateSync</span>
          </div>

          <h1 class="text-[24px] font-bold tracking-tight text-es-text mb-1.5">Forgot your password?</h1>
          <p class="text-sm text-es-muted mb-7">
            Enter your email and we'll send you a link to reset it.
          </p>

          <form [formGroup]="form" (ngSubmit)="onSubmit()" novalidate class="flex flex-col gap-1">
            <mat-form-field appearance="outline" class="w-full">
              <mat-label>Email address</mat-label>
              <mat-icon matPrefix>email</mat-icon>
              <input matInput type="email" formControlName="email"
                placeholder="you@example.com" autocomplete="email" />
              @if (emailControl.hasError('required') && emailControl.touched) {
                <mat-error>Email is required</mat-error>
              } @else if (emailControl.hasError('email') && emailControl.touched) {
                <mat-error>Enter a valid email address</mat-error>
              }
            </mat-form-field>

            <app-loading-button type="submit" [loading]="isLoading()">Send reset link</app-loading-button>
          </form>

          <p class="text-sm text-es-muted mt-6 text-center">
            Remembered it?
            <a routerLink="/auth/login" class="text-brand font-semibold no-underline hover:underline">Sign in</a>
          </p>
        }

      </div>
    </div>
  `
})
export class ForgotPasswordComponent {
  private readonly authService = inject(AuthService);
  private readonly destroyRef  = inject(DestroyRef);

  readonly isLoading = signal(false);
  readonly sent = signal(false);

  readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    })
  });

  get emailControl() { return this.form.controls.email; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.authService.forgotPassword(this.form.getRawValue().email)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        // Backend always returns 200 — never reveals whether the email exists.
        next: () => { this.isLoading.set(false); this.sent.set(true); },
        error: () => { this.isLoading.set(false); this.sent.set(true); },
      });
  }
}
