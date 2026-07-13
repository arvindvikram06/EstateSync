import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LeaseService } from '@core/services/leasing/lease.service';
import { OwnerLeaseSummaryResponse, LeaseStatusString } from '../../../../core/models/lease.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { formatDate, formatCurrency } from '../../../../core/utils/format.utils';

interface StatusPill {
  label: string;
  value: LeaseStatusString | null;
}

const STATUS_PILLS: StatusPill[] = [
  { label: 'All',            value: null },
  { label: 'Active',         value: 'Active' },
  { label: 'Pending',        value: 'PendingApproval' },
  { label: 'Partial Signed', value: 'PartialSigned' },
  { label: 'Awaiting Admin', value: 'PendingAdminApproval' },
  { label: 'Terminated',     value: 'Terminated' },
  { label: 'Voided',         value: 'Voided' },
  { label: 'Expired',        value: 'Expired' },
];

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
  private readonly destroyRef = inject(DestroyRef);

  readonly leases  = signal<OwnerLeaseSummaryResponse[]>([]);
  readonly loading = signal(true);

  readonly searchQuery  = signal('');
  readonly activeStatus = signal<LeaseStatusString | null>(null);

  readonly statusPills = STATUS_PILLS;

  readonly leaseRows = computed(() => this.leases().map(l => ({ ...l })));

  readonly filteredRows = computed(() => {
    const q      = this.searchQuery().toLowerCase().trim();
    const status = this.activeStatus();
    return this.leaseRows().filter(l => {
      const matchesStatus = status === null || l.status === status;
      const matchesSearch = !q ||
        (l.propertyTitle ?? '').toLowerCase().includes(q) ||
        (l.tenantName   ?? '').toLowerCase().includes(q);
      return matchesStatus && matchesSearch;
    });
  });

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  ngOnInit(): void {
    this.leaseSvc.getOwnerLeases().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.leases.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  setStatus(value: LeaseStatusString | null): void {
    this.activeStatus.set(value);
  }

  onSearch(event: Event): void {
    this.searchQuery.set((event.target as HTMLInputElement).value);
  }

  clearSearch(): void {
    this.searchQuery.set('');
  }
}
