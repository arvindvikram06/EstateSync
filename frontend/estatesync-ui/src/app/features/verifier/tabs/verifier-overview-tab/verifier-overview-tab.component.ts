import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef, output
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AdminService } from '@core/services/admin/admin.service';
import { AuthService } from '@core/services/auth/auth.service';

@Component({
  selector: 'app-verifier-overview-tab',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './verifier-overview-tab.component.html',
})
export class VerifierOverviewTabComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly authService  = inject(AuthService);
  private readonly destroyRef   = inject(DestroyRef);

  readonly tabSwitch = output<string>();

  readonly loading             = signal(true);
  readonly pendingKycCount     = signal(0);
  readonly pendingFinancialCount = signal(0);

  get userName(): string {
    return this.authService.currentUser()?.fullName ?? 'Staff';
  }

  ngOnInit(): void {
    forkJoin({
      kyc: this.adminService.getKycRequests('Pending', 1, 1),
      fin: this.adminService.getFinancialProfiles('Pending', 1, 1),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ kyc, fin }) => {
        this.pendingKycCount.set(kyc.total ?? 0);
        this.pendingFinancialCount.set(fin.total ?? 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
