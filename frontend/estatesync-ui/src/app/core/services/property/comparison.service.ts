import { Injectable, signal, computed } from '@angular/core';
import { PropertySummaryResponse } from '../../models/property.model';

@Injectable({ providedIn: 'root' })
export class ComparisonService {
  // Store properties checked for comparison in a reactive signal
  readonly selectedProperties = signal<PropertySummaryResponse[]>([]);
  
  // Controls if checkboxes are displayed on the cards
  readonly isCompareMode = signal<boolean>(false);

  readonly selectedCount = computed(() => this.selectedProperties().length);
  readonly isFull = computed(() => this.selectedCount() >= 3);

  toggleProperty(property: PropertySummaryResponse): void {
    this.selectedProperties.update(list => {
      const index = list.findIndex(p => p.propertyId === property.propertyId);
      if (index > -1) {
        return list.filter(p => p.propertyId !== property.propertyId);
      }
      if (list.length < 3) {
        return [...list, property];
      }
      return list;
    });
  }

  isSelected(propertyId: string): boolean {
    return this.selectedProperties().some(p => p.propertyId === propertyId);
  }

  clear(): void {
    this.selectedProperties.set([]);
  }
}
