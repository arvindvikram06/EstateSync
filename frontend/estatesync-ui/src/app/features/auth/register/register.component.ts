import { Component, ChangeDetectionStrategy, inject, signal, DestroyRef } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { AlertBannerComponent } from '../../../shared/components/alert-banner/alert-banner.component';
import { LoadingButtonComponent } from '../../../shared/components/loading-button/loading-button.component';
import { AuthService } from '../../../core/services/auth.service';
import { passwordMatchValidator } from '../../../shared/validators/password-match.validator';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatCardModule,
    AlertBannerComponent, LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly passwordVisible = signal(false);

  readonly registerForm = new FormGroup({
    fullName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(2)]
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^\+?[\d\s\-]{10,}$/)]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).+$/)
      ]
    }),
    confirmPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  }, { validators: passwordMatchValidator });


// getters for form controls
  get fullNameControl()      { return this.registerForm.controls.fullName; }
  get emailControl()         { return this.registerForm.controls.email; }
  get phoneControl()         { return this.registerForm.controls.phoneNumber; }
  get passwordControl()      { return this.registerForm.controls.password; }
  get confirmControl()       { return this.registerForm.controls.confirmPassword; }
  get passwordsDoNotMatch()  {
    return this.registerForm.hasError('passwordMismatch') && this.confirmControl.touched;
  }

  get passwordStrength(): number {
    const p = this.passwordControl.value;
    if (!p) return 0;
    let score = 0;
    if (p.length >= 8)         score++;
    if (/[A-Z]/.test(p))       score++;
    if (/\d/.test(p))          score++;
    if (/[!@#$%^&*]/.test(p))  score++;
    return score;
  }

  get strengthLabel(): string {
    return ['', 'Weak', 'Fair', 'Good', 'Strong'][this.passwordStrength];
  }

  togglePasswordVisibility(): void {
    this.passwordVisible.update(v => !v);
  }

  onEmailInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const lowercased = input.value.toLowerCase();
    if (input.value !== lowercased) {
      const start = input.selectionStart;
      const end = input.selectionEnd;
      this.emailControl.setValue(lowercased);
      input.setSelectionRange(start, end);
    }
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { fullName, email, phoneNumber, password } = this.registerForm.getRawValue();

    this.authService.register({ fullName, email, phoneNumber, password }).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        const detail = err.error?.detail ?? err.error?.message ?? 'Registration failed. Please try again.';
        this.errorMessage.set(detail);
      }
    });
  }
}
