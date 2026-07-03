import {
  Component, ChangeDetectionStrategy, inject, signal, computed, output, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { RentalApplicationService } from '../../../../core/services/rental-application.service';
import { OwnerApplicationResponse } from '../../../../core/models/rental-application.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { ReviewApplicationDialogComponent, ReviewApplicationDialogData } from './review-application-dialog.component';
import { CreateLeaseDialogComponent, CreateLeaseDialogData } from './create-lease-dialog.component';
import { formatDate } from '../../../../core/utils/format.utils';

type AppFilter = 'All' | 'Pending' | 'Approved' | 'Rejected';

@Component({
  selector: 'app-owner-applications-tab',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent, SkeletonLoaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './owner-applications-tab.component.html',
  styleUrl: './owner-applications-tab.component.css',
})
export class OwnerApplicationsTabComponent implements OnInit {
  readonly pendingCountChange = output<number>();

  private readonly svc        = inject(RentalApplicationService);
  private readonly dialog     = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly all          = signal<OwnerApplicationResponse[]>([]);
  readonly loading      = signal(true);
  readonly activeFilter = signal<AppFilter>('All');

  readonly filters: AppFilter[] = ['All', 'Pending', 'Approved', 'Rejected'];

  readonly filtered = computed(() => {
    const f = this.activeFilter();
    if (f === 'All') return this.all();
    return this.all().filter(a => a.status === f);
  });

  readonly pendingCount = computed(() => this.all().filter(a => a.status === 'Pending').length);

  readonly formatDate = formatDate;

  ngOnInit(): void {
    this.svc.getOwnerApplications().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.all.set(res.data ?? []);
        this.loading.set(false);
        this.pendingCountChange.emit(this.pendingCount());
      },
      error: () => this.loading.set(false),
    });
  }

  openReview(app: OwnerApplicationResponse, action: 'approve' | 'reject'): void {
    const data: ReviewApplicationDialogData = { application: app, action };
    const ref = this.dialog.open(ReviewApplicationDialogComponent, {
      data,
      maxWidth: '480px',
      width: '100%',
      panelClass: 'rounded-dialog',
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe((updated: OwnerApplicationResponse | undefined) => {
      if (updated) {
        this.all.update(list =>
          list.map(a => a.applicationId === updated.applicationId ? updated : a)
        );
      }
    });
  }

  openCreateLease(app: OwnerApplicationResponse): void {
    const data: CreateLeaseDialogData = {
      applicationId: app.applicationId,
      tenantName:    app.tenantName,
      propertyTitle: app.propertyTitle,
      monthlyRent:   0,
    };
    this.dialog.open(CreateLeaseDialogComponent, {
      data,
      maxWidth: '540px',
      width: '100%',
      panelClass: 'rounded-dialog',
    });
  }

  initials(name: string): string {
    return name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase();
  }
}
