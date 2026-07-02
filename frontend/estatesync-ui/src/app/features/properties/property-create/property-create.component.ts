import {
  Component, ChangeDetectionStrategy, inject, signal, computed,
  effect, OnInit, DestroyRef, ElementRef, ViewChild
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ICountry, IState, ICity } from 'country-state-city';
import { PropertyService } from '../../../core/services/property.service';
import { LocationService } from '../../../core/services/location.service';
import { PropertyDetailResponse, DocumentResponse } from '../../../core/models/property.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import * as L from 'leaflet';

const AMENITY_LIST = [
  'Parking', 'Swimming Pool', 'Gym', 'Power Backup',
  'Garden', 'Terrace', 'Security', 'Clubhouse',
  'Lift', 'CCTV', 'Play Area',
];

const normalize = (s: string) => s.toLowerCase().replace(/\s+/g, '');

@Component({
  selector: 'app-property-create',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatAutocompleteModule, MatProgressSpinnerModule,
    MatProgressBarModule, MatSnackBarModule, DecimalPipe, DatePipe,
    StatusBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './property-create.component.html',
  styleUrl: './property-create.component.css',
})
export class PropertyCreateComponent implements OnInit {
  @ViewChild('mapEl') mapElRef!: ElementRef<HTMLDivElement>;

  private readonly router     = inject(Router);
  readonly propSvc            = inject(PropertyService);
  private readonly locSvc     = inject(LocationService);
  private readonly fb         = inject(FormBuilder);
  private readonly snack      = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  readonly step        = signal<1 | 2 | 3>(1);
  readonly saving      = signal(false);
  readonly submitting  = signal(false);
  readonly createdProperty = signal<PropertyDetailResponse | null>(null);
  readonly ownershipDoc    = signal<DocumentResponse | null>(null);
  readonly uploadingImages = signal(false);
  readonly deletingImageId = signal<string | null>(null);
  readonly uploadingDoc    = signal(false);

  readonly amenityList = AMENITY_LIST;
  readonly selectedAmenities = signal<Set<string>>(new Set());

  readonly lat = signal<number | null>(null);
  readonly lng = signal<number | null>(null);

  readonly stepLabels = ['Property Details', 'Media & Documents', 'Review & Submit'];

  readonly canProceedToReview = computed(() => {
    const p = this.createdProperty();
    return !!(p && p.images.length > 0 && this.ownershipDoc());
  });

  // Location cascades — synchronous, no API calls
  readonly countries = this.locSvc.getCountries();
  readonly states    = signal<IState[]>([]);
  readonly cities    = signal<ICity[]>([]);
  readonly stateCode    = signal('');
  readonly stateDisplay = signal('');
  readonly cityDisplay  = signal('');

  readonly filteredStates = computed(() => {
    const q = this.stateDisplay().toLowerCase();
    return !q ? this.states() : this.states().filter(s => s.name.toLowerCase().includes(q));
  });
  readonly filteredCities = computed(() => {
    const q = this.cityDisplay().toLowerCase();
    return !q ? this.cities() : this.cities().filter(c => c.name.toLowerCase().includes(q));
  });

  private map?: L.Map;
  private marker?: L.Marker;

  readonly form = this.fb.nonNullable.group({
    title:           ['', Validators.required],
    description:     ['', Validators.required],
    propertyType:    ['IndependentHouse', Validators.required],
    countryCode:     ['IN', Validators.required],
    state:           ['', Validators.required],
    city:            ['', Validators.required],
    pincode:         ['', [Validators.required, Validators.pattern(/^[1-9][0-9]{5}$/)]],
    addressLine:     ['', Validators.required],
    bedrooms:        [1,  [Validators.required, Validators.min(1)]],
    bathrooms:       [1,  [Validators.required, Validators.min(1)]],
    landArea:        [0,  [Validators.required, Validators.min(1)]],
    buildupArea:     [0,  [Validators.required, Validators.min(1)]],
    monthlyRent:     [0,  [Validators.required, Validators.min(1)]],
    securityDeposit: [0,  [Validators.required, Validators.min(0)]],
    latInput:        [null as number | null, [Validators.required, Validators.min(-90), Validators.max(90)]],
    lngInput:        [null as number | null, [Validators.required, Validators.min(-180), Validators.max(180)]],
  });

  constructor() {
    effect(() => {
      const s = this.step();
      if (s === 1) {
        queueMicrotask(() => this.initMap());
      } else {
        this.map?.remove();
        this.map = undefined;
        this.marker = undefined;
      }
    });
  }

  ngOnInit(): void {
    // Pre-load India states
    this.states.set(this.locSvc.getStates('IN'));
  }

  onCountryChange(countryCode: string): void {
    this.states.set(this.locSvc.getStates(countryCode));
    this.cities.set([]);
    this.stateCode.set('');
    this.stateDisplay.set('');
    this.cityDisplay.set('');
    this.form.patchValue({ state: '', city: '' });
  }

  onStateInput(value: string): void {
    this.stateDisplay.set(value);
    if (value !== this.form.controls.state.value) {
      this.form.patchValue({ state: '', city: '' });
      this.stateCode.set('');
      this.cities.set([]);
      this.cityDisplay.set('');
    }
  }

  onStateSelected(state: IState): void {
    this.stateCode.set(state.isoCode);
    this.stateDisplay.set(state.name);
    this.form.patchValue({ state: state.name, city: '' });
    this.cityDisplay.set('');
    this.cities.set(this.locSvc.getCities(this.form.controls.countryCode.value, state.isoCode));
  }

  onCityInput(value: string): void {
    this.cityDisplay.set(value);
    if (value !== this.form.controls.city.value) {
      this.form.patchValue({ city: '' });
    }
  }

