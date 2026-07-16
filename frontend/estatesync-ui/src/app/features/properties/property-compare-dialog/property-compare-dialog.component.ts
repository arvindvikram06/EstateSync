import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PropertyService } from '@core/services/property/property.service';
import { PropertySummaryResponse, PropertyDetailResponse } from '@core/models/property.model';
import { forkJoin } from 'rxjs';
import { formatCurrency } from '@core/utils/format.utils';

@Component({
  selector: 'app-property-compare-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './property-compare-dialog.component.html',
  styleUrl: './property-compare-dialog.component.css'
})
export class PropertyCompareDialogComponent implements OnInit {
  private readonly dialogRef = inject(MatDialogRef<PropertyCompareDialogComponent>);
  private readonly data = inject<{ properties: PropertySummaryResponse[] }>(MAT_DIALOG_DATA);
  private readonly propService = inject(PropertyService);

  readonly properties = signal<PropertySummaryResponse[]>(this.data.properties);
  readonly detailedProperties = signal<PropertyDetailResponse[]>([]);
  readonly isLoading = signal(true);

  readonly formatRent = formatCurrency;
  readonly allAmenities = signal<string[]>([]);

  ngOnInit(): void {
    const ids = this.properties().map(p => p.propertyId);
    
    // Execute parallel requests to fetch detailed specifications (amenities, deposit)
    forkJoin(ids.map(id => this.propService.getProperty(id))).subscribe({
      next: responses => {
        const details = responses.map(r => r.data);
        this.detailedProperties.set(details);
        
        // Extract unique sorted union of all amenities across compared listings
        const amenitiesSet = new Set<string>();
        details.forEach(p => p.amenities?.forEach(a => amenitiesSet.add(a)));
        this.allAmenities.set(Array.from(amenitiesSet).sort());
        
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  hasAmenity(property: PropertyDetailResponse, amenity: string): boolean {
    return property.amenities?.includes(amenity) ?? false;
  }

  imageUrl(path: string | null): string {
    return this.propService.imageUrl(path);
  }

  close(): void {
    this.dialogRef.close();
  }
}
