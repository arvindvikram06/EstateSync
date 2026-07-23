import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { ComplaintService } from '@core/services/complaint/complaint.service';
import { DataRefreshService } from '@core/services/shared/data-refresh.service';
import { ComplaintDetailDialogComponent } from '../../../../shared/components/complaint-detail-dialog/complaint-detail-dialog.component';
import { ComplaintResponse } from '../../../../core/models/complaint.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { formatDate } from '../../../../core/utils/format.utils';

type ComplaintFilter = 'NeedsAction' | 'Active' | 'Waiting' | 'History';


@Component({
  selector: 'app-owner-complaints-tab',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './owner-complaints-tab.component.html',
  styleUrl: './owner-complaints-tab.component.css',
})
export class OwnerComplaintsTabComponent implements OnInit {
  private readonly svc = inject(ComplaintService);
  private readonly dataRefresh = inject(DataRefreshService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly complaints   = signal<ComplaintResponse[]>([]);
  readonly loading      = signal(true);
  readonly activeFilter = signal<ComplaintFilter>('NeedsAction');

  readonly filters: ComplaintFilter[] = ['NeedsAction', 'Active', 'Waiting', 'History'];

  // Precomputed per-complaint next-transition data — memoized instead of
  // method calls re-running in the @for loop on every change detection.
  readonly filtered = computed(() => {
    const f = this.activeFilter();
    return this.complaints().filter(c => {
      if (f === 'NeedsAction') return c.status === 'Open' || c.status === 'Reopened';
      if (f === 'Active') return c.status === 'InProgress';
      if (f === 'Waiting') return c.status === 'ResolutionProposed';
      return c.status === 'Resolved' || c.status === 'Closed';
    });
  });

  readonly counts = computed(() => ({
    NeedsAction: this.complaints().filter(c => c.status === 'Open' || c.status === 'Reopened').length,
    Active: this.complaints().filter(c => c.status === 'InProgress').length,
    Waiting: this.complaints().filter(c => c.status === 'ResolutionProposed').length,
    History: this.complaints().filter(c => c.status === 'Resolved' || c.status === 'Closed').length
  }));

  readonly formatDate = formatDate;

  statusLabel(status: ComplaintResponse['status']): string {
    return status === 'InProgress' ? 'In Progress' :
      status === 'ResolutionProposed' ? 'Waiting for Tenant' : status;
  }

  ngOnInit(): void {
    this.load();
    this.dataRefresh.on('complaints').pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.load(true);
    });
  }

  private load(silent = false): void {
    if (!silent) this.loading.set(true);
    this.svc.getOwnerComplaints().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.complaints.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openDetail(c: ComplaintResponse): void {
    this.svc.markOwnerViewed(c.complaintId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
    const ref = this.dialog.open(ComplaintDetailDialogComponent, {
      data: { complaint: c, viewer: 'owner' },
      maxWidth: '860px',
      width: '100%',
      panelClass: 'rounded-dialog',
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((updated?: ComplaintResponse) => {
      if (updated) this.complaints.update(list => list.map(item => item.complaintId === updated.complaintId ? updated : item));
    });
  }
}
