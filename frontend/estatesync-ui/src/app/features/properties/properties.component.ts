import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, of } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';
import { ActivatedRoute } from '@angular/router';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { PropertyService } from '../../core/services/property.service';
import { PropertySummaryResponse, PropertyFilters } from '../../core/models/property.model';
import { PropertyCardComponent } from './property-card/property-card.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-properties',
  standalone: true,
  imports: [
    PropertyCardComponent,
    EmptyStateComponent,
    MatChipsModule, MatButtonModule, MatIconModule,
    MatPaginatorModule, MatProgressBarModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './properties.component.html',
  styleUrl: './properties.component.css'
})
export class PropertiesComponent implements OnInit {
  private readonly propertyService = inject(PropertyService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly loadTrigger$ = new Subject<void>();

  readonly properties = signal<PropertySummaryResponse[]>([]);
  readonly isLoading = signal(true);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly limit = 12;

  // Filter pills
  readonly typeFilter     = signal<string>('');
  readonly bedroomsFilter = signal<string>('');
  readonly rentFilter     = signal<string>('');
  readonly stateFilter    = signal<string>('');

  readonly hasActiveFilters = computed(() =>
    !!this.typeFilter() || !!this.bedroomsFilter() || !!this.rentFilter() || !!this.stateFilter()
  );

  readonly typeOptions = [
    { label: 'All', value: '' },
    { label: 'Villa', value: 'Villa' },
    { label: 'Ind. House', value: 'IndependentHouse' },
  ];
  readonly bedroomsOptions = [
    { label: 'Any', value: '' },
    { label: '2+', value: '2' },
    { label: '3+', value: '3' },
    { label: '4+', value: '4' },
  ];
  readonly rentOptions = [
    { label: 'Any', value: '' },
    { label: 'Under ₹40K', value: 'under40' },
    { label: '₹40K–₹70K', value: '40to70' },
    { label: 'Above ₹70K', value: 'above70' },
  ];

  ngOnInit(): void {
    const state = this.route.snapshot.queryParamMap.get('state') ?? '';
    this.stateFilter.set(state);

    // Setup the reactive pipeline with switchMap to prevent race conditions!
    this.loadTrigger$.pipe(
      switchMap(() => {
        this.isLoading.set(true);
        const filters: PropertyFilters = {};

        if (this.stateFilter())    filters['state']        = this.stateFilter();
        if (this.typeFilter())     filters['propertyType'] = this.typeFilter();
        if (this.bedroomsFilter()) filters['bedrooms']     = Number(this.bedroomsFilter());

        const rent = this.rentFilter();
        if (rent === 'under40')  { filters['maxRent'] = 40000; }
        if (rent === '40to70')   { filters['minRent'] = 40000; filters['maxRent'] = 70000; }
        if (rent === 'above70')  { filters['minRent'] = 70000; }

        return this.propertyService.getProperties(filters, this.page(), this.limit).pipe(
          catchError(() => {
            this.properties.set([]);
            this.isLoading.set(false);
            return of(null); // safely ignore errors
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(res => {
      if (res) {
        this.properties.set(res.data ?? []);
        this.total.set(res.total ?? 0);
        this.isLoading.set(false);
      }
    });

    // Trigger initial load
    this.loadProperties();
  }

  setType(value: string): void     { this.typeFilter.set(value);     this.page.set(1); this.loadProperties(); }
  setBedrooms(value: string): void { this.bedroomsFilter.set(value); this.page.set(1); this.loadProperties(); }
  setRent(value: string): void     { this.rentFilter.set(value);     this.page.set(1); this.loadProperties(); }

  clearFilters(): void {
    this.typeFilter.set('');
    this.bedroomsFilter.set('');
    this.rentFilter.set('');
    this.stateFilter.set('');
    this.page.set(1);
    this.loadProperties();
  }

  loadProperties(): void {

    this.loadTrigger$.next();
  }

  get totalPages(): number { return Math.ceil(this.total() / this.limit); }

  nextPage(): void { if (this.page() < this.totalPages) { this.page.update(p => p + 1); this.loadProperties(); } }
  prevPage(): void { if (this.page() > 1)               { this.page.update(p => p - 1); this.loadProperties(); } }

  onPageChange(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.loadProperties();
  }
}
