import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, output, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { LeaseService } from '../../../../core/services/lease.service';
import { LeaseDetailResponse, LeaseStatusString } from '../../../../core/models/lease.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import {
  ApproveLeaseDialogComponent,
  ApproveLeaseDialogData,
} from './approve-lease-dialog.component';
import { formatDate, formatCurrency } from '../../../../core/utils/format.utils';

type FilterTab = 'all' | 'PendingAdminApproval' | 'Active' | 'Voided';

@Component({
  selector: 'app-admin-leases-tab',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-leases-tab.component.html',
  styleUrl: './admin-leases-tab.component.css',
})
export class AdminLeasesTabComponent implements OnInit {
  private readonly leaseSvc   = inject(LeaseService);
  private readonly dialog     = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly pendingCountChange = output<number>();

  readonly all     = signal<LeaseDetailResponse[]>([]);
  readonly loading = signal(true);
  readonly filter  = signal<FilterTab>('PendingAdminApproval');

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  readonly pendingCount = computed(() =>
    this.all().filter(l => l.status === 'PendingAdminApproval').length
  );

  readonly filtered = computed(() => {
    const f = this.filter();
    return f === 'all' ? this.all() : this.all().filter(l => l.status === f);
  });

  readonly filterTabs: { id: FilterTab; label: string }[] = [
    { id: 'PendingAdminApproval', label: 'Pending Review' },
    { id: 'Active',               label: 'Active' },
    { id: 'Voided',               label: 'Voided / Expired' },
    { id: 'all',                  label: 'All' },
  ];

  ngOnInit(): void {
    this.leaseSvc.getAdminLeases().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.all.set(res.data ?? []);
        this.loading.set(false);
        this.pendingCountChange.emit(this.pendingCount());
      },
      error: () => this.loading.set(false),
    });
  }

  setFilter(f: FilterTab): void {
    this.filter.set(f);
  }

  openApproveDialog(lease: LeaseDetailResponse): void {
    const data: ApproveLeaseDialogData = {
      leaseId: lease.leaseId,
      propertyTitle: lease.propertyTitle,
      tenantName: lease.tenantName,
      ownerName: lease.ownerName,
    };
    this.dialog.open(ApproveLeaseDialogComponent, {
      data,
      maxWidth: '520px',
      width: '100%',
      panelClass: 'rounded-dialog',
      autoFocus: false,
    }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => {
      if (result) {
        const newStatus: LeaseStatusString = result.approved ? 'Active' : 'Voided';
        this.all.update(list =>
          list.map(l => l.leaseId === lease.leaseId ? { ...l, status: newStatus } : l)
        );
        this.pendingCountChange.emit(this.pendingCount());
      }
    });
  }
}
