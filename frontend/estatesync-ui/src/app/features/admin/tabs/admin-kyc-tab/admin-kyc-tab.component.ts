import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SlicePipe } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { AdminService } from '../../../../core/services/admin.service';
import { KycVerificationResponse } from '../../../../core/models/admin.model';
import { AdminReviewDialogComponent, AdminReviewDialogData } from '../../dialogs/admin-review-dialog.component';

@Component({
  selector: 'app-admin-kyc-tab',
  standalone: true,
  imports: [
    SlicePipe, ReactiveFormsModule, MatButtonModule, MatIconModule,
    MatSelectModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule,
    StatusBadgeComponent, EmptyStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-kyc-tab.component.html',
  styleUrl: './admin-kyc-tab.component.css',
})
export class AdminKycTabComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly dialog       = inject(MatDialog);
  private readonly destroyRef   = inject(DestroyRef);

  readonly loading    = signal(true);
  readonly items      = signal<KycVerificationResponse[]>([]);
  readonly totalCount = signal(0);
  readonly page       = signal(1);
  readonly limit      = 20;

  readonly statusFilter = new FormControl<string>('Pending');
  readonly searchControl = new FormControl<string>('');

  private readonly searchTerm = signal('');

  readonly filteredItems = computed(() => {
    const q = this.searchTerm().trim().toLowerCase();
    // Always exclude Draft entries regardless of filter
    let list = this.items().filter(i => i.status !== 'Draft');
    if (q) {
      list = list.filter(i =>
        i.fullName.toLowerCase().includes(q) || i.email.toLowerCase().includes(q)
      );
    }
    return list;
  });

  ngOnInit(): void {
    this.load();
    this.statusFilter.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.page.set(1);
      this.load();
    });
    this.searchControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(v => {
      this.searchTerm.set(v ?? '');
    });
  }

  load(): void {
    this.loading.set(true);
    const status = this.statusFilter.value || undefined;
    this.adminService.getKycRequests(status, this.page(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.items.set(res.data ?? []); this.totalCount.set(res.total); this.loading.set(false); },
        error: () => this.loading.set(false),
      });
  }

  openReview(item: KycVerificationResponse): void {
    const isPending = item.status === 'Pending';
    const data: AdminReviewDialogData = {
      title: isPending ? 'Review KYC' : 'KYC Details',
      subtitle: `${item.fullName} · ${item.email}`,
      entityId: item.kycVerificationId,
      type: 'kyc',
      readOnly: !isPending,
      currentStatus: item.status,
      currentRemarks: item.reviewRemarks ?? undefined,
      kycItem: item,
    };
    const ref = this.dialog.open(AdminReviewDialogComponent, {
      width: '620px',
      panelClass: 'rounded-dialog',
      data,
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => {
      if (result) this.load();
    });
  }

  prevPage(): void { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
  nextPage(): void { if (this.page() * this.limit < this.totalCount()) { this.page.update(p => p + 1); this.load(); } }

  get totalPages(): number { return Math.ceil(this.totalCount() / this.limit); }
}
