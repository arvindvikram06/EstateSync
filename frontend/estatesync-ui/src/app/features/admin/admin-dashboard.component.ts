import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { DashboardLayoutComponent, NavItem } from '../../shared/components/dashboard-layout/dashboard-layout.component';
import { AuthService } from '@core/services/auth/auth.service';
import { AdminService } from '@core/services/admin/admin.service';
import { AdminDashboardStats } from '../../core/models/admin.model';
import { AdminOverviewTabComponent } from './tabs/admin-overview-tab/admin-overview-tab.component';
import { AdminKycTabComponent } from './tabs/admin-kyc-tab/admin-kyc-tab.component';
import { AdminFinancialProfilesTabComponent } from './tabs/admin-financial-profiles-tab/admin-financial-profiles-tab.component';
import { AdminPropertiesTabComponent } from './tabs/admin-properties-tab/admin-properties-tab.component';
import { AdminLeasesTabComponent } from './tabs/admin-leases-tab/admin-leases-tab.component';
import { AdminVerifiersTabComponent } from './tabs/admin-verifiers-tab/admin-verifiers-tab.component';
import { AdminFinancialTabComponent } from './tabs/admin-financial-tab/admin-financial-tab.component';
import { AdminComplaintsTabComponent } from './tabs/admin-complaints-tab/admin-complaints-tab.component';
import { AdminAuditLogsTabComponent } from './tabs/admin-audit-logs-tab/admin-audit-logs-tab.component';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    DashboardLayoutComponent,
    AdminOverviewTabComponent,
    AdminKycTabComponent,
    AdminFinancialProfilesTabComponent,
    AdminPropertiesTabComponent,
    AdminLeasesTabComponent,
    AdminVerifiersTabComponent,
    AdminFinancialTabComponent,
    AdminComplaintsTabComponent,
    AdminAuditLogsTabComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-dashboard.component.html',
})
export class AdminDashboardComponent implements OnInit {
  private readonly route        = inject(ActivatedRoute);
  private readonly router       = inject(Router);
  private readonly authService  = inject(AuthService);
  private readonly adminService = inject(AdminService);
  private readonly destroyRef   = inject(DestroyRef);

  readonly activeTab = signal('overview');

  // Fetched once here and shared with the overview tab, so tab switches
  // don't refire the stats endpoint.
  readonly stats        = signal<AdminDashboardStats | null>(null);
  readonly statsLoading = signal(true);

  private readonly pendingLeaseCount = signal(0);

  readonly navItems = computed<NavItem[]>(() => [
    { id: 'overview',           label: 'Overview',             icon: 'dashboard' },
    { id: 'kyc',                label: 'KYC Verifications',    icon: 'verified_user' },
    { id: 'financial-profiles', label: 'Financial Profiles',   icon: 'account_balance_wallet' },
    { id: 'properties',         label: 'Property Approvals',   icon: 'home_work' },
    { id: 'leases',             label: 'Lease Approvals',      icon: 'article', badge: this.pendingLeaseCount() || undefined },
    { id: 'verifiers',          label: 'Verifiers',            icon: 'manage_accounts' },
    { id: 'financial',          label: 'Financial Dashboard',  icon: 'payments' },
    { id: 'complaints',         label: 'Complaints',           icon: 'support_agent' },
    { id: 'audit-logs',         label: 'Audit Logs',           icon: 'history' },
  ]);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
      const tab = params.get('tab');
      if (tab) this.activeTab.set(tab);
    });
  }

  ngOnInit(): void {
    if (!this.authService.isAdmin()) {
      this.router.navigate(['/']);
      return;
    }
    this.adminService.getDashboardStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.stats.set(res.data);
          this.statsLoading.set(false);
          if (res.data?.pendingLeaseApprovalCount) {
            this.pendingLeaseCount.set(res.data.pendingLeaseApprovalCount);
          }
        },
        error: () => this.statsLoading.set(false),
      });
  }

  setTab(id: string): void {
    this.activeTab.set(id);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: id },
      queryParamsHandling: 'merge',
    });
  }

  updateLeaseBadge(count: number): void { this.pendingLeaseCount.set(count); }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
