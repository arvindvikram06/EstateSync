import { Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SavedPropertyService } from '@core/services/property/saved-property.service';
import { PropertySummaryResponse } from '../../../../core/models/property.model';
import { PropertyCardComponent } from '../../../properties/property-card/property-card.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';

@Component({
  selector: 'app-saved-tab',
  standalone: true,
  imports: [PropertyCardComponent, EmptyStateComponent, MatPaginatorModule, MatProgressSpinnerModule],
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
      <div class="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-7">
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
    }
  `
})
export class SavedTabComponent implements OnInit {
  private readonly savedPropertyService = inject(SavedPropertyService);
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
}
