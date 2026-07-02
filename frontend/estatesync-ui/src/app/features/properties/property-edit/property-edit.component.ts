import {
  Component, ChangeDetectionStrategy, inject, signal, computed,
  effect, OnInit, DestroyRef, ElementRef, ViewChild
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap, forkJoin } from 'rxjs';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { IState, ICity } from 'country-state-city';
import { PropertyService } from '../../../core/services/property.service';
import { LocationService } from '../../../core/services/location.service';
import { PropertyDetailResponse, PropertyImageResponse, DocumentResponse } from '../../../core/models/property.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import * as L from 'leaflet';

const AMENITY_LIST = [
  'Parking', 'Swimming Pool', 'Gym', 'Power Backup',
  'Garden', 'Terrace', 'Security', 'Clubhouse',
  'Lift', 'CCTV', 'Play Area',
];

const normalize = (s: string) => s.toLowerCase().replace(/\s+/g, '');

@Component({
  selector: 'app-property-edit',
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
  templateUrl: './property-edit.component.html',
  styleUrl: './property-edit.component.css',
})
export class PropertyEditComponent implements OnInit {
  @ViewChild('mapEl') mapElRef!: ElementRef<HTMLDivElement>;

  private readonly route       = inject(ActivatedRoute);
  private readonly router      = inject(Router);
  public  readonly propSvc     = inject(PropertyService);
  private readonly locSvc      = inject(LocationService);
  private readonly fb          = inject(FormBuilder);
  private readonly snack       = inject(MatSnackBar);
  private readonly destroyRef  = inject(DestroyRef);

  readonly property        = signal<PropertyDetailResponse | null>(null);
  readonly images          = signal<PropertyImageResponse[]>([]);
  readonly loading         = signal(true);
  readonly saving          = signal(false);
  readonly submitting      = signal(false);
  readonly ownershipDoc    = signal<DocumentResponse | null>(null);
  readonly uploadingImages = signal(false);
  readonly deletingImageId = signal<string | null>(null);
  readonly uploadingDoc    = signal(false);

  // Stepper — active only for Draft status
  readonly step       = signal<1 | 2 | 3>(1);
  readonly stepLabels = ['Property Details', 'Media & Documents', 'Review & Submit'];

  readonly isDraft = computed(() => this.property()?.status === 'Draft');

  readonly canProceedToReview = computed(() =>
    this.images().length > 0 && !!this.ownershipDoc()
  );

  // Location cascades — synchronous
  readonly countries    = this.locSvc.getCountries();
  readonly states       = signal<IState[]>([]);
  readonly cities       = signal<ICity[]>([]);
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

  readonly amenityList       = AMENITY_LIST;
  readonly selectedAmenities = signal<Set<string>>(new Set());

  // Non-draft edit permissions
  readonly canEdit = computed(() => {
    const s = this.property()?.status;
    return s === 'Draft' || s === 'Rejected' || s === 'Active' || s === 'Leased' || s === 'AvailableSoon';
  });
  readonly canSubmit = computed(() => {
    const s = this.property()?.status;
    return s === 'Draft' || s === 'Rejected';
  });
  readonly submitReady = computed(() =>
    this.images().length > 0 && !!this.ownershipDoc()
  );

