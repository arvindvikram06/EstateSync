import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { SlicePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { AdminService } from '../../../../core/services/admin.service';
import { AdminComplaintResponse } from '../../../../core/models/admin.model';
import { AdminUpdateComplaintStatusDialogComponent } from '../../dialogs/admin-update-complaint-status-dialog.component';

@Component({
  selector: 'app-admin-complaints-tab',
  standalone: true,
  imports: [
    SlicePipe, ReactiveFormsModule, MatButtonModule, MatIconModule,
    MatSelectModule, MatFormFieldModule, MatProgressSpinnerModule,
    StatusBadgeComponent, EmptyStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-complaints-tab.component.html',
  styleUrl: '../admin-kyc-tab/admin-kyc-tab.component.css',
})
export class AdminComplaintsTabComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly dialog       = inject(MatDialog);
  private readonly destroyRef   = inject(DestroyRef);

  readonly loading = signal(true);
  readonly items   = signal<AdminComplaintResponse[]>([]);
  readonly total   = signal(0);
  readonly page    = signal(1);
  readonly limit   = 20;

  readonly statusFilter   = new FormControl<string>('');
  readonly priorityFilter = new FormControl<string>('');

  readonly totalPages = computed(() => Math.ceil(this.total() / this.limit) || 1);

  ngOnInit(): void {
    this.load();
    this.statusFilter.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.page.set(1); this.load();
    });
    this.priorityFilter.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.page.set(1); this.load();
    });
  }

  load(): void {
    this.loading.set(true);
    const status   = this.statusFilter.value || undefined;
    const priority = this.priorityFilter.value || undefined;
    this.adminService.getAdminComplaints(status, priority, this.page(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: res => { this.items.set(res.data ?? []); this.total.set(res.total); this.loading.set(false); },
        error: () => this.loading.set(false),
      });
  }

  openUpdateStatus(complaint: AdminComplaintResponse): void {
    const ref = this.dialog.open(AdminUpdateComplaintStatusDialogComponent, {
      width: '420px',
      data: { complaintId: complaint.complaintId, currentStatus: complaint.status },
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(r => { if (r) this.load(); });
  }

  prevPage(): void { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
  nextPage(): void { if (this.page() < this.totalPages()) { this.page.update(p => p + 1); this.load(); } }

  priorityClass(p: string): string {
    const map: Record<string, string> = {
      Critical: 'es-badge--rejected',
      High: 'es-badge--pending',
      Medium: 'es-badge--partial',
      Low: 'es-badge--draft',
    };
    return map[p] ?? 'es-badge--draft';
  }
}
