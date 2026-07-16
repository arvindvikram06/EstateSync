import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef, input } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SiteVisitService } from '@core/services/property/site-visit.service';
import { SiteVisitResponse } from '@core/models/site-visit.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';

@Component({
  selector: 'app-visits-tab',
  standalone: true,
  imports: [
    DatePipe,
    MatIconModule,
    MatButtonModule,
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
  private readonly destroyRef = inject(DestroyRef);

  readonly visits = signal<SiteVisitResponse[]>([]);
  readonly loading = signal(true);
  readonly actionRunningId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadVisits();
  }

  loadVisits(): void {
    this.loading.set(true);
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

  approveVisit(visit: SiteVisitResponse): void {
    this.actionRunningId.set(visit.siteVisitId);
    this.siteVisitService.updateStatus(visit.siteVisitId, {
      status: 'Approved',
      ownerNotes: null
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.actionRunningId.set(null);
          if (res.success) {
            this.loadVisits();
          }
        },
        error: () => this.actionRunningId.set(null)
      });
  }

  declineVisit(visit: SiteVisitResponse): void {
    const note = prompt('Please enter a note for the tenant (e.g. suggesting another time):');
    if (note === null) return; // User cancelled prompt

    this.actionRunningId.set(visit.siteVisitId);
    this.siteVisitService.updateStatus(visit.siteVisitId, {
      status: 'Declined',
      ownerNotes: note.trim() || 'No reason provided.'
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.actionRunningId.set(null);
          if (res.success) {
            this.loadVisits();
          }
        },
        error: () => this.actionRunningId.set(null)
      });
  }

  cancelVisit(visit: SiteVisitResponse): void {
    if (!confirm('Are you sure you want to cancel this visit request?')) return;

    this.actionRunningId.set(visit.siteVisitId);
    this.siteVisitService.updateStatus(visit.siteVisitId, {
      status: 'Cancelled',
      ownerNotes: null
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.actionRunningId.set(null);
          if (res.success) {
            this.loadVisits();
          }
        },
        error: () => this.actionRunningId.set(null)
      });
  }
}
