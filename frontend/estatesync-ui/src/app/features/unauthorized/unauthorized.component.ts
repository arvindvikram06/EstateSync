import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '@core/services/auth/auth.service';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-[calc(100vh-64px)] bg-[#F7F7F7] flex items-center justify-center p-6">
      <div class="text-center max-w-sm">
        <div class="w-20 h-20 rounded-full bg-[#FEE2E2] flex items-center justify-center mx-auto mb-6">
          <mat-icon style="font-size:40px;width:40px;height:40px;color:#DC2626">lock</mat-icon>
        </div>
        <h1 style="font-size:24px;font-weight:700;color:#222;margin-bottom:8px">Access Denied</h1>
        <p style="font-size:14px;color:#717171;margin-bottom:32px">
          You don't have permission to view this page. Please contact your administrator if you believe this is an error.
        </p>
        <div class="flex flex-col gap-3">
          <button mat-flat-button color="primary" class="!rounded-full !h-11 !font-semibold"
            (click)="goHome()">
            Go to Home
          </button>
          @if (!authService.isLoggedIn()) {
            <button mat-stroked-button class="!rounded-full !h-11"
              (click)="goLogin()">
              Sign In
            </button>
          }
        </div>
      </div>
    </div>
  `,
})
export class UnauthorizedComponent {
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  goHome(): void {
    if (this.authService.isAdmin()) {
      this.router.navigate(['/admin/dashboard']);
    } else if (this.authService.isOwner()) {
      this.router.navigate(['/owner/dashboard']);
    } else {
      this.router.navigate(['/']);
    }
  }

  goLogin(): void {
    this.router.navigate(['/auth/login']);
  }
}
