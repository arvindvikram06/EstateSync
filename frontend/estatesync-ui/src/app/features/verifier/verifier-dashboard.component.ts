import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { DashboardLayoutComponent, NavItem } from '../../shared/components/dashboard-layout/dashboard-layout.component';
import { AuthService } from '@core/services/auth/auth.service';
import { VerifierOverviewTabComponent } from './tabs/verifier-overview-tab/verifier-overview-tab.component';
import { AdminKycTabComponent } from '../admin/tabs/admin-kyc-tab/admin-kyc-tab.component';
@Component({
  selector: 'app-verifier-dashboard',
  standalone: true,
  imports: [
    DashboardLayoutComponent,
    VerifierOverviewTabComponent,
    AdminKycTabComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './verifier-dashboard.component.html',
})
export class VerifierDashboardComponent implements OnInit {
  private readonly route       = inject(ActivatedRoute);
  private readonly router      = inject(Router);
  private readonly authService = inject(AuthService);

  readonly activeTab = signal('overview');

  readonly navItems: NavItem[] = [
    { id: 'overview', label: 'Overview',   icon: 'dashboard' },
    { id: 'kyc',      label: 'KYC Reviews', icon: 'verified_user' },
  ];

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
      const tab = params.get('tab');
      if (tab) this.activeTab.set(tab);
    });
  }

  ngOnInit(): void {}

  setTab(id: string): void {
    this.activeTab.set(id);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab: id },
      queryParamsHandling: 'merge',
    });
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
