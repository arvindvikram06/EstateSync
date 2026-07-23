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
import { PropertyVisitSlot, SiteVisitResponse } from '@core/models/site-visit.model';
import { PropertySummaryResponse } from '@core/models/property.model';
import { PropertyService } from '@core/services/property/property.service';
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
    SkeletonLoaderComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './visits-tab.component.html',
  styleUrl: './visits-tab.component.css'
})
export class VisitsTabComponent implements OnInit {
  isOwnerView = input<boolean>(false);

  private readonly siteVisitService = inject(SiteVisitService);
  private readonly propertyService = inject(PropertyService);
  private readonly dataRefresh = inject(DataRefreshService);
  private readonly destroyRef = inject(DestroyRef);
  private sectionInitialized = false;

  readonly visits = signal<SiteVisitResponse[]>([]);
  readonly loading = signal(true);
  readonly actionRunningId = signal<string | null>(null);

  readonly declineNotesVisitId = signal<string | null>(null);
  readonly declineReason = signal<string>('');
  readonly rescheduleVisitId = signal<string | null>(null);
  readonly rescheduleDate = signal<Date | null>(null);
  readonly rescheduleSlotId = signal('');
  readonly rescheduleReason = signal('');
  readonly availableSlots = signal<PropertyVisitSlot[]>([]);
  readonly slotsLoading = signal(false);
  readonly activeSection = signal<'action' | 'upcoming' | 'history'>('action');
  readonly hoursOpen = signal(false);
  readonly properties = signal<PropertySummaryResponse[]>([]);
  readonly hoursPropertyId = signal('');
  readonly hoursDate = signal(this.localDate(new Date()));
  readonly managedSlots = signal<PropertyVisitSlot[]>([]);
  readonly selectedHours = signal<string[]>([]);
  readonly hoursSaving = signal(false);
  readonly workingHours = Array.from({ length: 10 }, (_, index) =>
    `${String(index + 9).padStart(2, '0')}:00`);

  readonly searchQuery = signal('');
  readonly dateFilter = signal<Date | null>(null);

  readonly filteredVisits = computed(() => {
    let result = this.visits().filter(visit => {
      if (this.activeSection() === 'history')
        return ['Completed', 'Cancelled', 'Declined'].includes(visit.status);
      if (this.activeSection() === 'action')
        return this.needsMyAction(visit);
      return !this.needsMyAction(visit) &&
        ['PendingOwnerApproval', 'PendingTenantApproval', 'Approved'].includes(visit.status);
    });
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
  readonly actionCount = computed(() => this.visits().filter(visit => this.needsMyAction(visit)).length);
  readonly upcomingCount = computed(() => this.visits().filter(visit =>
    !this.needsMyAction(visit) &&
    ['PendingOwnerApproval', 'PendingTenantApproval', 'Approved'].includes(visit.status)).length);
  readonly historyCount = computed(() => this.visits().filter(visit =>
    ['Completed', 'Cancelled', 'Declined'].includes(visit.status)).length);
  readonly selectedHoursProperty = computed(() =>
    this.properties().find(property => property.propertyId === this.hoursPropertyId()));

  ngOnInit(): void {
    this.loadVisits();
    if (this.isOwnerView()) this.loadProperties();
    this.dataRefresh.on('visits').pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadVisits(true);
    });
  }

  setSection(section: 'action' | 'upcoming' | 'history'): void {
    this.activeSection.set(section);
    this.clearFilters();
  }

  needsMyAction(visit: SiteVisitResponse): boolean {
    return this.isOwnerView()
      ? visit.status === 'PendingOwnerApproval'
      : visit.status === 'PendingTenantApproval';
  }

  statusLabel(visit: SiteVisitResponse): string {
    if (visit.status === 'PendingOwnerApproval')
      return this.isOwnerView() ? 'Your approval needed' : 'Waiting for owner';
    if (visit.status === 'PendingTenantApproval')
      return this.isOwnerView() ? 'Waiting for tenant' : 'Your approval needed';
    return visit.status === 'Approved' ? 'Confirmed' : visit.status;
  }

  loadProperties(): void {
    this.propertyService.getMyProperties()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(res => {
        this.properties.set(res.data ?? []);
        if (!this.hoursPropertyId() && this.properties().length) {
          this.hoursPropertyId.set(this.properties()[0].propertyId);
          this.loadManagedSlots();
        }
      });
  }

