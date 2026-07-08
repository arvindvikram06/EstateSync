import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { ComplaintService } from '@core/services/complaint/complaint.service';
import { LeaseService } from '@core/services/leasing/lease.service';
import { ComplaintResponse } from '../../../../core/models/complaint.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { formatDate } from '../../../../core/utils/format.utils';
import { NewComplaintDialogComponent } from './new-complaint-dialog.component';
import { ComplaintDetailDialogComponent } from '../../../../shared/components/complaint-detail-dialog/complaint-detail-dialog.component';

@Component({
  selector: 'app-complaints-tab',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent, SkeletonLoaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './complaints-tab.component.html',
  styleUrl: './complaints-tab.component.css',
})
export class ComplaintsTabComponent implements OnInit {
  readonly svc = inject(ComplaintService);
  private readonly leaseSvc   = inject(LeaseService);
  private readonly dialog     = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly complaints    = signal<ComplaintResponse[]>([]);
  readonly loading       = signal(true);
  readonly statusFilter  = signal<'All' | ComplaintResponse['status']>('All');

  readonly statusOptions = ['All', 'Open', 'InProgress', 'Resolved', 'Closed'] as const;

  readonly filtered = computed(() => {
    const f = this.statusFilter();
    const list = this.complaints();
    return f === 'All' ? list : list.filter(c => c.status === f);
  });
  readonly activeLeaseId      = signal<string | null>(null);
  readonly activePropertyId   = signal<string | null>(null);
  readonly activePropertyTitle= signal<string | null>(null);

  readonly formatDate = formatDate;

  ngOnInit(): void {
    this.svc.getComplaints().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.complaints.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });

    this.leaseSvc.getMyLeases().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        const active = (res.data ?? []).find(l => l.status === 'Active');
        if (active) {
          this.activeLeaseId.set(active.leaseId);
          this.activePropertyId.set(active.propertyId);
          this.activePropertyTitle.set(active.propertyTitle);
        }
      },
    });
  }

  raiseComplaint(): void {
    const lid   = this.activeLeaseId();
    const pid   = this.activePropertyId();
    const title = this.activePropertyTitle();
    if (!lid || !pid || !title) return;
    const ref = this.dialog.open(NewComplaintDialogComponent, {
      data: { leaseId: lid, propertyId: pid, propertyTitle: title },
      width: '100%',
      maxWidth: '540px',
      panelClass: 'rounded-dialog',
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result: ComplaintResponse | undefined) => {
      if (result) this.complaints.update(list => [result, ...list]);
    });
  }

  openImage(url: string, event: MouseEvent): void {
    event.stopPropagation();
    window.open(this.svc.fileUrl(url), '_blank');
  }

  openDetail(c: ComplaintResponse): void {
    this.dialog.open(ComplaintDetailDialogComponent, {
      data: { complaint: c },
      maxWidth: '560px',
      width: '100%',
      panelClass: 'rounded-dialog',
    });
  }
}
