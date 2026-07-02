import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ComplaintService } from '../../../../core/services/complaint.service';
import { ComplaintResponse } from '../../../../core/models/complaint.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { formatDate } from '../../../../core/utils/format.utils';

type ComplaintFilter = 'All' | 'Open' | 'InProgress' | 'Resolved' | 'Closed';

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
  private readonly svc        = inject(ComplaintService);
  private readonly destroyRef = inject(DestroyRef);

  readonly complaints   = signal<ComplaintResponse[]>([]);
  readonly loading      = signal(true);
  readonly activeFilter = signal<ComplaintFilter>('All');
  readonly updatingId   = signal<string | null>(null);

  readonly filters: ComplaintFilter[] = ['All', 'Open', 'InProgress', 'Resolved', 'Closed'];

  readonly filtered = computed(() => {
    const f = this.activeFilter();
    if (f === 'All') return this.complaints();
    return this.complaints().filter(c => c.status === f);
  });

  readonly formatDate     = formatDate;
  readonly priorityStyle  = PRIORITY_STYLE;

  ngOnInit(): void {
    this.svc.getOwnerComplaints().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.complaints.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  nextStatus(current: ComplaintResponse['status']): string | null {
    if (current === 'Open')       return 'InProgress';
    if (current === 'InProgress') return 'Resolved';
    return null;
  }

  nextLabel(current: ComplaintResponse['status']): string {
    if (current === 'Open')       return 'Mark In Progress';
    if (current === 'InProgress') return 'Mark Resolved';
    return '';
  }

  updateStatus(complaint: ComplaintResponse): void {
    const next = this.nextStatus(complaint.status);
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
}
