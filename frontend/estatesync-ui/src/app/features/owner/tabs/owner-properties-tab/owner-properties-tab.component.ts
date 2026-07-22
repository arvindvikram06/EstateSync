import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { PropertyService } from '@core/services/property/property.service';
import { AuthService } from '@core/services/auth/auth.service';
import { DataRefreshService } from '@core/services/shared/data-refresh.service';
import { PropertySummaryResponse } from '../../../../core/models/property.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { AlertBannerComponent } from '../../../../shared/components/alert-banner/alert-banner.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { formatCurrency } from '../../../../core/utils/format.utils';

type PropFilter = 'All' | 'Draft' | 'PendingApproval' | 'Active' | 'Rejected';

@Component({
  selector: 'app-owner-properties-tab',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    StatusBadgeComponent,
    EmptyStateComponent, SkeletonLoaderComponent,
    AlertBannerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './owner-properties-tab.component.html',
  styleUrl: './owner-properties-tab.component.css',
})
export class OwnerPropertiesTabComponent implements OnInit {
  private readonly svc         = inject(PropertyService);
  private readonly dataRefresh = inject(DataRefreshService);
  private readonly authService = inject(AuthService);
  private readonly router      = inject(Router);
  private readonly dialog      = inject(MatDialog);
  private readonly destroyRef  = inject(DestroyRef);

  readonly isOnboarded   = this.authService.isOwnerVerified;
  readonly all           = signal<PropertySummaryResponse[]>([]);
  readonly loading       = signal(true);
  readonly activeFilter  = signal<PropFilter>('All');
  readonly submittingId  = signal<string | null>(null);
  readonly deletingId    = signal<string | null>(null);

  readonly filters: PropFilter[] = ['All', 'Draft', 'PendingApproval', 'Active', 'Rejected'];

  readonly filtered = computed(() => {
    const f = this.activeFilter();
    if (f === 'All') return this.all();
    return this.all().filter(p => p.status === f);
  });

  readonly formatCurrency = formatCurrency;

  hasActiveLease(property: PropertySummaryResponse): boolean {
    return property.status === 'Leased' || property.status === 'AvailableSoon';
  }

  ngOnInit(): void {
    this.loadProperties();
    this.dataRefresh.on('properties').pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.loadProperties(true);
    });
  }

  private loadProperties(silent = false): void {
    if (!silent) this.loading.set(true);
    this.svc.getMyProperties().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.all.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openAddProperty(): void {
    if (!this.isOnboarded()) return;
    // Clear any stale wizard state so a fresh wizard starts at step 1
    localStorage.removeItem('es_wizard_property_id');
    localStorage.removeItem('es_wizard_step');
    this.router.navigate(['/properties/create']);
  }

  resumeWizardDraft(prop: PropertySummaryResponse): void {
    // Seed the property ID so the wizard restores this specific draft.
    // Do NOT set es_wizard_step — the wizard will infer it from the property data.
    localStorage.setItem('es_wizard_property_id', prop.propertyId);
    localStorage.removeItem('es_wizard_step');
    this.router.navigate(['/properties/create']);
  }

  submitForApproval(property: PropertySummaryResponse): void {
    this.submittingId.set(property.propertyId);
    this.svc.submitForApproval(property.propertyId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.submittingId.set(null);
          const newStatus = res.data?.status ?? 'PendingApproval';
          this.all.update(list =>
            list.map(p => p.propertyId === property.propertyId ? { ...p, status: newStatus } : p)
          );
        },
        error: () => this.submittingId.set(null),
      });
  }

  deleteProperty(property: PropertySummaryResponse): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Property',
        message: `Delete "${property.title || 'this property'}"? This cannot be undone. Properties with an active lease cannot be deleted.`,
        confirmLabel: 'Delete',
        destructive: true,
      },
      maxWidth: '440px',
      width: '100%',
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => {
      if (!confirmed) return;
      this.deletingId.set(property.propertyId);
      this.svc.deleteProperty(property.propertyId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.deletingId.set(null);
            this.all.update(list => list.filter(p => p.propertyId !== property.propertyId));
          },
          error: () => this.deletingId.set(null),
        });
    });
  }
}
