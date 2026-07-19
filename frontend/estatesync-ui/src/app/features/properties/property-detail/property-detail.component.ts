import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef,
  ElementRef, ViewChild, effect
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, catchError, distinctUntilChanged, filter, map, switchMap, tap } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { PropertyService } from '@core/services/property/property.service';
import { SavedPropertyService } from '@core/services/property/saved-property.service';
import { AuthService } from '@core/services/auth/auth.service';
import { PropertyDetailResponse } from '../../../core/models/property.model';
import { formatCurrency } from '../../../core/utils/format.utils';
import { ApplyDialogComponent } from './apply-dialog.component';
import { ImageGalleryDialogComponent } from './image-gallery-dialog.component';
import { PropertyScheduleVisitComponent } from './property-schedule-visit.component';
import * as L from 'leaflet';


@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [
    RouterLink,

    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    PropertyScheduleVisitComponent,
    MatSnackBarModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './property-detail.component.html',
  styleUrl: './property-detail.component.css'
})
export class PropertyDetailComponent implements OnInit {
  @ViewChild('mapEl') mapElRef!: ElementRef<HTMLDivElement>;

  private readonly route            = inject(ActivatedRoute);
  private readonly router           = inject(Router);
  private readonly propService      = inject(PropertyService);
  private readonly savedPropService = inject(SavedPropertyService);
  private readonly authService      = inject(AuthService);
  private readonly dialog           = inject(MatDialog);
  private readonly snack            = inject(MatSnackBar);
  private readonly destroyRef       = inject(DestroyRef);

  readonly property    = signal<PropertyDetailResponse | null>(null);
  readonly loading     = signal(true);
  readonly error       = signal<string | null>(null);
  readonly applied     = signal(false);
  readonly kycApproved = this.authService.isKycVerified;

  readonly user       = this.authService.currentUser;
  readonly isLoggedIn = this.authService.isLoggedIn;
  readonly isOwner    = this.authService.isOwner;

  readonly isSaved = computed(() => {
    const p = this.property();
    return !!p && this.savedPropService.savedIds().has(p.propertyId);
  });

  private map?: L.Map;

  readonly isMyProperty = computed(() => {
    const p = this.property();
    const u = this.user();
    return !!p && !!u && p.ownerId === u.userId;
  });

  readonly ctaState = computed((): 'login' | 'kyc' | 'apply' | 'applied' => {
    if (this.applied()) return 'applied';
    if (!this.isLoggedIn()) return 'login';
    if (!this.kycApproved()) return 'kyc';
    return 'apply';
  });

  readonly formatCurrency = formatCurrency;

  constructor() {
    effect(() => {
      if (!this.loading() && this.property()) {
        queueMicrotask(() => this.initMap());
      }
    });
  }

  ngOnInit(): void {
    if (this.authService.isLoggedIn()) this.savedPropService.ensureIdsLoaded();

    this.route.paramMap.pipe(
      map(params => params.get('id')),
      filter((id): id is string => !!id),
      distinctUntilChanged(),
      tap(() => {
        this.map?.remove();
        this.map = undefined;
        this.property.set(null);
        this.error.set(null);
        this.loading.set(true);
      }),
      switchMap(id => this.propService.getProperty(id).pipe(
        catchError(() => {
          this.error.set('Property not found.');
          this.loading.set(false);
          return EMPTY;
        }),
      )),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(res => {
        const property = res.data;
        this.property.set(property ? {
          ...property,
          amenities: property.amenities ?? [],
          images: property.images ?? [],
        } : null);
        this.loading.set(false);
      });
  }

  toggleSave(): void {
    if (!this.authService.isLoggedIn()) { this.router.navigate(['/auth/login']); return; }
    const p = this.property();
    if (p) this.savedPropService.toggle(p.propertyId);
  }

  share(): void {
    const url = window.location.href;
    if (navigator.share) {
      navigator.share({ title: this.property()?.title ?? 'Property', url });
    } else {
      navigator.clipboard.writeText(url).then(() =>
        this.snack.open('Link copied to clipboard', 'OK', { duration: 2500 })
      );
    }
  }

imageUrl(path: string | null): string {
    return this.propService.imageUrl(path);
  }

  onCta(): void {
    if (this.ctaState() === 'login') {
      this.router.navigate(['/auth/login']);
    } else if (this.ctaState() === 'kyc') {
      this.router.navigate(['/kyc']);
    } else if (this.ctaState() === 'apply') {
      const p = this.property();
      if (!p) return;
      const ref = this.dialog.open(ApplyDialogComponent, {
        data: { propertyId: p.propertyId, propertyTitle: p.title, addressLine: p.addressLine },
        width: '100%',
        maxWidth: '520px',
        panelClass: 'rounded-dialog',
      });
      ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => {
        if (result === 'applied') this.applied.set(true);
      });
    }
  }

  openImageGallery(): void {
    const p = this.property();
    if (!p || !p.images || p.images.length === 0) return;
    
    this.dialog.open(ImageGalleryDialogComponent, {
      data: { images: p.images },
      maxWidth: '100vw',
      maxHeight: '100vh',
      height: '100%',
      width: '100%',
      panelClass: 'full-screen-modal'
    } as never);
  }

  private initMap(): void {
    if (!this.mapElRef?.nativeElement || this.map) return;

    const p = this.property();
    if (!p) return;

    const lat = p.latitude ?? 13.0827;
    const lng = p.longitude ?? 80.2707;

    this.map = L.map(this.mapElRef.nativeElement, { zoomControl: true, scrollWheelZoom: false }).setView(
      [lat, lng], p.latitude ? 14 : 7
    );

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(this.map);

    if (p.latitude && p.longitude) {
      const icon = L.divIcon({
        className: '',
        html: `<div style="width:18px;height:18px;background:var(--mat-sys-primary);border:2px solid white;border-radius:50%;box-shadow:0 2px 6px rgba(0,0,0,0.3)"></div>`,
        iconSize: [18, 18], iconAnchor: [9, 9],
      });
      L.marker([lat, lng], { icon }).addTo(this.map);
    }
  }
}
