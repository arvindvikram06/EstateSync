import { Component, ChangeDetectionStrategy, inject, signal, DestroyRef } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AlertBannerComponent } from '../../../shared/components/alert-banner/alert-banner.component';
import { LoadingButtonComponent } from '../../../shared/components/loading-button/loading-button.component';
import { AuthService } from '../../../core/services/auth.service';
import { TokenService } from '../../../core/services/token.service';
import { getRoleHome } from '../../../core/guards/role-home.util';

type StaffRole = 'Admin' | 'Verifier';

@Component({
  selector: 'app-staff-login',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule,
    AlertBannerComponent, LoadingButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './staff-login.component.html',
})
export class StaffLoginComponent {
  private readonly authService  = inject(AuthService);
  private readonly tokenService = inject(TokenService);
  private readonly router       = inject(Router);
  private readonly destroyRef   = inject(DestroyRef);

  readonly selectedRole    = signal<StaffRole>('Verifier');
  readonly isLoading       = signal(false);
  readonly errorMessage    = signal<string | null>(null);
  readonly passwordVisible = signal(false);

  readonly loginForm = new FormGroup({
    email:    new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] }),
  });

  get emailControl()    { return this.loginForm.controls.email; }
  get passwordControl() { return this.loginForm.controls.password; }

  selectRole(role: StaffRole): void {
    this.selectedRole.set(role);
    this.errorMessage.set(null);
  }

  togglePasswordVisibility(): void { this.passwordVisible.update(v => !v); }

  onEmailInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const lc = input.value.toLowerCase();
    if (input.value !== lc) {
      const s = input.selectionStart; const e = input.selectionEnd;
      this.emailControl.setValue(lc);
      input.setSelectionRange(s, e);
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) { this.loginForm.markAllAsTouched(); return; }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.loginForm.getRawValue()).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: () => {
        this.isLoading.set(false);
        const roles = this.tokenService.decodeRoles();
        const selected = this.selectedRole();

        if (selected === 'Admin' && !roles.includes('Admin')) {
          this.authService.logout();
          this.errorMessage.set('This account does not have Admin access.');
          return;
        }
        if (selected === 'Verifier' && !roles.includes('Verifier')) {
          this.authService.logout();
          this.errorMessage.set('This account does not have Verifier access.');
          return;
        }
        if (!roles.includes('Admin') && !roles.includes('Verifier')) {
          this.authService.logout();
          this.errorMessage.set('This portal is for EstateSync staff only. Please use the regular login.');
          return;
        }

        this.router.navigateByUrl(getRoleHome(roles));
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.detail ?? err.error?.message ?? 'Invalid email or password.');
      },
    });
  }
}
