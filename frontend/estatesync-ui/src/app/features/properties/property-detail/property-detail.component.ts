import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { PropertyService } from '../../../core/services/property.service';
import { AuthService } from '../../../core/services/auth.service';
import { KycService } from '../../../core/services/kyc.service';
import { PropertyDetailResponse } from '../../../core/models/property.model';
import { formatCurrency } from '../../../core/utils/format.utils';
import { ApplyDialogComponent } from './apply-dialog.component';

const GRADIENTS = [
  'linear-gradient(135deg, #52B69A 0%, #1A759F 100%)',
  'linear-gradient(135deg, #E9C46A 0%, #E76F51 100%)',
  'linear-gradient(135deg, #9B5DE5 0%, #6A0DAD 100%)',
  'linear-gradient(135deg, #4CC9A0 0%, #2D6A4F 100%)',
  'linear-gradient(135deg, #E77BA4 0%, #B5446E 100%)',
  'linear-gradient(135deg, #74B3CE 0%, #1B4F72 100%)',
];

function hashCode(s: string): number {
  let h = 0;
  for (let i = 0; i < s.length; i++) { h = (Math.imul(31, h) + s.charCodeAt(i)) | 0; }
  return Math.abs(h);
}

@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './property-detail.component.html',
  styleUrl: './property-detail.component.css'
})
export class PropertyDetailComponent implements OnInit {
  private readonly route       = inject(ActivatedRoute);
  private readonly router      = inject(Router);
  private readonly propService = inject(PropertyService);
  private readonly authService = inject(AuthService);
  private readonly kycService  = inject(KycService);
  private readonly dialog      = inject(MatDialog);
  private readonly destroyRef  = inject(DestroyRef);

  readonly property    = signal<PropertyDetailResponse | null>(null);
  readonly loading     = signal(true);
  readonly error       = signal<string | null>(null);
  readonly applied     = signal(false);
  readonly kycApproved = signal(false);

  readonly user       = this.authService.currentUser;
  readonly isLoggedIn = this.authService.isLoggedIn;

  readonly ctaState = computed((): 'login' | 'kyc' | 'apply' | 'applied' => {
    if (this.applied()) return 'applied';
    if (!this.isLoggedIn()) return 'login';
    if (!this.kycApproved()) return 'kyc';
    return 'apply';
  });

  readonly formatCurrency = formatCurrency;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.propService.getProperty(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.property.set(res.data); this.loading.set(false); },
        error: () => { this.error.set('Property not found.'); this.loading.set(false); }
      });

    if (this.isLoggedIn()) {
      // Token-stored isKycVerified can be stale — always check live.
      if (this.user()?.isKycVerified) {
        this.kycApproved.set(true);
      } else {
        this.kycService.getMyKyc()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (res) => {
              this.kycApproved.set((res.data ?? []).some(k => k.status === 'Approved'));
            },
          });
      }
    }
  }

  gradient(id: string): string {
    return GRADIENTS[hashCode(id) % GRADIENTS.length];
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
        maxWidth: 520,
        borderRadius: '16px',
      } as never);
      ref.afterClosed().subscribe(result => {
        if (result === 'applied') this.applied.set(true);
      });
    }
  }
}
