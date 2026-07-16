import { Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef, input, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SiteVisitService } from '@core/services/property/site-visit.service';
import { DataRefreshService } from '@core/services/shared/data-refresh.service';
import { SiteVisitResponse } from '@core/models/site-visit.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';

@Component({
  selector: 'app-visits-tab',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    StatusBadgeComponent,
    EmptyStateComponent,
    SkeletonLoaderComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './visits-tab.component.html',
  styleUrl: './visits-tab.component.css'
})
export class VisitsTabComponent implements OnInit {
  isOwnerView = input<boolean>(false);

  private readonly siteVisitService = inject(SiteVisitService);
  private readonly dataRefresh = inject(DataRefreshService);
  private readonly destroyRef = inject(DestroyRef);

  readonly visits = signal<SiteVisitResponse[]>([]);
  readonly loading = signal(true);
  readonly actionRunningId = signal<string | null>(null);

  readonly declineNotesVisitId = signal<string | null>(null);
  readonly declineReason = signal<string>('');

  readonly searchQuery = signal('');
  readonly dateFilter = signal<Date | null>(null);

  readonly filteredVisits = computed(() => {
    let result = this.visits();
    const q = this.searchQuery().toLowerCase().trim();
    if (q) result = result.filter(v => v.tenantName?.toLowerCase().includes(q));
    const d = this.dateFilter();
    if (d) {
      const dateStr = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
      result = result.filter(v => v.visitDate.startsWith(dateStr));
    }
    return result;
  });

  readonly hasFilters = computed(() => !!this.searchQuery() || !!this.dateFilter());

  ngOnInit(): void {
    this.loadVisits();
    this.dataRefresh.on('visits').pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadVisits(true);
    });
  }

  loadVisits(silent = false): void {
    if (!silent) this.loading.set(true);
    const request$ = this.isOwnerView()
      ? this.siteVisitService.getOwnerVisits()
      : this.siteVisitService.getTenantVisits();

    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.visits.set(res.data ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  clearFilters(): void {
    this.searchQuery.set('');
    this.dateFilter.set(null);
  }

  approveVisit(visit: SiteVisitResponse): void {
    this.actionRunningId.set(visit.siteVisitId);
    this.siteVisitService.updateStatus(visit.siteVisitId, { status: 'Approved', ownerNotes: null })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.actionRunningId.set(null); if (res.success) this.loadVisits(); },
        error: () => this.actionRunningId.set(null)
      });
  }

  markComplete(visit: SiteVisitResponse): void {
    this.actionRunningId.set(visit.siteVisitId);
    this.siteVisitService.updateStatus(visit.siteVisitId, { status: 'Completed', ownerNotes: null })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.actionRunningId.set(null); if (res.success) this.loadVisits(); },
        error: () => this.actionRunningId.set(null)
      });
  }

  startDecline(visit: SiteVisitResponse): void {
    this.declineNotesVisitId.set(visit.siteVisitId);
    this.declineReason.set('');
  }

  cancelDecline(): void {
    this.declineNotesVisitId.set(null);
    this.declineReason.set('');
  }

  submitDecline(visit: SiteVisitResponse): void {
    this.actionRunningId.set(visit.siteVisitId);
    this.siteVisitService.updateStatus(visit.siteVisitId, {
      status: 'Declined',
      ownerNotes: this.declineReason().trim() || 'No reason provided.'
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.actionRunningId.set(null);
          this.declineNotesVisitId.set(null);
          this.declineReason.set('');
          if (res.success) this.loadVisits();
        },
        error: () => this.actionRunningId.set(null)
      });
  }

  cancelVisit(visit: SiteVisitResponse): void {
    if (!confirm('Are you sure you want to cancel this visit request?')) return;
    this.actionRunningId.set(visit.siteVisitId);
    this.siteVisitService.updateStatus(visit.siteVisitId, { status: 'Cancelled', ownerNotes: null })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.actionRunningId.set(null); if (res.success) this.loadVisits(); },
        error: () => this.actionRunningId.set(null)
      });
  }
}
