import { Component, ChangeDetectionStrategy, inject, signal, OnInit, effect } from '@angular/core';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { KycStore } from '../../core/stores/kyc.store';
import { FinancialStore } from '../../core/stores/financial.store';
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
  readonly kycStore = inject(KycStore);
  readonly finStore = inject(FinancialStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  constructor() {
    effect(() => {
      if (this.kycStore.kyc()?.status === 'Approved' && this.finStore.step() === 'loading') {
        this.finStore.loadExisting();
      }
    });
  }

  readonly activeTab = signal('overview');

  // Convenience aliases surfaced from the store — no duplicate API call
  readonly latestKyc = this.kycStore.kyc;

  readonly navItems = signal<NavItem[]>([
    { id: 'overview',     label: 'Overview',     icon: 'dashboard' },
    { id: 'applications', label: 'Applications', icon: 'mail' },
    { id: 'leases',       label: 'Leases',       icon: 'article' },
    { id: 'bills',        label: 'Bills',        icon: 'receipt_long' },
    { id: 'complaints',   label: 'Complaints',   icon: 'report_problem' },
    { id: 'verifications',label: 'Verifications',icon: 'verified_user' },
  ]);

  get kycStatus()   { return this.kycStore.kyc()?.status ?? null; }
  get kycIsLoading(){ return this.kycStore.step() === 'loading'; }

  ngOnInit(): void {
    // Read the tab from the URL, if present
    const urlTab = this.route.snapshot.queryParamMap.get('tab');
    if (urlTab) {
      this.activeTab.set(urlTab);
    }

    // Load KYC state into the store (only if not already loaded)
    if (this.kycStore.step() === 'loading') {
      this.kycStore.loadExisting();
    }
  }

  setTab(id: string): void {
    this.activeTab.set(id);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: id },
      queryParamsHandling: 'merge'
    });
  }

  updateBillsBadge(count: number): void {
    this.navItems.update(items =>
      items.map(i => i.id === 'bills' ? { ...i, badge: count || undefined } : i)
    );
  }

  get sidebarSubtitle(): string { return ''; }
}
