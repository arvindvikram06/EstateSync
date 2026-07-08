import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { LeaseService } from '@core/services/leasing/lease.service';
import { OwnerLeaseSummaryResponse } from '../../../../core/models/lease.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { VoidLeaseDialogComponent, VoidLeaseDialogData } from './void-lease-dialog.component';
import { ApproveRenewalDialogComponent, ApproveRenewalDialogData } from './approve-renewal-dialog.component';
import { TerminateLeaseDialogComponent, TerminateLeaseDialogData } from './terminate-lease-dialog.component';
import { formatDate, formatCurrency } from '../../../../core/utils/format.utils';

@Component({
  selector: 'app-owner-leases-tab',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './owner-leases-tab.component.html',
  styleUrl: './owner-leases-tab.component.css',
})
export class OwnerLeasesTabComponent implements OnInit {
  private readonly leaseSvc   = inject(LeaseService);
  private readonly dialog     = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly leases  = signal<OwnerLeaseSummaryResponse[]>([]);
  readonly loading = signal(true);

  // Precomputed per-lease action flags — memoized, unlike method calls in
  // the @for loop which re-run on every change detection cycle.
  readonly leaseRows = computed(() => this.leases().map(l => ({
    ...l,
    canOwnerSign: l.status === 'PendingApproval' || (l.status === 'PartialSigned' && !l.ownerSignedAt),
    canVoid: l.status === 'PendingApproval' || l.status === 'PartialSigned',
    hasRenewalRequest: l.status === 'Active' && l.isRenewalRequested,
  })));

  readonly renewalActionId = signal<string | null>(null);

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  ngOnInit(): void {
    this.loadLeases();
  }

  private loadLeases(): void {
    this.leaseSvc.getOwnerLeases().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.leases.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openApproveRenewalDialog(l: OwnerLeaseSummaryResponse): void {
    const data: ApproveRenewalDialogData = {
      leaseId: l.leaseId,
      propertyTitle: l.propertyTitle ?? 'Lease',
      startDate: l.startDate,
      endDate: l.endDate,
    };
    const ref = this.dialog.open(ApproveRenewalDialogComponent, {
      data,
      maxWidth: '460px',
      width: '100%',
      panelClass: 'rounded-dialog',
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(approved => {
      // End date changed server-side — re-fetch so the card shows the new term
      if (approved) this.loadLeases();
    });
  }

  rejectRenewal(l: OwnerLeaseSummaryResponse): void {
    this.renewalActionId.set(l.leaseId);
    this.leaseSvc.rejectRenewal(l.leaseId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.leases.update(list =>
          list.map(lease => lease.leaseId === l.leaseId ? { ...lease, isRenewalRequested: false } : lease)
        );
        this.renewalActionId.set(null);
      },
      error: () => this.renewalActionId.set(null),
    });
  }

  openVoidDialog(l: OwnerLeaseSummaryResponse): void {
    const data: VoidLeaseDialogData = { leaseId: l.leaseId, propertyTitle: l.propertyTitle ?? 'Lease' };
    const ref = this.dialog.open(VoidLeaseDialogComponent, {
      data,
      maxWidth: '460px',
      width: '100%',
      panelClass: 'rounded-dialog',
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(voided => {
      if (voided) {
        this.leases.update(list =>
          list.map(lease => lease.leaseId === l.leaseId ? { ...lease, status: 'Voided' as const } : lease)
        );
      }
    });
  }

  openTerminateDialog(l: OwnerLeaseSummaryResponse): void {
    const data: TerminateLeaseDialogData = { leaseId: l.leaseId, propertyTitle: l.propertyTitle ?? 'Lease' };
    const ref = this.dialog.open(TerminateLeaseDialogComponent, {
      data,
      maxWidth: '460px',
      width: '100%',
      panelClass: 'rounded-dialog',
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(terminated => {
      if (terminated) {
        this.leases.update(list =>
          list.map(lease => lease.leaseId === l.leaseId ? { ...lease, status: 'Terminated' as const } : lease)
        );
      }
    });
  }
}
