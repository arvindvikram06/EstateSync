import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { SlicePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { AdminService } from '../../../../core/services/admin.service';
import { AuditLogResponse } from '../../../../core/models/admin.model';

@Component({
  selector: 'app-admin-audit-logs-tab',
  standalone: true,
  imports: [
    SlicePipe, ReactiveFormsModule, MatButtonModule, MatIconModule,
    MatSelectModule, MatInputModule, MatFormFieldModule, MatProgressSpinnerModule,
    EmptyStateComponent, SkeletonLoaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-audit-logs-tab.component.html',
  styleUrl: '../admin-kyc-tab/admin-kyc-tab.component.css',
})
export class AdminAuditLogsTabComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly destroyRef   = inject(DestroyRef);

  readonly loading = signal(true);
  readonly items   = signal<AuditLogResponse[]>([]);
  readonly total   = signal(0);
  readonly page    = signal(1);
  readonly limit   = 30;

  readonly entityTypeFilter = new FormControl<string>('');
  readonly actionFilter     = new FormControl<string>('');

  readonly totalPages = computed(() => Math.ceil(this.total() / this.limit) || 1);

  readonly entityTypes = [
    'User', 'Property', 'Lease', 'RentalApplication',
    'KycVerification', 'TenantFinancialProfile', 'Complaint',
    'Payment', 'OwnerSettlement',
  ];

  readonly actions = [
    'Created', 'Updated', 'Deleted', 'StatusChanged', 'Approved', 'Rejected',
  ];

  ngOnInit(): void {
    this.load();
    this.entityTypeFilter.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.page.set(1); this.load();
    });
    this.actionFilter.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.page.set(1); this.load();
    });
  }

  load(): void {
    this.loading.set(true);
    const entityType = this.entityTypeFilter.value || undefined;
    const action     = this.actionFilter.value || undefined;
    this.adminService.getAuditLogs({ entityType, action }, this.page(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: res => { this.items.set(res.data ?? []); this.total.set(res.total); this.loading.set(false); },
        error: () => this.loading.set(false),
      });
  }

  prevPage(): void { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
  nextPage(): void { if (this.page() < this.totalPages()) { this.page.update(p => p + 1); this.load(); } }

  actionClass(action: string): string {
    if (action === 'Deleted' || action === 'Rejected') return 'es-badge--rejected';
    if (action === 'Approved' || action === 'Created') return 'es-badge--approved';
    if (action === 'StatusChanged' || action === 'Updated') return 'es-badge--partial';
    return 'es-badge--draft';
  }
}
