import {
  Component, ChangeDetectionStrategy, inject, signal, computed, output, OnInit, DestroyRef
} from '@angular/core';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../../core/services/auth.service';
import { PropertyService } from '../../../../core/services/property.service';
import { RentalApplicationService } from '../../../../core/services/rental-application.service';
import { LeaseService } from '../../../../core/services/lease.service';
import { ComplaintService } from '../../../../core/services/complaint.service';
import { PaymentService } from '../../../../core/services/payment.service';
import { PropertySummaryResponse } from '../../../../core/models/property.model';
import { OwnerApplicationResponse } from '../../../../core/models/rental-application.model';
import { OwnerLeaseSummaryResponse } from '../../../../core/models/lease.model';
import { ComplaintResponse } from '../../../../core/models/complaint.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { formatDate } from '../../../../core/utils/format.utils';

@Component({
  selector: 'app-owner-overview-tab',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './owner-overview-tab.component.html',
  styleUrl: './owner-overview-tab.component.css',
})
export class OwnerOverviewTabComponent implements OnInit {
  readonly tabSwitch = output<string>();

  private readonly route             = inject(ActivatedRoute);
  private readonly authService       = inject(AuthService);
  private readonly propertySvc       = inject(PropertyService);
  private readonly applicationSvc    = inject(RentalApplicationService);
  private readonly leaseSvc          = inject(LeaseService);
  private readonly complaintSvc      = inject(ComplaintService);
  private readonly paymentSvc        = inject(PaymentService);
  private readonly destroyRef        = inject(DestroyRef);

  readonly user                = this.authService.currentUser;
  readonly isOnboarded         = computed(() => this.authService.currentUser()?.isOwnerVerified ?? false);
  readonly onboardLoading      = signal(false);
  readonly stripeCallbackState = signal<'none' | 'checking' | 'done'>('none');

  readonly properties   = signal<PropertySummaryResponse[]>([]);
  readonly apps         = signal<OwnerApplicationResponse[]>([]);
  readonly leases       = signal<OwnerLeaseSummaryResponse[]>([]);
  readonly complaints   = signal<ComplaintResponse[]>([]);
  readonly statsLoading = signal(true);

  readonly totalProperties  = computed(() => this.properties().length);
  readonly pendingApps      = computed(() => this.apps().filter(a => a.status === 'Pending').length);
  readonly activeLeases     = computed(() => this.leases().filter(l => l.status === 'Active').length);
  readonly openComplaints   = computed(() => this.complaints().filter(c => c.status === 'Open').length);
  readonly recentPendingApps = computed(() => this.apps().filter(a => a.status === 'Pending').slice(0, 3));

  readonly formatDate = formatDate;

  ngOnInit(): void {
    const cb = this.route.snapshot.queryParamMap.get('stripeCallback');
    if (cb === 'onboarding' || cb === 'refresh') {
      // Explicit return from Stripe — show spinner while syncing
      this.stripeCallbackState.set('checking');
      this.authService.syncProfile()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => this.stripeCallbackState.set('done'),
          error: () => this.stripeCallbackState.set('none'),
        });
    } else if (!this.isOnboarded()) {
      // Silent sync to catch stale isOwnerVerified in localStorage (uses JWT, no cookie needed)
      this.authService.syncProfile()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe();
    }

    forkJoin({
      props:      this.propertySvc.getMyProperties(),
      apps:       this.applicationSvc.getOwnerApplications(),
      leases:     this.leaseSvc.getOwnerLeases(),
      complaints: this.complaintSvc.getOwnerComplaints(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ props, apps, leases, complaints }) => {
        this.properties.set(props.data ?? []);
        this.apps.set(apps.data ?? []);
        this.leases.set(leases.data ?? []);
        this.complaints.set(complaints.data ?? []);
        this.statsLoading.set(false);
      },
      error: () => this.statsLoading.set(false),
    });
  }

  startOnboarding(): void {
    this.onboardLoading.set(true);
    this.paymentSvc.createOwnerOnboardingLink()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { window.location.href = res.data.url; },
        error: () => this.onboardLoading.set(false),
      });
  }
}
