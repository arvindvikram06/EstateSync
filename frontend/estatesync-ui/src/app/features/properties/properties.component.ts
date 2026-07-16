import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, of } from 'rxjs';
import { switchMap, catchError, debounceTime } from 'rxjs/operators';
import { ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog } from '@angular/material/dialog';
import { PropertyService } from '@core/services/property/property.service';
import { ComparisonService } from '@core/services/property/comparison.service';
import { PropertySummaryResponse, PropertyFilters } from '../../core/models/property.model';
import { PropertyCardComponent } from './property-card/property-card.component';
import { PropertyCompareDialogComponent } from './property-compare-dialog/property-compare-dialog.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-properties',
  standalone: true,
  imports: [
    PropertyCardComponent,
    EmptyStateComponent,
    MatButtonModule, MatIconModule,
    MatPaginatorModule, MatProgressBarModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './properties.component.html',
  styleUrl: './properties.component.css'
})
export class PropertiesComponent implements OnInit {
  private readonly propertyService = inject(PropertyService);
  readonly comparisonService = inject(ComparisonService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly loadTrigger$ = new Subject<void>();

  readonly properties = signal<PropertySummaryResponse[]>([]);
  readonly isLoading = signal(true);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly limit = 12;

  readonly searchFilter   = signal<string>('');
  readonly typeFilter     = signal<string>('');
  readonly bedroomsMin    = signal<number | null>(null);
  readonly rentMin        = signal<number | null>(null);
  readonly rentMax        = signal<number | null>(null);
  readonly rentPreset     = signal<string>('');
  readonly showCustomRent = signal(false);

  private readonly rentCustomChange$ = new Subject<void>();

  readonly activeFilterCount = computed(() => {
    let n = 0;
    if (this.searchFilter()) n++;
    if (this.typeFilter()) n++;
    if (this.bedroomsMin() !== null) n++;
    if (this.rentMin() !== null || this.rentMax() !== null) n++;
    return n;
  });

  readonly showClearAll = computed(() => this.activeFilterCount() > 1);

  readonly isCustomBedrooms = computed(() => {
    const b = this.bedroomsMin();
    return b !== null && ![2, 3, 4].includes(b);
  });

  readonly typeOptions = [
    { label: 'All', value: '' },
    { label: 'Villa', value: 'Villa' },
    { label: 'Ind. House', value: 'IndependentHouse' },
  ];
  readonly bedroomOptions = [
    { label: 'Any', value: null as number | null },
    { label: '2+', value: 2 },
    { label: '3+', value: 3 },
    { label: '4+', value: 4 },
  ];
  readonly rentPresets = [
    { label: 'Any',          value: '' },
    { label: 'Under ₹40K',  value: 'under40' },
    { label: '₹40K–₹70K',  value: '40to70' },
    { label: 'Above ₹70K',  value: 'above70' },
  ];

  ngOnInit(): void {
    // Setup reactive pipeline first
    this.loadTrigger$.pipe(
      switchMap(() => {
        this.isLoading.set(true);
        const filters: PropertyFilters = {};

        if (this.searchFilter())       filters['search']       = this.searchFilter();
        if (this.typeFilter())         filters['propertyType'] = this.typeFilter();
        if (this.bedroomsMin() !== null) filters['bedrooms']   = this.bedroomsMin()!;
        if (this.rentMin() !== null)   filters['minRent']      = this.rentMin()!;
        if (this.rentMax() !== null)   filters['maxRent']      = this.rentMax()!;

        return this.propertyService.getProperties(filters, this.page(), this.limit).pipe(
          catchError(() => {
            this.properties.set([]);
            this.isLoading.set(false);
            return of(null);
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

    // React to URL query params — handles both initial load and same-route navbar searches
    this.route.queryParamMap.pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(params => {
      this.searchFilter.set(params.get('search') ?? '');
      this.page.set(1);
      this.loadProperties();
    });

    // Debounced trigger for custom rent inputs
    this.rentCustomChange$.pipe(
      debounceTime(600),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => { this.page.set(1); this.loadProperties(); });
  }

  setType(v: string): void          { this.typeFilter.set(v);   this.page.set(1); this.loadProperties(); }
  clearSearch(): void                { this.searchFilter.set(''); this.page.set(1); this.loadProperties(); }

  setBedrooms(v: number | null): void {
    this.bedroomsMin.set(v);
    this.page.set(1);
    this.loadProperties();
  }

  onCustomBedrooms(e: Event): void {
    const v = parseInt((e.target as HTMLInputElement).value, 10);
    this.bedroomsMin.set(isNaN(v) || v < 1 ? null : v);
    this.page.set(1);
    this.loadProperties();
  }

  setRentPreset(preset: string): void {
    this.rentPreset.set(preset);
    this.showCustomRent.set(false);
    this.rentMin.set(null);
    this.rentMax.set(null);
    if (preset === 'under40')       { this.rentMax.set(40000); }
    else if (preset === '40to70')   { this.rentMin.set(40000); this.rentMax.set(70000); }
    else if (preset === 'above70')  { this.rentMin.set(70000); }
    this.page.set(1);
    this.loadProperties();
  }

  toggleCustomRent(): void {
    const next = !this.showCustomRent();
    this.showCustomRent.set(next);
    if (next) { this.rentPreset.set(''); this.rentMin.set(null); this.rentMax.set(null); }
    else       { this.page.set(1); this.loadProperties(); }
  }

  onCustomRentMin(e: Event): void {
    const v = +(e.target as HTMLInputElement).value;
    this.rentMin.set(v > 0 ? v : null);
    this.rentCustomChange$.next();
  }

  onCustomRentMax(e: Event): void {
    const v = +(e.target as HTMLInputElement).value;
    this.rentMax.set(v > 0 ? v : null);
    this.rentCustomChange$.next();
  }

  clearFilters(): void {
    this.searchFilter.set('');
    this.typeFilter.set('');
    this.bedroomsMin.set(null);
    this.rentMin.set(null);
    this.rentMax.set(null);
    this.rentPreset.set('');
    this.showCustomRent.set(false);
    this.page.set(1);
    this.loadProperties();
  }

  loadProperties(): void {
    this.loadTrigger$.next();
  }

  readonly totalPages = computed(() => Math.ceil(this.total() / this.limit));

  nextPage(): void { if (this.page() < this.totalPages()) { this.page.update(p => p + 1); this.loadProperties(); } }
  prevPage(): void { if (this.page() > 1)                { this.page.update(p => p - 1); this.loadProperties(); } }

  onPageChange(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.loadProperties();
  }

  openComparison(): void {
    const selected = this.comparisonService.selectedProperties();
    if (selected.length < 2) return;
    this.dialog.open(PropertyCompareDialogComponent, {
      data: { properties: selected },
      width: '95vw',
      maxWidth: '1000px',
      maxHeight: '90vh',
      panelClass: 'compare-dialog-panel'
    });
  }
}
