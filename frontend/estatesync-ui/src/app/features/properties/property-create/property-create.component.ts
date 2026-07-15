import {
  Component, ChangeDetectionStrategy, inject, signal, computed,
  effect, DestroyRef, ElementRef, ViewChild, OnInit, NgZone,
} from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { Subject } from 'rxjs';
import { debounceTime, map, startWith } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocomplete, MatAutocompleteTrigger } from '@angular/material/autocomplete';
import { MatOption } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DecimalPipe, DatePipe } from '@angular/common';
import { IState, ICity } from 'country-state-city';
import { PropertyService } from '@core/services/property/property.service';
import { LocationService } from '@core/services/shared/location.service';
import { GeocodingService } from '@core/services/shared/geocoding.service';
import {
  PropertyDetailResponse, DocumentResponse, PatchPropertyDraftRequest,
} from '../../../core/models/property.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import * as L from 'leaflet';

const AMENITY_LIST = [
  'Parking', 'Swimming Pool', 'Gym', 'Power Backup',
  'Garden', 'Terrace', 'Security', 'Clubhouse',
  'Lift', 'CCTV', 'Play Area',
];

const TOTAL_STEPS = 10;
const STORAGE_PROPERTY_ID = 'es_wizard_property_id';
const STORAGE_STEP        = 'es_wizard_step';

@Component({
  selector: 'app-property-create',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatAutocomplete, MatAutocompleteTrigger, MatOption, MatProgressSpinnerModule,
    MatProgressBarModule, MatSnackBarModule, DecimalPipe, DatePipe,
    StatusBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './property-create.component.html',
  styleUrl: './property-create.component.css',
})
export class PropertyCreateComponent implements OnInit {
  @ViewChild('mapEl') mapElRef?: ElementRef<HTMLDivElement>;

  private readonly router       = inject(Router);
  readonly propSvc              = inject(PropertyService);
  private readonly locSvc       = inject(LocationService);
  private readonly geocodingSvc = inject(GeocodingService);
  private readonly fb           = inject(FormBuilder);
  private readonly snack        = inject(MatSnackBar);
  private readonly destroyRef   = inject(DestroyRef);
  private readonly zone         = inject(NgZone);

  readonly TOTAL_STEPS = TOTAL_STEPS;
  readonly amenityList = AMENITY_LIST;

  // ── Step navigation ───────────────────────────────────────────────
  readonly step            = signal<number>(1);
  readonly progressPct     = computed(() => Math.round((this.step() / TOTAL_STEPS) * 100));
  readonly showExitConfirm = signal(false);

  // ── UI state ──────────────────────────────────────────────────────
  readonly patching      = signal(false);  // PATCH in flight on step advance
  readonly draftRestored = signal(false);  // shows resume banner

  // ── Step 1: Property type ─────────────────────────────────────────
  readonly propertyType = signal<'IndependentHouse' | 'Villa' | null>(null);

  // ── Step 2: Location ─────────────────────────────────────────────
  readonly states       = signal<IState[]>([]);
  readonly cities       = signal<ICity[]>([]);
  readonly stateCode    = signal('');
  readonly stateDisplay = signal('');
  readonly cityDisplay  = signal('');

  readonly locationForm = this.fb.nonNullable.group({
    state:       ['', Validators.required],
    city:        ['', Validators.required],
    pincode:     ['', [Validators.required, Validators.pattern(/^[1-9][0-9]{5}$/)]],
    addressLine: ['', Validators.required],
  });
  private readonly locationValid$ = toSignal(
    this.locationForm.statusChanges.pipe(map(s => s === 'VALID'), startWith(false)),
    { initialValue: false }
  );

  readonly filteredStates = computed(() => {
    const q = this.stateDisplay().toLowerCase();
    return !q ? this.states() : this.states().filter(s => s.name.toLowerCase().includes(q));
  });
  readonly filteredCities = computed(() => {
    const q = this.cityDisplay().toLowerCase();
    return !q ? this.cities() : this.cities().filter(c => c.name.toLowerCase().includes(q));
  });

  // ── Step 3: Map pin ───────────────────────────────────────────────
  readonly lat             = signal<number | null>(null);
  readonly lng             = signal<number | null>(null);
  readonly geocodedAddress = signal<string>('');
  private map?:    L.Map;
  private marker?: L.Marker;
  private readonly dragEnd$ = new Subject<L.LatLng>();

