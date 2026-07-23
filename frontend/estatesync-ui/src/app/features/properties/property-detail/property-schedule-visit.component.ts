import { Component, ChangeDetectionStrategy, OnInit, DestroyRef, inject, input, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SiteVisitService } from '@core/services/property/site-visit.service';
import { AuthService } from '@core/services/auth/auth.service';
import { PropertyVisitSlot, SiteVisitResponse } from '@core/models/site-visit.model';

@Component({
  selector: 'app-property-schedule-visit',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatInputModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './property-schedule-visit.component.html',
  styleUrl: './property-schedule-visit.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PropertyScheduleVisitComponent implements OnInit {
  propertyId = input.required<string>();
  ownerId = input.required<string>();

  private readonly siteVisitService = inject(SiteVisitService);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly isLoggedIn = this.authService.isLoggedIn;
  
  readonly visits = signal<SiteVisitResponse[]>([]);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly selectedDate = signal<Date | null>(null);
  readonly selectedSlotId = signal('');
  readonly availableSlots = signal<PropertyVisitSlot[]>([]);
  readonly allAvailableSlots = signal<PropertyVisitSlot[]>([]);
  readonly slotsLoading = signal(false);

  readonly minDate = new Date();
  readonly maxDate = new Date(new Date().getTime() + 60 * 24 * 60 * 60 * 1000); // 60 days out

  readonly activeVisit = computed(() => 
    this.visits().find(v =>
      v.status === 'PendingOwnerApproval' ||
      v.status === 'PendingTenantApproval' ||
      v.status === 'Approved')
  );

  readonly availableDates = computed(() => {
    const unique = [...new Set(this.allAvailableSlots().map(slot => slot.date.slice(0, 10)))];
    return unique.slice(0, 6);
  });

  readonly declinedVisit = computed(() => {
    const declined = this.visits().filter(v => v.status === 'Declined');
    return declined.length > 0 ? declined[declined.length - 1] : undefined;
  });

  ngOnInit(): void {
    if (this.isLoggedIn()) {
      this.loadVisits();
      this.loadAvailability();
    }
  }

  loadVisits(): void {
    this.loading.set(true);
    this.siteVisitService.getTenantVisits()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          if (res.success && res.data) {
            // Filter visits for this property
            const filtered = res.data.filter(v => v.propertyId === this.propertyId());
            this.visits.set(filtered);
          }
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }

  onDateChange(date: Date | null): void {
    this.selectedDate.set(date);
    this.selectedSlotId.set('');
    const day = date ? this.toLocalDate(date) : '';
    this.availableSlots.set(
      this.allAvailableSlots().filter(slot => slot.date.slice(0, 10) === day)
    );
  }

  chooseAvailableDate(date: string): void {
    const [year, month, day] = date.split('-').map(Number);
    this.onDateChange(new Date(year, month - 1, day));
  }

  loadAvailability(): void {
    const from = this.toLocalDate(this.minDate);
    const to = this.toLocalDate(this.maxDate);
    this.slotsLoading.set(true);
    this.siteVisitService.getAvailableSlots(this.propertyId(), from, to)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.allAvailableSlots.set(res.data ?? []);
          this.onDateChange(this.selectedDate());
          this.slotsLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('Unable to load the owner’s available visit times.');
          this.slotsLoading.set(false);
        }
      });
  }

  submitRequest(): void {
    const date = this.selectedDate();
    const slotId = this.selectedSlotId();

    if (!date) {
      this.errorMessage.set('Please select a date.');
      return;
    }
    if (!slotId) {
      this.errorMessage.set('Please select an available time.');
      return;
    }

    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.submitting.set(true);

    this.siteVisitService.requestVisit({
      propertyId: this.propertyId(),
      slotId
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.submitting.set(false);
          if (res.success) {
            this.successMessage.set('Visit request submitted successfully!');
            this.selectedDate.set(null);
            this.selectedSlotId.set('');
            this.availableSlots.set([]);
            this.loadVisits();
            this.loadAvailability();
          } else {
            this.errorMessage.set(res.message || 'Failed to submit request.');
          }
        },
        error: err => {
          this.submitting.set(false);
          this.errorMessage.set(err.error?.message || 'An error occurred while submitting.');
        }
      });
  }

  cancelActiveVisit(): void {
    const active = this.activeVisit();
    if (!active) return;

    if (!confirm('Are you sure you want to cancel this visit request?')) return;

    this.submitting.set(true);
    this.siteVisitService.updateStatus(active.siteVisitId, {
      status: 'Cancelled',
      ownerNotes: null
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.submitting.set(false);
          if (res.success) {
            this.successMessage.set('Visit request cancelled.');
            this.loadVisits();
          } else {
            this.errorMessage.set(res.message || 'Failed to cancel request.');
          }
        },
        error: err => {
          this.submitting.set(false);
          this.errorMessage.set(err.error?.message || 'An error occurred.');
        }
      });
  }

  bookAnother(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.visits.update(list => list.filter(v => v.status !== 'Declined'));
  }

  private toLocalDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }
}
