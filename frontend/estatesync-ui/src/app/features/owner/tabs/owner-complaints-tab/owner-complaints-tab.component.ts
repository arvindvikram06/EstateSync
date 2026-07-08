import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { ComplaintService } from '@core/services/complaint/complaint.service';
import { ComplaintDetailDialogComponent } from '../../../../shared/components/complaint-detail-dialog/complaint-detail-dialog.component';
import { ComplaintResponse } from '../../../../core/models/complaint.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { formatDate } from '../../../../core/utils/format.utils';

type ComplaintFilter = 'All' | 'Open' | 'InProgress' | 'Resolved' | 'Closed';

function nextStatus(current: ComplaintResponse['status']): string | null {
  if (current === 'Open')       return 'InProgress';
  if (current === 'InProgress') return 'Resolved';
  if (current === 'Resolved')   return 'Closed';
  return null;
}

function nextLabel(current: ComplaintResponse['status']): string {
  if (current === 'Open')       return 'Mark In Progress';
  if (current === 'InProgress') return 'Mark Resolved';
  if (current === 'Resolved')   return 'Close complaint';
  return '';
}

const PRIORITY_STYLE: Record<ComplaintResponse['priority'], { bg: string; color: string }> = {
  Critical: { bg: '#FEE2E2', color: '#991B1B' },
  High:     { bg: '#FFEDD5', color: '#9A3412' },
  Medium:   { bg: '#FEF3C7', color: '#92400E' },
  Low:      { bg: '#F3F4F6', color: '#6B7280' },
};

@Component({
  selector: 'app-owner-complaints-tab',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './owner-complaints-tab.component.html',
  styleUrl: './owner-complaints-tab.component.css',
})
export class OwnerComplaintsTabComponent implements OnInit {
  readonly svc = inject(ComplaintService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly complaints   = signal<ComplaintResponse[]>([]);
  readonly loading      = signal(true);
  readonly activeFilter = signal<ComplaintFilter>('All');
  readonly updatingId   = signal<string | null>(null);

  readonly filters: ComplaintFilter[] = ['All', 'Open', 'InProgress', 'Resolved', 'Closed'];

  // Precomputed per-complaint next-transition data — memoized instead of
  // method calls re-running in the @for loop on every change detection.
  readonly filtered = computed(() => {
    const f = this.activeFilter();
    const list = f === 'All' ? this.complaints() : this.complaints().filter(c => c.status === f);
    return list.map(c => ({
      ...c,
      nextStatus: nextStatus(c.status),
      nextLabel: nextLabel(c.status),
    }));
  });

  readonly formatDate     = formatDate;
  readonly priorityStyle  = PRIORITY_STYLE;

  ngOnInit(): void {
    this.svc.getComplaints().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.complaints.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  updateStatus(complaint: ComplaintResponse): void {
    const next = nextStatus(complaint.status);
    if (!next) return;
    this.updatingId.set(complaint.complaintId);
    this.svc.updateComplaintStatus(complaint.complaintId, { status: next })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.updatingId.set(null);
          this.complaints.update(list =>
            list.map(c => c.complaintId === complaint.complaintId ? res.data : c)
          );
        },
        error: () => this.updatingId.set(null),
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
