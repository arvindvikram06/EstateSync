import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { LeaseService } from '@core/services/leasing/lease.service';
import { DataRefreshService } from '@core/services/shared/data-refresh.service';
import { LeaseSummaryResponse, LeaseVacateNoticeResponse } from '../../../../core/models/lease.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { formatDate, formatCurrency } from '../../../../core/utils/format.utils';
import { VacateNoticeDialogComponent, VacateNoticeDialogData } from './vacate-notice-dialog.component';

@Component({
  selector: 'app-leases-tab',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './leases-tab.component.html',
  styleUrl: './leases-tab.component.css',
})
export class LeasesTabComponent implements OnInit {
  private readonly svc          = inject(LeaseService);
  private readonly dataRefresh  = inject(DataRefreshService);
  private readonly dialog       = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly leases  = signal<LeaseSummaryResponse[]>([]);
  readonly loading = signal(true);
  readonly renewingId = signal<string | null>(null);
  readonly pendingNotices = signal<Record<string, LeaseVacateNoticeResponse>>({});

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  ngOnInit(): void {
    this.load();
    this.dataRefresh.on('leases').pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.load(true);
    });
  }

  private load(silent = false): void {
    if (!silent) this.loading.set(true);
    this.svc.getMyLeases().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.leases.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  canSign(lease: LeaseSummaryResponse): boolean {
    return (lease.status === 'PendingApproval' || lease.status === 'PartialSigned') && !lease.tenantSignedAt;
  }

  requestRenewal(lease: LeaseSummaryResponse): void {
    this.renewingId.set(lease.leaseId);
    this.svc.requestRenewal(lease.leaseId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.leases.update(list =>
          list.map(l => l.leaseId === lease.leaseId ? { ...l, isRenewalRequested: true } : l)
        );
        this.renewingId.set(null);
      },
      error: () => this.renewingId.set(null),
    });
  }

  openVacateNoticeDialog(lease: LeaseSummaryResponse & { noticePeriodDays?: number | null; penaltyPercentage?: number | null; securityDeposit?: number }): void {
    const data: VacateNoticeDialogData = {
      leaseId: lease.leaseId,
      propertyTitle: lease.propertyTitle ?? 'Lease',
      noticePeriodDays: lease.noticePeriodDays ?? null,
      penaltyPercentage: lease.penaltyPercentage ?? null,
      securityDeposit: lease.securityDeposit ?? 0,
    };
    this.dialog.open(VacateNoticeDialogComponent, {
      data,
      maxWidth: '480px',
      width: '100%',
      panelClass: 'rounded-dialog',
    }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((notice: LeaseVacateNoticeResponse | undefined) => {
      if (notice) {
        this.pendingNotices.update(m => ({ ...m, [lease.leaseId]: notice }));
      }
    });
  }

  getNotice(leaseId: string): LeaseVacateNoticeResponse | null {
    return this.pendingNotices()[leaseId] ?? null;
  }
}
