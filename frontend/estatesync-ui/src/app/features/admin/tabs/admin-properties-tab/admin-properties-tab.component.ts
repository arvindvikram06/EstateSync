import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { SlicePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { Subject, debounceTime } from 'rxjs';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { AdminService } from '@core/services/admin/admin.service';
import { DataRefreshService } from '@core/services/shared/data-refresh.service';
import { formatCurrency } from '../../../../core/utils/format.utils';
import { PropertySummaryResponse } from '../../../../core/models/admin.model';
import { AdminReviewDialogComponent, AdminReviewDialogData } from '../../dialogs/admin-review-dialog.component';

@Component({
  selector: 'app-admin-properties-tab',
  standalone: true,
  imports: [
    FormsModule, SlicePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatProgressSpinnerModule,
    EmptyStateComponent, SkeletonLoaderComponent, StatusBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-properties-tab.component.html',
  styleUrl: '../admin-kyc-tab/admin-kyc-tab.component.css',
})
export class AdminPropertiesTabComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly dataRefresh  = inject(DataRefreshService);
  private readonly dialog       = inject(MatDialog);
  private readonly destroyRef   = inject(DestroyRef);

  readonly loading       = signal(true);
  readonly items         = signal<PropertySummaryResponse[]>([]);
  readonly totalCount    = signal(0);
  readonly page          = signal(1);
  readonly limit         = 20;
  readonly formatCurrency = formatCurrency;

  readonly search       = signal('');
  readonly statusFilter = signal('');

  readonly hasFilters = computed(() => !!this.search() || !!this.statusFilter());
  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.limit));

  private readonly search$ = new Subject<void>();

  readonly statusOptions = [
    { value: '',                label: 'All Statuses' },
    { value: 'PendingApproval', label: 'Pending Approval' },
    { value: 'Active',          label: 'Active' },
    { value: 'Rejected',        label: 'Rejected' },
    { value: 'Leased',          label: 'Leased' },
    { value: 'AvailableSoon',   label: 'Available Soon' },
  ];

  ngOnInit(): void {
    this.search$.pipe(debounceTime(400), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => { this.page.set(1); this.load(); });
    this.load();
    this.dataRefresh.on('admin-properties').pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load(true));
  }

  load(silent = false): void {
    if (!silent) this.loading.set(true);
    this.adminService.getAllProperties({
      search: this.search() || undefined,
      status: this.statusFilter() || undefined,
    }, this.page(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.items.set(res.data ?? []); this.totalCount.set(res.total); this.loading.set(false); },
        error: () => this.loading.set(false),
      });
  }

  onSearchInput(): void { this.search$.next(); }
  onFilterChange(): void { this.page.set(1); this.load(); }

  clearFilters(): void {
    this.search.set('');
    this.statusFilter.set('');
    this.page.set(1);
    this.load();
  }

  openReview(item: PropertySummaryResponse): void {
    const data: AdminReviewDialogData = {
      title: 'Review Property',
      subtitle: `${item.title} · ${item.city}, ${item.state} — Owner: ${item.ownerName} (${item.ownerEmail})`,
      entityId: item.propertyId,
      type: 'property',
    };
    const ref = this.dialog.open(AdminReviewDialogComponent, { width: '820px', maxWidth: '96vw', data });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(r => { if (r) this.load(); });
  }

  prevPage(): void { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
  nextPage(): void { if (this.page() < this.totalPages()) { this.page.update(p => p + 1); this.load(); } }
}
