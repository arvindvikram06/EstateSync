import {
  Component, ChangeDetectionStrategy, inject, input, output, signal, OnInit, DestroyRef
} from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../../core/services/auth.service';
import { RentalApplicationService } from '../../../../core/services/rental-application.service';
import { LeaseService } from '../../../../core/services/lease.service';
import { PaymentService } from '../../../../core/services/payment.service';
import { ComplaintService } from '../../../../core/services/complaint.service';
import { KycVerificationResponse } from '../../../../core/models/kyc.model';
import { FinancialStore } from '../../../../core/stores/financial.store';

@Component({
  selector: 'app-overview-tab',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './overview-tab.component.html',
})
export class OverviewTabComponent implements OnInit {
  readonly latestKyc  = input<KycVerificationResponse | null>(null);
  readonly kycLoading = input<boolean>(true);
  readonly tabSwitch  = output<string>();

  private readonly router       = inject(Router);
  private readonly authService  = inject(AuthService);
  private readonly appSvc       = inject(RentalApplicationService);
  private readonly leaseSvc     = inject(LeaseService);
  private readonly billSvc      = inject(PaymentService);
  private readonly complaintSvc = inject(ComplaintService);
  private readonly destroyRef   = inject(DestroyRef);
  readonly finStore             = inject(FinancialStore);

  readonly user = this.authService.currentUser;
  readonly isOwner = this.authService.isOwner;

  readonly stats = signal({ applications: 0, activeLeases: 0, pendingBills: 0, openComplaints: 0 });
  readonly statsLoading = signal(true);

  get firstName(): string {
    return this.user()?.fullName?.split(' ')?.[0] ?? 'there';
  }

  get kycStatus(): string | null {
    return this.latestKyc()?.status ?? null;
  }

  ngOnInit(): void {
    forkJoin({
      apps:      this.appSvc.getMyApplications(),
      leases:    this.leaseSvc.getMyLeases(),
      bills:     this.billSvc.getTenantBills(),
      complaints:this.complaintSvc.getMyComplaints(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ apps, leases, bills, complaints }) => {
        this.stats.set({
          applications:  (apps.data ?? []).length,
          activeLeases:  (leases.data ?? []).filter(l => l.status === 'Active').length,
          pendingBills:  (bills.data ?? []).filter(b => b.status === 'Pending').length,
          openComplaints:(complaints.data ?? []).filter(c => c.status === 'Open').length,
        });
        this.statsLoading.set(false);
      },
      error: () => this.statsLoading.set(false),
    });
  }

  goToKyc(): void { this.router.navigate(['/kyc']); }
}
