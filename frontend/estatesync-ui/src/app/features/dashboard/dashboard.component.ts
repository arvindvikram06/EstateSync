import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, effect, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { KycStore } from '../../core/stores/kyc.store';
import { FinancialStore } from '../../core/stores/financial.store';
import { PaymentService } from '../../core/services/payment.service';
import { DashboardLayoutComponent, NavItem } from '../../shared/components/dashboard-layout/dashboard-layout.component';
import { OverviewTabComponent } from './tabs/overview-tab/overview-tab.component';
import { ApplicationsTabComponent } from './tabs/applications-tab/applications-tab.component';
import { LeasesTabComponent } from './tabs/leases-tab/leases-tab.component';
import { BillsTabComponent } from './tabs/bills-tab/bills-tab.component';
import { ComplaintsTabComponent } from './tabs/complaints-tab/complaints-tab.component';
import { VerificationsTabComponent } from './tabs/verifications-tab/verifications-tab.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    DashboardLayoutComponent,
    OverviewTabComponent,
    ApplicationsTabComponent,
    LeasesTabComponent,
    BillsTabComponent,
    ComplaintsTabComponent,
    VerificationsTabComponent,
    RouterModule,
    MatButtonModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  readonly kycStore              = inject(KycStore);
  readonly finStore              = inject(FinancialStore);
  private readonly paymentService = inject(PaymentService);
  private readonly destroyRef    = inject(DestroyRef);
  private readonly route         = inject(ActivatedRoute);
  private readonly router        = inject(Router);

  constructor() {
    effect(() => {
      if (this.kycStore.kyc()?.status === 'Approved' && this.finStore.step() === 'loading') {
        this.finStore.loadExisting();
      }
    });
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
      const tab = params.get('tab');
      if (tab) this.activeTab.set(tab);
    });
  }

  readonly activeTab = signal('overview');

  readonly latestKyc = this.kycStore.kyc;

  private readonly pendingBillCount = signal(0);

  readonly navItems = computed<NavItem[]>(() => {
    const pendingBills = this.pendingBillCount();
    // Owner Dashboard is reachable from the navbar link; no sidebar duplicate here.
    return [
      { id: 'overview',     label: 'Overview',     icon: 'dashboard' },
      { id: 'applications', label: 'Applications', icon: 'mail' },
      { id: 'leases',       label: 'Leases',       icon: 'article' },
      { id: 'bills',        label: 'Bills',        icon: 'receipt_long', badge: pendingBills || undefined },
      { id: 'complaints',   label: 'Complaints',   icon: 'report_problem' },
      { id: 'verifications',label: 'Verifications',icon: 'verified_user' },
    ];
  });

  readonly kycStatus    = computed(() => this.kycStore.kyc()?.status ?? null);
  readonly kycIsLoading = computed(() => this.kycStore.step() === 'loading');

  ngOnInit(): void {
    if (this.kycStore.step() === 'loading') {
      this.kycStore.loadExisting();
    }
    this.paymentService.getTenantBills()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          const pending = (res.data ?? []).filter(b => b.status === 'Pending').length;
          this.pendingBillCount.set(pending);
        },
      });
  }

  setTab(id: string): void {
    const item = this.navItems().find(i => i.id === id);
    if (item?.route) {
      this.router.navigate([item.route]);
      return;
    }
    this.activeTab.set(id);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: id },
      queryParamsHandling: 'merge'
    });
  }

  updateBillsBadge(count: number): void {
    this.pendingBillCount.set(count);
  }

}
