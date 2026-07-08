import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { SlicePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { AdminService } from '@core/services/admin/admin.service';
import { AdminComplaintResponse } from '../../../../core/models/admin.model';
import { AdminUpdateComplaintStatusDialogComponent } from '../../dialogs/admin-update-complaint-status-dialog.component';
import { Subject, EMPTY, merge, debounceTime, switchMap, catchError } from 'rxjs';

@Component({
  selector: 'app-admin-complaints-tab',
  standalone: true,
  imports: [
    SlicePipe, ReactiveFormsModule, MatButtonModule, MatIconModule,
    MatMenuModule, MatProgressSpinnerModule,
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
  private readonly statusValue   = toSignal(this.statusFilter.valueChanges,   { initialValue: this.statusFilter.value });
  private readonly priorityValue = toSignal(this.priorityFilter.valueChanges, { initialValue: this.priorityFilter.value });
  readonly statusLabel   = computed(() => this.statusValue()   || 'All statuses');
  readonly priorityLabel = computed(() => this.priorityValue() || 'Any priority');

  readonly totalPages = computed(() => Math.ceil(this.total() / this.limit) || 1);

  private readonly reload$ = new Subject<void>();

  ngOnInit(): void {
    // switchMap cancels an in-flight request when a newer reload fires,
    // so a slow older response can never overwrite fresher results.
    this.reload$
      .pipe(
        switchMap(() => this.adminService.getAdminComplaints(
          this.statusFilter.value || undefined,
          this.priorityFilter.value || undefined,
          this.page(), this.limit,
        ).pipe(
          // Catch inside switchMap so one failed request doesn't kill the stream.
          catchError(() => { this.loading.set(false); return EMPTY; }),
        )),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(res => { this.items.set(res.data ?? []); this.total.set(res.total); this.loading.set(false); });

    // Both filters share one debounced trigger — rapid changes fire one request.
    merge(this.statusFilter.valueChanges, this.priorityFilter.valueChanges)
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => { this.page.set(1); this.load(); });

    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.reload$.next();
  }

  openUpdateStatus(complaint: AdminComplaintResponse): void {
    const ref = this.dialog.open(AdminUpdateComplaintStatusDialogComponent, {
      width: '100%',
      maxWidth: '420px',
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