  // ── Step 4: Rooms ─────────────────────────────────────────────────
  readonly bedrooms  = signal<number>(1);
  readonly bathrooms = signal<number>(1);

  // ── Step 5: Area ─────────────────────────────────────────────────
  readonly areaForm = this.fb.nonNullable.group({
    landArea:    [null as number | null, [Validators.required, Validators.min(1)]],
    buildupArea: [null as number | null, [Validators.required, Validators.min(1)]],
  });
  private readonly areaValid$ = toSignal(
    this.areaForm.statusChanges.pipe(map(s => s === 'VALID'), startWith(false)),
    { initialValue: false }
  );

  // ── Step 6: Title ────────────────────────────────────────────────
  readonly titleForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(100)]],
  });
  private readonly titleValid$ = toSignal(
    this.titleForm.statusChanges.pipe(map(s => s === 'VALID'), startWith(false)),
    { initialValue: false }
  );

  // ── Step 7: Description ──────────────────────────────────────────
  readonly descForm = this.fb.nonNullable.group({
    description: ['', [Validators.required, Validators.minLength(20), Validators.maxLength(2000)]],
  });
  private readonly descValid$ = toSignal(
    this.descForm.statusChanges.pipe(map(s => s === 'VALID'), startWith(false)),
    { initialValue: false }
  );

  // ── Step 8: Pricing ───────────────────────────────────────────────
  readonly pricingForm = this.fb.nonNullable.group({
    monthlyRent:     [null as number | null, [Validators.required, Validators.min(1)]],
    securityDeposit: [null as number | null, [Validators.required, Validators.min(0)]],
  });
  private readonly pricingValid$ = toSignal(
    this.pricingForm.statusChanges.pipe(map(s => s === 'VALID'), startWith(false)),
    { initialValue: false }
  );

  // ── Step 9: Amenities ────────────────────────────────────────────
  readonly selectedAmenities = signal<Set<string>>(new Set());

  // ── Step 10: Photos, Documents & Submit ──────────────────────────
  readonly creatingDraft   = signal(false);  // loading spinner (restore from API or step 1 create)
  readonly createdProperty = signal<PropertyDetailResponse | null>(null);
  readonly ownershipDoc    = signal<DocumentResponse | null>(null);
  readonly uploadingImages = signal(false);
  readonly deletingImageId = signal<string | null>(null);
  readonly uploadingDoc    = signal(false);
  readonly submitting      = signal(false);

  readonly canSubmit = computed(() => {
    const p = this.createdProperty();
    return !!(p && p.images.length > 0 && this.ownershipDoc());
  });

  // ── Per-step "Next" gate ─────────────────────────────────────────
  readonly stepValid = computed(() => {
    switch (this.step()) {
      case 1:  return this.propertyType() !== null;
      case 2:  return this.locationValid$();
      case 3:  return true;
      case 4:  return this.bedrooms() >= 1 && this.bathrooms() >= 1;
      case 5:  return this.areaValid$();
      case 6:  return this.titleValid$();
      case 7:  return this.descValid$();
      case 8:  return this.pricingValid$();
      case 9:  return true;
      case 10: return this.canSubmit();
      default: return false;
    }
  });

  constructor() {
    effect(() => {
      if (this.step() === 3) {
        queueMicrotask(() => this.initMapStep());
      } else {
        this.destroyMap();
      }
    });

    this.dragEnd$.pipe(
      debounceTime(600),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(pos => this.doReverseGeocode(pos.lat, pos.lng));
  }

  ngOnInit(): void {
    this.states.set(this.locSvc.getStates('IN'));

    const savedId = localStorage.getItem(STORAGE_PROPERTY_ID);
    if (savedId) {
      // Same device: we know the exact draft ID — load it directly.
      // If STORAGE_STEP is absent (e.g., arriving via Edit button), pass null so
      // the wizard infers the step from the property data instead of defaulting to 2.
      const rawStep = localStorage.getItem(STORAGE_STEP);
      this.loadDraftById(savedId, rawStep ? parseInt(rawStep, 10) : null);
    } else {
      // Different device or cleared storage: ask the API for the most recent draft
      this.creatingDraft.set(true);
      this.propSvc.getMyActiveDraft()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: res => {
            const p = res.data;
            if (p) {
              // Seed localStorage so same-device navigation works from here on
              localStorage.setItem(STORAGE_PROPERTY_ID, p.propertyId);
              // Step is unknown on a foreign device — resume at the last meaningful step
              const resumeStep = this.inferStepFromProperty(p);
              localStorage.setItem(STORAGE_STEP, resumeStep.toString());
              this.applyRestoredProperty(p, resumeStep);
            }
            this.creatingDraft.set(false);
          },
          error: () => {
            // No draft on server — start fresh (no-op)
            this.creatingDraft.set(false);
          },
        });
    }
  }

  private loadDraftById(id: string, savedStep: number | null): void {
    this.creatingDraft.set(true);
    this.propSvc.getProperty(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          const p = res.data;
          if (p && p.status === 'Draft') {
            // null means we have no saved step (e.g., arrived from Edit button) — infer it
            const targetStep = savedStep ?? this.inferStepFromProperty(p);
            this.applyRestoredProperty(p, targetStep);
          } else {
            // Property no longer a Draft (submitted/deleted) — clear stale keys
            this.clearWizardStorage();
          }
          this.creatingDraft.set(false);
        },
        error: () => {
          this.clearWizardStorage();
          this.creatingDraft.set(false);
        },
      });
  }

  private applyRestoredProperty(p: PropertyDetailResponse, targetStep: number): void {
    this.restoreFromProperty(p);
    const clampedStep = Math.max(2, Math.min(targetStep, TOTAL_STEPS));
    this.step.set(clampedStep);
    this.draftRestored.set(true);

    if (clampedStep >= 10) {
      this.loadOwnershipDoc(p.propertyId);
    }
  }

  /**
   * Infers the furthest completed wizard step from a draft returned by the API,
   * used when step is unknown (cross-device restore). Steps are evaluated in
   * reverse so the user lands as far along as their data allows.
   */
  private inferStepFromProperty(p: PropertyDetailResponse): number {
    if (p.amenities?.length)                           return 10; // past amenities → upload step
    if (p.securityDeposit > 0 || p.monthlyRent > 0)   return 9;  // pricing done
    if (p.description?.trim())                         return 8;  // description done
    if (p.title?.trim())                               return 7;  // title done
    if ((p.buildupArea ?? 0) > 0)                      return 6;  // area done
    if (p.bedrooms > 0 || p.bathrooms > 0)             return 5;  // rooms done
    if (p.latitude != null || p.longitude != null)     return 4;  // map done
    if (p.city?.trim())                                return 3;  // location done
    return 2;
  }

  // ── Navigation ────────────────────────────────────────────────────

  nextStep(): void {
    if (!this.stepValid()) { this.touchCurrentStep(); return; }
    if (this.step() >= TOTAL_STEPS) return;

    const currentStep = this.step();

    if (currentStep === 1) {
      // Step 1 → 2: create the backend draft
      this.creatingDraft.set(true);
      this.propSvc.createWizardDraft({ propertyType: this.propertyType()! })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: res => {
            this.createdProperty.set(res.data!);
            localStorage.setItem(STORAGE_PROPERTY_ID, res.data!.propertyId);
            this.creatingDraft.set(false);
            this.step.set(2);
            localStorage.setItem(STORAGE_STEP, '2');
          },
          error: err => {
            this.creatingDraft.set(false);
            const msg = err.status === 403
              ? 'You must complete payment setup before listing a property.'
              : 'Failed to start listing. Please try again.';
            this.snack.open(msg, 'OK', { duration: 5000 });
          },
        });
      return;
    }

    // Steps 2–9: PATCH the draft, then advance
    const patch = this.buildPatchForStep(currentStep);
    const property = this.createdProperty();

    if (patch && property) {
      this.patching.set(true);
      this.propSvc.patchWizardDraft(property.propertyId, patch)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: res => {
            this.createdProperty.set(res.data!);
            this.patching.set(false);
            this.step.update(s => s + 1);
            localStorage.setItem(STORAGE_STEP, this.step().toString());
          },
          error: () => {
            this.patching.set(false);
            this.snack.open('Failed to save progress. Please try again.', 'OK', { duration: 4000 });
          },
        });
    } else {
      // No patch needed for this step (e.g., step 3 map skipped, step 9 no amenities)
      this.step.update(s => s + 1);
      localStorage.setItem(STORAGE_STEP, this.step().toString());
    }
  }

  prevStep(): void {
    if (this.step() > 1) this.step.update(s => s - 1);
  }

  saveAndExit(): void {
    const property = this.createdProperty();

    if (!property) {
      // Draft not started yet — nothing to save
      this.router.navigate(['/owner/dashboard'], { queryParams: { tab: 'properties' } });
      return;
    }

    // Persist the current step's data before leaving (fire-and-forget)
    const patch = this.buildPatchForStep(this.step());
    if (patch) {
      this.propSvc.patchWizardDraft(property.propertyId, patch)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe();
    }

    localStorage.setItem(STORAGE_STEP, this.step().toString());
    this.router.navigate(['/owner/dashboard'], { queryParams: { tab: 'properties' } });
  }

  exitWithoutSaving(): void {
    const property = this.createdProperty();
    this.clearWizardStorage();
    if (property) {
      this.propSvc.deleteProperty(property.propertyId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe();
    }
    this.router.navigate(['/owner/dashboard'], { queryParams: { tab: 'properties' } });
  }

  startOver(): void {
    this.clearWizardStorage();
    this.step.set(1);
    this.propertyType.set(null);
    this.bedrooms.set(1);
    this.bathrooms.set(1);
    this.lat.set(null);
    this.lng.set(null);
    this.geocodedAddress.set('');
    this.selectedAmenities.set(new Set());
    this.locationForm.reset();
    this.areaForm.reset();
    this.titleForm.reset();
    this.descForm.reset();
    this.pricingForm.reset();
    this.stateDisplay.set('');
    this.cityDisplay.set('');
    this.stateCode.set('');
    this.cities.set([]);
    this.createdProperty.set(null);
    this.ownershipDoc.set(null);
    this.draftRestored.set(false);
  }

  private touchCurrentStep(): void {
    switch (this.step()) {
      case 2: this.locationForm.markAllAsTouched(); break;
      case 5: this.areaForm.markAllAsTouched(); break;
      case 6: this.titleForm.markAllAsTouched(); break;
      case 7: this.descForm.markAllAsTouched(); break;
      case 8: this.pricingForm.markAllAsTouched(); break;
    }
  }

  // ── Draft persistence helpers ─────────────────────────────────────

  private buildPatchForStep(step: number): PatchPropertyDraftRequest | null {
    switch (step) {
      case 2: {
        const loc = this.locationForm.getRawValue();
        return { state: loc.state, city: loc.city, pincode: loc.pincode, addressLine: loc.addressLine };
      }
      case 3: {
        const lat = this.lat();
        const lng = this.lng();
        return (lat !== null && lng !== null) ? { latitude: lat, longitude: lng } : null;
      }
      case 4: return { bedrooms: this.bedrooms(), bathrooms: this.bathrooms() };
      case 5: {
        const area = this.areaForm.getRawValue();
        return {
          landArea:    area.landArea    ?? undefined,
          buildupArea: area.buildupArea ?? undefined,
        };
      }
      case 6: return { title: this.titleForm.controls.title.value };
      case 7: return { description: this.descForm.controls.description.value };
      case 8: {
        const p = this.pricingForm.getRawValue();
        return {
          monthlyRent:     p.monthlyRent     ?? undefined,
          securityDeposit: p.securityDeposit ?? undefined,
        };
      }
      case 9: return { amenities: [...this.selectedAmenities()] };
      default: return null;
    }
  }

  private restoreFromProperty(p: PropertyDetailResponse): void {
    this.createdProperty.set(p);
    this.propertyType.set(p.propertyType as 'IndependentHouse' | 'Villa');

    // Location
    if (p.state) {
      this.stateDisplay.set(p.state);
      this.locationForm.controls.state.setValue(p.state);
      const stateObj = this.locSvc.getStates('IN').find(s => s.name === p.state);
      if (stateObj) {
        this.stateCode.set(stateObj.isoCode);
        this.cities.set(this.locSvc.getCities('IN', stateObj.isoCode));
      }
    }
    if (p.city) {
      this.cityDisplay.set(p.city);
      this.locationForm.controls.city.setValue(p.city);
    }
    if (p.pincode) this.locationForm.controls.pincode.setValue(p.pincode);
    if (p.addressLine) this.locationForm.controls.addressLine.setValue(p.addressLine);

    // Coords
    if (p.latitude  != null) this.lat.set(Number(p.latitude));
    if (p.longitude != null) this.lng.set(Number(p.longitude));

    // Rooms (only restore if non-default)
    if (p.bedrooms  > 0) this.bedrooms.set(p.bedrooms);
    if (p.bathrooms > 0) this.bathrooms.set(p.bathrooms);

    // Area
    if ((p.landArea    ?? 0) > 0) this.areaForm.controls.landArea.setValue(p.landArea);
    if ((p.buildupArea ?? 0) > 0) this.areaForm.controls.buildupArea.setValue(p.buildupArea);

    // Title & Description (skip placeholder empty values from initial draft creation)
    if (p.title)       this.titleForm.controls.title.setValue(p.title);
    if (p.description) this.descForm.controls.description.setValue(p.description);

    // Pricing
    if (p.monthlyRent     > 0) this.pricingForm.controls.monthlyRent.setValue(p.monthlyRent);
    if (p.securityDeposit > 0) this.pricingForm.controls.securityDeposit.setValue(p.securityDeposit);

    // Amenities — stored normalised (lowercase, no spaces); match back to display names
    if (p.amenities?.length) {
      const restoredSet = new Set(
        p.amenities
          .map(stored => AMENITY_LIST.find(a => a.replace(/\s+/g, '').toLowerCase() === stored) ?? stored)
      );
      this.selectedAmenities.set(restoredSet);
    }
  }

  private loadOwnershipDoc(propertyId: string): void {
    this.propSvc.getDocuments(propertyId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          const doc = (res.data ?? []).find(d => d.documentType === 'OwnershipCertificate') ?? null;
          this.ownershipDoc.set(doc);
        },
      });
  }

  private clearWizardStorage(): void {
    localStorage.removeItem(STORAGE_PROPERTY_ID);
    localStorage.removeItem(STORAGE_STEP);
  }

  // ── Step 1 ────────────────────────────────────────────────────────

  selectType(type: 'IndependentHouse' | 'Villa'): void {
    this.propertyType.set(type);
  }

  // ── Step 2 ────────────────────────────────────────────────────────

  onStateInput(value: string): void {
    this.stateDisplay.set(value);
    if (value !== this.locationForm.controls.state.value) {
      this.locationForm.controls.state.setValue('');
      this.locationForm.controls.city.setValue('');
      this.stateCode.set('');
      this.cities.set([]);
      this.cityDisplay.set('');
    }
  }

  onStateSelected(state: IState): void {
    this.stateCode.set(state.isoCode);
    this.stateDisplay.set(state.name);
    this.locationForm.controls.state.setValue(state.name);
    this.locationForm.controls.city.setValue('');
    this.cityDisplay.set('');
    this.cities.set(this.locSvc.getCities('IN', state.isoCode));
  }

  onCityInput(value: string): void {
    this.cityDisplay.set(value);
    if (value !== this.locationForm.controls.city.value) {
      this.locationForm.controls.city.setValue('');
    }
  }

  onCitySelected(city: ICity): void {
    this.cityDisplay.set(city.name);
    this.locationForm.controls.city.setValue(city.name);
  }

  // ── Step 3 ────────────────────────────────────────────────────────

  skipMap(): void {
    this.lat.set(null);
    this.lng.set(null);
    this.step.update(s => s + 1);
    localStorage.setItem(STORAGE_STEP, this.step().toString());
  }

  private initMapStep(): void {
    if (!this.mapElRef?.nativeElement) return;

    const existing = this.lat();
    if (existing !== null) {
      this.initMap(existing, this.lng()!, 13);
      return;
    }

    const city  = this.locationForm.controls.city.value;
    const state = this.locationForm.controls.state.value;
    this.geocodingSvc.searchLocation(city, state)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          if (result) {
            this.zone.run(() => this.initMap(result.lat, result.lon, 13));
          } else {
            this.tryGeolocation();
          }
        },
        error: () => this.tryGeolocation(),
      });
  }

  private tryGeolocation(): void {
    if (!navigator.geolocation) { this.initMap(20.5937, 78.9629, 5); return; }
    navigator.geolocation.getCurrentPosition(
      pos => this.zone.run(() => this.initMap(pos.coords.latitude, pos.coords.longitude, 13)),
      ()  => this.zone.run(() => this.initMap(20.5937, 78.9629, 5)),
      { timeout: 5000 },
    );
  }

  private initMap(lat: number, lon: number, zoom: number): void {
    if (!this.mapElRef?.nativeElement || this.map) return;

    this.map = L.map(this.mapElRef.nativeElement, { zoomControl: true }).setView([lat, lon], zoom);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    }).addTo(this.map);

    const icon = L.divIcon({
      className: '',
      html: `<div style="width:40px;height:40px;background:var(--mat-sys-primary);border:3px solid white;border-radius:50%;box-shadow:0 3px 10px rgba(0,0,0,0.35);display:flex;align-items:center;justify-content:center">
               <span class="material-symbols-outlined" style="color:white;font-size:20px;line-height:1">home</span>
             </div>`,
      iconSize: [40, 40],
      iconAnchor: [20, 20],
    });

    this.lat.set(parseFloat(lat.toFixed(6)));
    this.lng.set(parseFloat(lon.toFixed(6)));

    this.marker = L.marker([lat, lon], { icon, draggable: true }).addTo(this.map);
    this.marker.on('dragend', () => {
      const pos = this.marker!.getLatLng();
      const lt  = parseFloat(pos.lat.toFixed(6));
      const ln  = parseFloat(pos.lng.toFixed(6));
      this.zone.run(() => {
        this.lat.set(lt);
        this.lng.set(ln);
        this.dragEnd$.next(pos);
      });
    });
  }

  private doReverseGeocode(lat: number, lon: number): void {
    this.geocodingSvc.reverseGeocode(lat, lon)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: addr => {
          if (addr?.displayName) {
            this.zone.run(() => this.geocodedAddress.set(addr.displayName));
          }
        },
      });
  }

  private destroyMap(): void {
    this.map?.remove();
    this.map    = undefined;
    this.marker = undefined;
  }

  // ── Step 4 ────────────────────────────────────────────────────────

  increment(field: 'bedrooms' | 'bathrooms'): void {
    if (field === 'bedrooms') this.bedrooms.update(v => v + 1);
    else                      this.bathrooms.update(v => v + 1);
  }

  decrement(field: 'bedrooms' | 'bathrooms'): void {
    if (field === 'bedrooms') this.bedrooms.update(v => Math.max(1, v - 1));
    else                      this.bathrooms.update(v => Math.max(1, v - 1));
  }

  // ── Step 8 ────────────────────────────────────────────────────────

  autoFillDeposit(): void {
    const rent = this.pricingForm.controls.monthlyRent.value;
    if (rent && rent > 0 && !this.pricingForm.controls.securityDeposit.value) {
      this.pricingForm.controls.securityDeposit.setValue(rent * 2);
    }
  }

  // ── Step 9 ────────────────────────────────────────────────────────

  isSelected(name: string): boolean { return this.selectedAmenities().has(name); }

  toggleAmenity(name: string): void {
    this.selectedAmenities.update(set => {
      const next = new Set(set);
      next.has(name) ? next.delete(name) : next.add(name);
      return next;
    });
  }

  // ── Step 10 ───────────────────────────────────────────────────────

  onImagesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const id = this.createdProperty()!.propertyId;
    this.uploadingImages.set(true);
    this.propSvc.uploadImages(id, Array.from(input.files))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  () => this.refreshProperty(id, () => this.uploadingImages.set(false)),
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
          this.createdProperty.update(p =>
            p ? { ...p, images: p.images.filter(i => i.imageId !== imageId) } : p
          );
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
    const id = this.createdProperty()!.propertyId;
    this.uploadingDoc.set(true);
    this.propSvc.uploadDocument(id, input.files[0])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next:  res => { this.ownershipDoc.set(res.data ?? null); this.uploadingDoc.set(false); },
        error: () => {
          this.uploadingDoc.set(false);
          this.snack.open('Upload failed. Accepted: PDF/JPG/PNG under 10 MB.', 'OK', { duration: 4000 });
        },
      });
    input.value = '';
  }

  submitForApproval(): void {
    if (!this.canSubmit()) return;
    const id = this.createdProperty()!.propertyId;
    this.submitting.set(true);
    this.propSvc.submitForApproval(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.clearWizardStorage();
          this.submitting.set(false);
          this.snack.open('Property submitted for admin review!', 'OK', { duration: 3000 });
          this.router.navigate(['/owner/dashboard'], { queryParams: { tab: 'properties' } });
        },
        error: () => {
          this.submitting.set(false);
          this.snack.open('Submission failed. Please try again.', 'OK', { duration: 4000 });
        },
      });
  }

  private refreshProperty(id: string, onDone: () => void): void {
    this.propSvc.getProperty(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: r => { this.createdProperty.set(r.data!); onDone(); }, error: onDone });
  }
}