  selectHoursProperty(propertyId: string): void {
    this.hoursPropertyId.set(propertyId);
    this.selectedHours.set([]);
    this.loadManagedSlots();
  }

  selectHoursDate(date: string): void {
    this.hoursDate.set(date);
    this.selectedHours.set([]);
    this.loadManagedSlots();
  }

  loadManagedSlots(): void {
    if (!this.hoursPropertyId() || !this.hoursDate()) return;
    this.slotsLoading.set(true);
    this.siteVisitService.getManageSlots(this.hoursPropertyId(), this.hoursDate(), this.hoursDate())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.managedSlots.set(res.data ?? []);
          this.slotsLoading.set(false);
        },
        error: () => this.slotsLoading.set(false)
      });
  }

  managedSlotAt(time: string): PropertyVisitSlot | undefined {
    return this.managedSlots().find(slot => slot.startTime.slice(0, 5) === time);
  }

  toggleNewHour(time: string): void {
    if (this.managedSlotAt(time)) return;
    this.selectedHours.update(hours =>
      hours.includes(time) ? hours.filter(hour => hour !== time) : [...hours, time]);
  }

  publishHours(): void {
    if (!this.hoursPropertyId() || !this.selectedHours().length) return;
    this.hoursSaving.set(true);
    this.siteVisitService.createSlots(this.hoursPropertyId(), this.hoursDate(), this.selectedHours())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.selectedHours.set([]);
          this.hoursSaving.set(false);
          this.loadManagedSlots();
        },
        error: () => this.hoursSaving.set(false)
      });
  }

  toggleManagedSlot(slot: PropertyVisitSlot): void {
    if (slot.status === 'Reserved' || slot.status === 'Booked') return;
    this.hoursSaving.set(true);
    this.siteVisitService.setSlotBlocked(slot.propertyId, slot.slotId, slot.status === 'Available')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.hoursSaving.set(false);
          this.loadManagedSlots();
        },
        error: () => this.hoursSaving.set(false)
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
        if (!this.sectionInitialized) {
          this.sectionInitialized = true;
          if (this.actionCount() === 0 && this.upcomingCount() > 0) this.activeSection.set('upcoming');
          else if (this.actionCount() === 0 && this.historyCount() > 0) this.activeSection.set('history');
        }
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

  startReschedule(visit: SiteVisitResponse): void {
    this.rescheduleVisitId.set(visit.siteVisitId);
    this.rescheduleDate.set(null);
    this.rescheduleSlotId.set('');
    this.rescheduleReason.set('');
    this.availableSlots.set([]);
  }

  closeReschedule(): void {
    this.rescheduleVisitId.set(null);
    this.availableSlots.set([]);
  }

  loadSlots(visit: SiteVisitResponse, date: Date | null): void {
    this.rescheduleDate.set(date);
    this.rescheduleSlotId.set('');
    this.availableSlots.set([]);
    if (!date) return;
    const day = this.localDate(date);
    this.slotsLoading.set(true);
    this.siteVisitService.getAvailableSlots(visit.propertyId, day, day)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.availableSlots.set(res.data ?? []);
          this.slotsLoading.set(false);
        },
        error: () => this.slotsLoading.set(false)
      });
  }

  submitReschedule(visit: SiteVisitResponse): void {
    if (!this.rescheduleSlotId() || !this.rescheduleReason().trim()) return;
    this.actionRunningId.set(visit.siteVisitId);
    this.siteVisitService.reschedule(visit.siteVisitId, {
      newSlotId: this.rescheduleSlotId(),
      reason: this.rescheduleReason().trim()
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.actionRunningId.set(null);
        if (res.success) {
          this.closeReschedule();
          this.loadVisits();
        }
      },
      error: () => this.actionRunningId.set(null)
    });
  }

  respondToReschedule(visit: SiteVisitResponse, accept: boolean): void {
    this.actionRunningId.set(visit.siteVisitId);
    const request$ = accept
      ? this.siteVisitService.confirmReschedule(visit.siteVisitId)
      : this.siteVisitService.rejectReschedule(visit.siteVisitId);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.actionRunningId.set(null);
        if (res.success) this.loadVisits();
      },
      error: () => this.actionRunningId.set(null)
    });
  }

  private localDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
