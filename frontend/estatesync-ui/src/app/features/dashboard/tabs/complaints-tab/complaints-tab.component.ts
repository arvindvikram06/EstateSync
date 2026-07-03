import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { ComplaintService } from '../../../../core/services/complaint.service';
import { LeaseService } from '../../../../core/services/lease.service';
import { ComplaintResponse } from '../../../../core/models/complaint.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { formatDate } from '../../../../core/utils/format.utils';
import { NewComplaintDialogComponent } from './new-complaint-dialog.component';

@Component({
  selector: 'app-complaints-tab',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent, SkeletonLoaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './complaints-tab.component.html',
  styleUrl: './complaints-tab.component.css',
})
export class ComplaintsTabComponent implements OnInit {
  private readonly svc        = inject(ComplaintService);
  private readonly leaseSvc   = inject(LeaseService);
  private readonly dialog     = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly complaints    = signal<ComplaintResponse[]>([]);
  readonly loading       = signal(true);
  readonly activePropertyId   = signal<string | null>(null);
  readonly activePropertyTitle= signal<string | null>(null);

  readonly formatDate = formatDate;

  ngOnInit(): void {
    this.svc.getMyComplaints().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.complaints.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });

    this.leaseSvc.getMyLeases().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        const active = (res.data ?? []).find(l => l.status === 'Active');
        if (active) {
          this.activePropertyId.set(active.propertyId);
          this.activePropertyTitle.set(active.propertyTitle);
        }
      },
    });
  }

  raiseComplaint(): void {
    const pid   = this.activePropertyId();
    const title = this.activePropertyTitle();
    if (!pid || !title) return;
    const ref = this.dialog.open(NewComplaintDialogComponent, {
      data: { propertyId: pid, propertyTitle: title },
      maxWidth: 540,
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((result: ComplaintResponse | undefined) => {
      if (result) this.complaints.update(list => [result, ...list]);
    });
  }
}
