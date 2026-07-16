import { Component, ChangeDetectionStrategy, inject, input, computed, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { PropertySummaryResponse } from '../../../core/models/property.model';
import { PropertyService } from '@core/services/property/property.service';
import { SavedPropertyService } from '@core/services/property/saved-property.service';
import { AuthService } from '@core/services/auth/auth.service';
import { ComparisonService } from '@core/services/property/comparison.service';
import { formatCurrency } from '../../../core/utils/format.utils';

const GRADIENTS = [
  'linear-gradient(135deg, #52B69A 0%, #1A759F 100%)',
  'linear-gradient(135deg, #E9C46A 0%, #E76F51 100%)',
  'linear-gradient(135deg, #9B5DE5 0%, #6A0DAD 100%)',
  'linear-gradient(135deg, #4CC9A0 0%, #2D6A4F 100%)',
  'linear-gradient(135deg, #E77BA4 0%, #B5446E 100%)',
  'linear-gradient(135deg, #74B3CE 0%, #1B4F72 100%)',
  'linear-gradient(135deg, #F4A261 0%, #E76F51 100%)',
  'linear-gradient(135deg, #8ECAE6 0%, #219EBC 100%)',
];

function hashCode(s: string): number {
  let h = 0;
  for (let i = 0; i < s.length; i++) { h = (Math.imul(31, h) + s.charCodeAt(i)) | 0; }
  return Math.abs(h);
}

@Component({
  selector: 'app-property-card',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatChipsModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './property-card.component.html',})
export class PropertyCardComponent implements OnInit {
  readonly property = input.required<PropertySummaryResponse>();
  readonly propertyService = inject(PropertyService);
  readonly comparisonService = inject(ComparisonService);
  private readonly savedPropertyService = inject(SavedPropertyService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly gradient = computed(() =>
    GRADIENTS[hashCode(this.property().propertyId) % GRADIENTS.length]
  );

  readonly isSaved = computed(() =>
    this.savedPropertyService.savedIds().has(this.property().propertyId)
  );

  readonly isSelected = computed(() => this.comparisonService.isSelected(this.property().propertyId));
  readonly canSelect = computed(() => !this.comparisonService.isFull() || this.isSelected());

  readonly formatRent = formatCurrency;

  ngOnInit(): void {
    if (this.authService.isLoggedIn()) this.savedPropertyService.ensureIdsLoaded();
  }

  toggleSave(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (!this.authService.isLoggedIn()) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.savedPropertyService.toggle(this.property().propertyId);
  }

  toggleCompare(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.comparisonService.toggleProperty(this.property());
  }
}
