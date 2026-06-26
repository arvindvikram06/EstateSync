import { Component, ChangeDetectionStrategy, inject, input, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { PropertySummaryResponse } from '../../../core/models/property.model';
import { PropertyService } from '../../../core/services/property.service';

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
  templateUrl: './property-card.component.html',
  styleUrl: './property-card.component.css'
})
export class PropertyCardComponent {
  readonly property = input.required<PropertySummaryResponse>();
  readonly propertyService = inject(PropertyService);

  readonly gradient = computed(() =>
    GRADIENTS[hashCode(this.property().propertyId) % GRADIENTS.length]
  );

  formatRent(amount: number): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(amount);
  }
}