  onCitySelected(city: ICity): void {
    this.cityDisplay.set(city.name);
    this.form.patchValue({ city: city.name });
  }

  isSelected(name: string): boolean {
    return [...this.selectedAmenities()].some(a => normalize(a) === normalize(name));
  }

  toggleAmenity(name: string): void {
    const key = normalize(name);
    this.selectedAmenities.update(set => {
      const next = new Set(set);
      const existing = [...next].find(a => normalize(a) === key);
      existing ? next.delete(existing) : next.add(name);
      return next;
    });
  }

  onLatLngInput(): void {
    const lat = this.form.controls.latInput.value;
    const lng = this.form.controls.lngInput.value;
    if (lat !== null && lng !== null && !isNaN(lat) && !isNaN(lng)) {
      this.lat.set(lat);
      this.lng.set(lng);
      this.placeMarker(lat, lng);
    }
  }

  saveDetails(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const v = this.form.getRawValue();
    this.saving.set(true);

    this.propSvc.createProperty({
      title: v.title,
      description: v.description,
      propertyType: v.propertyType,
      state: v.state,
      city: v.city,
      pincode: v.pincode,
      addressLine: v.addressLine,
      bedrooms: v.bedrooms,
      bathrooms: v.bathrooms,
      landArea: v.landArea,
      buildupArea: v.buildupArea,
      monthlyRent: v.monthlyRent,
      securityDeposit: v.securityDeposit,
      latitude: v.latInput!,
      longitude: v.lngInput!,
    }).pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.createdProperty.set(res.data!);
          if (this.selectedAmenities().size > 0) {
            this.propSvc.updateAmenities(res.data!.propertyId, [...this.selectedAmenities()])
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe();
          }
          this.saving.set(false);
          this.step.set(2);
        },
        error: () => {
          this.saving.set(false);
          this.snack.open('Failed to save property. Check all fields.', 'OK', { duration: 4000 });
        },
      });
  }

  onImagesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const files = Array.from(input.files);
    const id = this.createdProperty()!.propertyId;
    this.uploadingImages.set(true);
    this.propSvc.uploadImages(id, files)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.refreshProperty(id, () => this.uploadingImages.set(false)),
        error: () => {
          this.uploadingImages.set(false);
          this.snack.open('Image upload failed.', 'OK', { duration: 3000 });
        },
      });
    input.value = '';
  }

  deleteImage(imageId: string): void {
    const id = this.createdProperty()!.propertyId;
    this.deletingImageId.set(imageId);
    this.propSvc.deleteImage(id, imageId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.createdProperty.update(p => p ? {
            ...p, images: p.images.filter(i => i.imageId !== imageId)
          } : p);
          this.deletingImageId.set(null);
        },
        error: () => {
          this.deletingImageId.set(null);
          this.snack.open('Failed to delete image.', 'OK', { duration: 3000 });
        },
      });
  }

  onDocSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.[0]) return;
    const file = input.files[0];
    const id = this.createdProperty()!.propertyId;
    this.uploadingDoc.set(true);
    this.propSvc.uploadDocument(id, file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.ownershipDoc.set(res.data ?? null);
          this.uploadingDoc.set(false);
        },
        error: () => {
          this.uploadingDoc.set(false);
          this.snack.open('Document upload failed. Must be PDF/JPG/PNG under 10 MB.', 'OK', { duration: 4000 });
        },
      });
    input.value = '';
  }

  submitForApproval(): void {
    const id = this.createdProperty()!.propertyId;
    this.submitting.set(true);
    this.propSvc.submitForApproval(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.snack.open('Submitted for admin review!', 'OK', { duration: 3000 });
          this.router.navigate(['/owner/dashboard'], { queryParams: { tab: 'properties' } });
        },
        error: () => {
          this.submitting.set(false);
          this.snack.open('Submission failed.', 'OK', { duration: 4000 });
        },
      });
  }

  saveDraft(): void {
    this.router.navigate(['/owner/dashboard'], { queryParams: { tab: 'properties' } });
  }

  getStepIndex(): number { return this.step(); }

  private refreshProperty(id: string, onDone: () => void): void {
    this.propSvc.getProperty(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: r => { this.createdProperty.set(r.data!); onDone(); }, error: onDone });
  }

  private initMap(): void {
    if (!this.mapElRef?.nativeElement || this.map) return;

    const lat = this.lat() ?? 20.5937;
    const lng = this.lng() ?? 78.9629;

    this.map = L.map(this.mapElRef.nativeElement, { zoomControl: true }).setView([lat, lng], this.lat() ? 13 : 5);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(this.map);

    if (this.lat() && this.lng()) this.placeMarker(this.lat(), this.lng());

    this.map.on('click', (e: L.LeafletMouseEvent) => {
      const lt = parseFloat(e.latlng.lat.toFixed(6));
      const ln = parseFloat(e.latlng.lng.toFixed(6));
      this.lat.set(lt);
      this.lng.set(ln);
      this.form.patchValue({ latInput: lt, lngInput: ln });
      this.placeMarker(lt, ln);
    });
  }

  private placeMarker(lat: number | null, lng: number | null): void {
    if (!this.map || lat === null || lng === null) return;
    const icon = L.divIcon({
      className: '',
      html: `<div style="width:18px;height:18px;background:var(--mat-sys-primary);border:2px solid white;border-radius:50%;box-shadow:0 2px 6px rgba(0,0,0,0.3)"></div>`,
      iconSize: [18, 18], iconAnchor: [9, 9],
    });
    if (this.marker) {
      this.marker.setLatLng([lat, lng]);
    } else {
      this.marker = L.marker([lat, lng], { icon }).addTo(this.map!);
    }
    this.map.setView([lat, lng], Math.max(this.map.getZoom(), 12));
  }
}
