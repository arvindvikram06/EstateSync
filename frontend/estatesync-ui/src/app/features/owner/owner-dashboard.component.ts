import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { DashboardLayoutComponent, NavItem } from '../../shared/components/dashboard-layout/dashboard-layout.component';
import { OwnerOverviewTabComponent } from './tabs/owner-overview-tab/owner-overview-tab.component';
import { OwnerPropertiesTabComponent } from './tabs/owner-properties-tab/owner-properties-tab.component';
import { OwnerApplicationsTabComponent } from './tabs/owner-applications-tab/owner-applications-tab.component';
import { OwnerLeasesTabComponent } from './tabs/owner-leases-tab/owner-leases-tab.component';
import { OwnerComplaintsTabComponent } from './tabs/owner-complaints-tab/owner-complaints-tab.component';
import { OwnerEarningsTabComponent } from './tabs/owner-earnings-tab/owner-earnings-tab.component';

@Component({
  selector: 'app-owner-dashboard',
  standalone: true,
  imports: [
    DashboardLayoutComponent,
    OwnerOverviewTabComponent,
    OwnerPropertiesTabComponent,
    OwnerApplicationsTabComponent,
    OwnerLeasesTabComponent,
    OwnerComplaintsTabComponent,
    OwnerEarningsTabComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './owner-dashboard.component.html',
})
export class OwnerDashboardComponent {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly activeTab = signal('overview');

  readonly navItems = signal<NavItem[]>([
    { id: 'overview',     label: 'Overview',      icon: 'dashboard' },
    { id: 'properties',   label: 'My Properties', icon: 'home_work' },
    { id: 'applications', label: 'Applications',  icon: 'mail' },
    { id: 'leases',       label: 'Leases',        icon: 'article' },
    { id: 'complaints',   label: 'Complaints',    icon: 'report_problem' },
    { id: 'earnings',     label: 'Earnings',      icon: 'payments' },
  ]);

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed()).subscribe(params => {
      const tab = params.get('tab');
      if (tab) this.activeTab.set(tab);
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

  updateApplicationsBadge(count: number): void {
    this.navItems.update(items =>
      items.map(i => i.id === 'applications' ? { ...i, badge: count || undefined } : i)
    );
  }
}
