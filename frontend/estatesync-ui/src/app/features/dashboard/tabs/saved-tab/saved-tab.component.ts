import { Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { SavedPropertyService } from '@core/services/property/saved-property.service';
import { ComparisonService } from '@core/services/property/comparison.service';
import { PropertySummaryResponse } from '../../../../core/models/property.model';
import { PropertyCardComponent } from '../../../properties/property-card/property-card.component';
import { PropertyCompareDialogComponent } from '../../../properties/property-compare-dialog/property-compare-dialog.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-saved-tab',
  standalone: true,
  imports: [
    PropertyCardComponent,
    EmptyStateComponent,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatButtonModule,
    MatIconModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isLoading()) {
      <div class="flex items-center gap-3 py-16 justify-center text-sm text-es-muted">
        <mat-spinner diameter="24" aria-label="Loading saved properties" /><span>Loading saved properties…</span>
      </div>
    } @else if (properties().length === 0) {
      <app-empty-state icon="favorite_border" title="No saved properties yet"
        subtitle="Tap the heart on any listing to save it here" />
    } @else {
      <div class="flex items-center justify-between mb-6">
        <h3 class="text-sm font-semibold text-es-muted uppercase tracking-wider">My Saved Listings</h3>
        <button type="button" (click)="comparisonService.isCompareMode.set(!comparisonService.isCompareMode())"
          class="filter-pill flex items-center gap-1.5"
          [class.filter-pill--active]="comparisonService.isCompareMode()">
          <mat-icon class="!text-[14px] !w-[14px] !h-[14px]">compare_arrows</mat-icon>
          Compare Saved
        </button>
      </div>

      <div class="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-7 pb-20">
        @for (property of properties(); track property.propertyId) {
          <app-property-card [property]="property" />
        }
      </div>

      @if (total() > limit) {
        <mat-paginator class="mt-10 bg-transparent"
          [length]="total()"
          [pageSize]="limit"
          [pageIndex]="page() - 1"
          [hidePageSize]="true"
          (page)="onPageChange($event)" />
      }

      <!-- Bottom floating comparison drawer -->
      @if (comparisonService.selectedCount() > 0) {
        <div class="fixed bottom-4 left-4 right-4 bg-white border border-gray-100 shadow-[0_-4px_24px_rgba(0,0,0,0.08)] py-4 px-6 z-50 rounded-card animate-slide-up">
          <div class="max-w-[1200px] mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
            <div class="flex items-center gap-4 flex-wrap">
              <span class="text-sm font-semibold text-es-text shrink-0">Comparing ({{ comparisonService.selectedCount() }}/3)</span>
              <div class="flex gap-2 flex-wrap">
                @for (p of comparisonService.selectedProperties(); track p.propertyId) {
                  <div class="flex items-center gap-1.5 bg-gray-50 border border-gray-100 px-3 py-1 rounded-full text-xs text-es-text shadow-sm">
                    <span class="truncate max-w-[150px] font-medium">{{ p.title }}</span>
                    <button (click)="comparisonService.toggleProperty(p)" class="text-es-muted hover:text-red-500 border-none bg-transparent cursor-pointer flex items-center justify-center font-bold text-sm">×</button>
                  </div>
                }
              </div>
            </div>
            <div class="flex gap-3 w-full sm:w-auto shrink-0 justify-end">
              <button mat-button (click)="comparisonService.clear()" class="!rounded-full text-xs">Clear all</button>
              <button mat-flat-button color="primary" [disabled]="comparisonService.selectedCount() < 2" (click)="openComparison()" class="!rounded-full text-xs shadow-sm bg-es-primary text-white hover:bg-es-primary/95">Compare Now</button>
            </div>
          </div>
        </div>
      }
    }
  `
})
export class SavedTabComponent implements OnInit {
  private readonly savedPropertyService = inject(SavedPropertyService);
  readonly comparisonService = inject(ComparisonService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly properties = signal<PropertySummaryResponse[]>([]);
  readonly isLoading = signal(true);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly limit = 12;

  ngOnInit(): void {
    this.savedPropertyService.ensureIdsLoaded();
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.savedPropertyService.getSaved(this.page(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          const items = res.data ? [...res.data] : [];
          this.properties.set(items);
          // Everything on this page is saved by definition — seed the shared
          // set so the grid renders without waiting for the /ids call.
          this.savedPropertyService.addIds(items.map(p => p.propertyId));
          this.total.set(res.total ?? 0);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false),
      });
  }

  onPageChange(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.load();
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
