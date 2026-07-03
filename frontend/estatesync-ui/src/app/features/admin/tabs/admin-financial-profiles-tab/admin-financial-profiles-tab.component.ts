import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SlicePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { AdminService } from '../../../../core/services/admin.service';
import { FinancialProfileResponse } from '../../../../core/models/admin.model';
import { AdminReviewDialogComponent, AdminReviewDialogData } from '../../dialogs/admin-review-dialog.component';

@Component({
  selector: 'app-admin-financial-profiles-tab',
  standalone: true,
  imports: [
    SlicePipe, ReactiveFormsModule, MatButtonModule, MatIconModule,
    MatSelectModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule,
    StatusBadgeComponent, EmptyStateComponent, SkeletonLoaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-financial-profiles-tab.component.html',
  styleUrl: '../admin-kyc-tab/admin-kyc-tab.component.css',
})
export class AdminFinancialProfilesTabComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly dialog       = inject(MatDialog);
  private readonly destroyRef   = inject(DestroyRef);

  readonly loading     = signal(true);
  readonly reviewingId = signal<string | null>(null);
  readonly items       = signal<FinancialProfileResponse[]>([]);
  readonly totalCount  = signal(0);
  readonly page        = signal(1);
  readonly limit       = 20;

  readonly statusFilter = new FormControl<string>('Pending');
  readonly searchControl = new FormControl<string>('');
  private readonly searchTerm = signal('');

  readonly filteredItems = computed(() => {
    const q = this.searchTerm().trim().toLowerCase();
    let list = this.items().filter(i => i.status !== 'Draft');
    if (q) {
      list = list.filter(i =>
        i.tenantName.toLowerCase().includes(q) || i.tenantEmail.toLowerCase().includes(q)
      );
    }
    return list;
  });

  ngOnInit(): void {
    this.load();
    this.statusFilter.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.page.set(1); this.load();
    });
    this.searchControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(v => {
      this.searchTerm.set(v ?? '');
    });
  }

  load(): void {
    this.loading.set(true);
    const status = this.statusFilter.value || undefined;
    this.adminService.getFinancialProfiles(status, this.page(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.items.set(res.data ?? []); this.totalCount.set(res.total); this.loading.set(false); },
        error: () => this.loading.set(false),
      });
  }

  openReview(item: FinancialProfileResponse): void {
    if (this.reviewingId()) return;
    this.reviewingId.set(item.profileId);

    this.adminService.getFinancialProfileById(item.profileId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.reviewingId.set(null);
          const full = res.data;
          const isPending = full.status === 'Pending';
          const data: AdminReviewDialogData = {
            title: isPending ? 'Review Financial Profile' : 'Financial Profile',
            subtitle: `${full.tenantName} · ${full.tenantEmail}`,
            entityId: full.profileId,
            type: 'financial-profile',
            readOnly: !isPending,
            currentStatus: full.status,
            currentRemarks: full.reviewRemarks ?? undefined,
            financialProfileItem: full,
          };
          this.dialog.open(AdminReviewDialogComponent, {
            width: '620px',
            panelClass: 'rounded-dialog',
            data,
          }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(r => {
            if (r) this.load();
          });
        },
        error: () => this.reviewingId.set(null),
      });
  }

  prevPage(): void { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
  nextPage(): void { if (this.page() * this.limit < this.totalCount()) { this.page.update(p => p + 1); this.load(); } }
  get totalPages(): number { return Math.ceil(this.totalCount() / this.limit); }
}