  readonly lat = signal<number | null>(null);
  readonly lng = signal<number | null>(null);

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
    landArea:        [null as number | null],
    buildupArea:     [null as number | null],
    monthlyRent:     [0,  [Validators.required, Validators.min(1)]],
    securityDeposit: [0,  [Validators.required, Validators.min(0)]],
    latInput:        [null as number | null],
    lngInput:        [null as number | null],
  });

  constructor() {
    effect(() => {
      const step   = this.step();
      const loaded = !this.loading() && !!this.property();
      const draft  = this.isDraft();

      if (draft) {
        // Stepper mode: map only lives on step 1
        if (loaded && step === 1) {
          queueMicrotask(() => this.initMap());
        } else {
          this.map?.remove();
          this.map    = undefined;
          this.marker = undefined;
        }
      } else {
        // Long-form mode: init map once data is ready
        if (loaded) {
          queueMicrotask(() => this.initMap());
        }
      }
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.states.set(this.locSvc.getStates('IN'));

    // Load property and documents in parallel so initial step can be determined atomically
    forkJoin({
      propRes: this.propSvc.getProperty(id),
      docsRes: this.propSvc.getDocuments(id),
    })
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: ({ propRes, docsRes }) => {
        const p = propRes.data!;
        this.property.set(p);

        const allStates    = this.locSvc.getStates('IN');
        const matchedState = allStates.find(s => s.name === p.state);
        const sc           = matchedState?.isoCode ?? '';
        if (sc) {
          this.stateCode.set(sc);
          this.cities.set(this.locSvc.getCities('IN', sc));
        }
        this.stateDisplay.set(p.state);
        this.cityDisplay.set(p.city);

        this.form.patchValue({
          title: p.title,
          description: p.description,
          propertyType: p.propertyType,
          countryCode: 'IN',
          state: p.state,
          city: p.city,
          pincode: p.pincode,
          addressLine: p.addressLine,
          bedrooms: p.bedrooms,
          bathrooms: p.bathrooms,
          landArea: p.landArea,
          buildupArea: p.buildupArea,
          monthlyRent: p.monthlyRent,
          securityDeposit: p.securityDeposit,
          latInput: p.latitude,
          lngInput: p.longitude,
        });

        const loadedKeys = new Set((p.amenities ?? []).map(a => normalize(a)));
        this.selectedAmenities.set(
          new Set<string>(AMENITY_LIST.filter(name => loadedKeys.has(normalize(name))))
        );
        this.lat.set(p.latitude);
        this.lng.set(p.longitude);

        this.images.set(p.images ?? []);

        const doc = (docsRes.data ?? []).find(d => d.documentType === 'OwnershipCertificate') ?? null;
        this.ownershipDoc.set(doc);

        // Determine starting step for draft BEFORE setting loading=false
        // (so the map effect fires with the correct step already applied)
        if (p.status === 'Draft') {
          const hasImages = (p.images ?? []).length > 0;
          const hasDoc    = !!doc;
          this.step.set(hasImages && hasDoc ? 3 : 2);
        }

        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/owner/dashboard'], { queryParams: { tab: 'properties' } });
      },
    });
  }

  // ── Draft stepper: Step 1 → Step 2 ──────────────────────────────────────

  saveDetails(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const id = this.property()!.propertyId;
    const v  = this.form.getRawValue();
    this.saving.set(true);

    const updateReq = {
      title: v.title, description: v.description, propertyType: v.propertyType,
      state: v.state, city: v.city, pincode: v.pincode, addressLine: v.addressLine,
      bedrooms: v.bedrooms, bathrooms: v.bathrooms,
      landArea: v.landArea ?? undefined, buildupArea: v.buildupArea ?? undefined,
      monthlyRent: v.monthlyRent, securityDeposit: v.securityDeposit,
      latitude: this.lat() ?? undefined, longitude: this.lng() ?? undefined,
    };

    forkJoin([
      this.propSvc.updateProperty(id, updateReq),
      this.propSvc.updateAmenities(id, [...this.selectedAmenities()]),
    ])
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe({
      next: () => {
        // property() keeps its initial-load state (images intact); just advance
        this.saving.set(false);
        this.step.set(2);
      },
      error: () => {
        this.saving.set(false);
        this.snack.open('Failed to save changes.', 'OK', { duration: 4000 });
      },
    });
  }

  // ── Draft stepper: Step 3 submit ────────────────────────────────────────

  submitForApproval(): void {
    const id = this.property()!.propertyId;
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

  // ── Non-draft long-form save ─────────────────────────────────────────────

  save(andSubmit = false): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    const id = this.property()!.propertyId;
    const v  = this.form.getRawValue();

    const updateReq = {
      title: v.title, description: v.description, propertyType: v.propertyType,
      state: v.state, city: v.city, pincode: v.pincode, addressLine: v.addressLine,
      bedrooms: v.bedrooms, bathrooms: v.bathrooms,
      landArea: v.landArea ?? undefined, buildupArea: v.buildupArea ?? undefined,
      monthlyRent: v.monthlyRent, securityDeposit: v.securityDeposit,
      latitude: this.lat() ?? undefined, longitude: this.lng() ?? undefined,
    };

    andSubmit ? this.submitting.set(true) : this.saving.set(true);

    forkJoin([
      this.propSvc.updateProperty(id, updateReq),
      this.propSvc.updateAmenities(id, [...this.selectedAmenities()]),
    ])
    .pipe(
      switchMap(([res]) => {
        this.property.set(res.data!);
        if (andSubmit) return this.propSvc.submitForApproval(id);
        return [null];
      }),
      takeUntilDestroyed(this.destroyRef),
    )
    .subscribe({
      next: () => {
        this.saving.set(false);
        this.submitting.set(false);
        this.snack.open(
          andSubmit ? 'Submitted for admin review!' : 'Changes saved.',
          'OK', { duration: 3000 }
        );
        if (andSubmit) {
          this.router.navigate(['/owner/dashboard'], { queryParams: { tab: 'properties' } });
        }
      },
      error: () => {
        this.saving.set(false);
        this.submitting.set(false);
        this.snack.open('Failed to save changes.', 'OK', { duration: 4000 });
      },
    });
  }

  // ── Location handlers ────────────────────────────────────────────────────

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

  // ── Amenities ────────────────────────────────────────────────────────────

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

  // ── Map ──────────────────────────────────────────────────────────────────

  onLatLngInput(): void {
    const lat = this.form.controls.latInput.value;
    const lng = this.form.controls.lngInput.value;
    if (lat !== null && lng !== null && !isNaN(lat) && !isNaN(lng)) {
      this.lat.set(lat);
      this.lng.set(lng);
      this.placeMarker(lat, lng);
    }
  }

  // ── Images ───────────────────────────────────────────────────────────────

  onImagesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const files = Array.from(input.files);
    const id    = this.property()!.propertyId;
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
    const id = this.property()!.propertyId;
    this.deletingImageId.set(imageId);
    this.propSvc.deleteImage(id, imageId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.images.update(imgs => imgs.filter(i => i.imageId !== imageId));
          this.deletingImageId.set(null);
        },
        error: () => {
          this.deletingImageId.set(null);
          this.snack.open('Failed to delete image.', 'OK', { duration: 3000 });
        },
      });
  }

  // ── Documents ────────────────────────────────────────────────────────────

  onDocSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.[0]) return;
    const file = input.files[0];
    const id   = this.property()!.propertyId;
    this.uploadingDoc.set(true);
    this.propSvc.uploadDocument(id, file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.ownershipDoc.set(res.data ?? null); this.uploadingDoc.set(false); },
        error: (err) => {
          this.uploadingDoc.set(false);
          const msg = err?.error?.detail ?? err?.error?.message ?? 'Document upload failed. Must be PDF/JPG/PNG under 10 MB.';
          this.snack.open(msg, 'OK', { duration: 4000 });
        },
      });
    input.value = '';
  }

  // ── Private helpers ──────────────────────────────────────────────────────

  private refreshProperty(id: string, onDone: () => void): void {
    this.propSvc.getProperty(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: r => {
          this.property.set(r.data!);
          this.images.set(r.data!.images ?? []);
          onDone();
        },
        error: onDone,
      });
  }

  private initMap(): void {
    // Guard: in stepper mode the map only belongs on step 1
    if (this.isDraft() && this.step() !== 1) return;
    if (!this.mapElRef?.nativeElement || this.map) return;

    const defaultLat = this.lat() ?? 13.0827;
    const defaultLng = this.lng() ?? 80.2707;

    this.map = L.map(this.mapElRef.nativeElement, { zoomControl: true }).setView(
      [defaultLat, defaultLng], this.lat() ? 13 : 7
    );
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
