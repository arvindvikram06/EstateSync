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

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatCardModule,
    AlertBannerComponent, LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly passwordVisible = signal(false);

  readonly loginForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8)]
    })
  });

  get emailControl() { return this.loginForm.controls.email; }
  get passwordControl() { return this.loginForm.controls.password; }

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
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.loginForm.getRawValue()).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        const detail = err.error?.detail ?? err.error?.message ?? 'Invalid email or password.';
        this.errorMessage.set(detail);
      }
    });
  }
}
