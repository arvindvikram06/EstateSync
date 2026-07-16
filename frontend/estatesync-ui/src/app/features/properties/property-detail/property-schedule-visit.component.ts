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
import { SiteVisitResponse } from '@core/models/site-visit.model';

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
  readonly selectedTime = signal<string>(''); // "HH:mm"

  readonly timeSlots = [
    { value: '09:00', label: '9:00 AM' },
    { value: '10:00', label: '10:00 AM' },
    { value: '11:00', label: '11:00 AM' },
    { value: '12:00', label: '12:00 PM' },
    { value: '13:00', label: '1:00 PM' },
    { value: '14:00', label: '2:00 PM' },
    { value: '15:00', label: '3:00 PM' },
    { value: '16:00', label: '4:00 PM' },
    { value: '17:00', label: '5:00 PM' },
    { value: '18:00', label: '6:00 PM' },
  ];

  readonly minDate = new Date();
  readonly maxDate = new Date(new Date().getTime() + 60 * 24 * 60 * 60 * 1000); // 60 days out

  readonly activeVisit = computed(() => 
    this.visits().find(v => v.status === 'Pending' || v.status === 'Approved')
  );

  readonly declinedVisit = computed(() => {
    const declined = this.visits().filter(v => v.status === 'Declined');
    return declined.length > 0 ? declined[declined.length - 1] : undefined;
  });

  ngOnInit(): void {
    if (this.isLoggedIn()) {
      this.loadVisits();
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

  submitRequest(): void {
    const date = this.selectedDate();
    const time = this.selectedTime();

    if (!date) {
      this.errorMessage.set('Please select a date.');
      return;
    }
    if (!time) {
      this.errorMessage.set('Please select a time.');
      return;
    }

    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.submitting.set(true);

    // Format date as local date but represented in ISO format
    const offset = date.getTimezoneOffset();
    const localDate = new Date(date.getTime() - (offset * 60 * 1000));
    const isoDateStr = localDate.toISOString().split('T')[0] + 'T00:00:00Z';

    this.siteVisitService.requestVisit({
      propertyId: this.propertyId(),
      visitDate: isoDateStr,
      visitTime: time
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.submitting.set(false);
          if (res.success) {
            this.successMessage.set('Visit request submitted successfully!');
            this.selectedDate.set(null);
            this.selectedTime.set('');
            this.loadVisits();
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

  reschedule(): void {
    // Clear display state of declined visit so form displays again
    this.errorMessage.set(null);
    this.successMessage.set(null);
    // Remove the declined visit from the list locally so the UI switches to booking form
    this.visits.update(list => list.filter(v => v.status !== 'Declined'));
  }
}
