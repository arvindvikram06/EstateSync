import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { LeaseService } from '../../../core/services/lease.service';
import { AuthService } from '../../../core/services/auth.service';
import { LeaseDetailResponse } from '../../../core/models/lease.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { formatDate, formatCurrency } from '../../../core/utils/format.utils';
import {
  VoidLeaseDialogComponent,
  VoidLeaseDialogData,
} from '../../owner/tabs/owner-leases-tab/void-lease-dialog.component';
import {
  TerminateLeaseDialogComponent,
  TerminateLeaseDialogData,
} from '../../owner/tabs/owner-leases-tab/terminate-lease-dialog.component';

@Component({
  selector: 'app-lease-detail',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './lease-detail.component.html',
  styleUrl: './lease-detail.component.css'
})
export class LeaseDetailComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly svc        = inject(LeaseService);
  private readonly auth       = inject(AuthService);
  private readonly dialog     = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly lease   = signal<LeaseDetailResponse | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  private readonly uid = computed(() => this.auth.currentUser()?.userId ?? null);

  readonly canTenantSign = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.tenantId === uid &&
      (l.status === 'PendingApproval' || l.status === 'PartialSigned') &&
      !l.tenantSignedAt;
  });

  readonly canOwnerSign = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.ownerId === uid &&
      (l.status === 'PendingApproval' || l.status === 'PartialSigned') &&
      !l.ownerSignedAt;
  });

  readonly canVoid = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.ownerId === uid &&
      (l.status === 'PendingApproval' || l.status === 'PartialSigned');
  });

  readonly canTerminate = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.status === 'Active' &&
      (l.ownerId === uid || this.auth.isAdmin());
  });

  readonly backLink = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    if (l && l.ownerId === uid) return ['/owner/dashboard'];
    return ['/dashboard'];
  });

  readonly backQueryParams = computed(() => {
    return { tab: 'leases' };
  });

  ngOnInit(): void {
    this.loadLease();
  }

  private loadLease(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.getLeaseDetail(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.lease.set(res.data); this.loading.set(false); },
      error: () => { this.error.set('Could not load lease.'); this.loading.set(false); }
    });
  }

  openVoidDialog(): void {
    const l = this.lease();
    if (!l) return;
    const data: VoidLeaseDialogData = { leaseId: l.leaseId, propertyTitle: l.propertyTitle ?? 'Lease' };
    this.dialog.open(VoidLeaseDialogComponent, { data, panelClass: 'rounded-dialog', autoFocus: false })
      .afterClosed().subscribe(result => {
        if (result) {
          this.router.navigate(this.backLink(), { queryParams: this.backQueryParams() });
        }
      });
  }

  openTerminateDialog(): void {
    const l = this.lease();
    if (!l) return;
    const data: TerminateLeaseDialogData = { leaseId: l.leaseId, propertyTitle: l.propertyTitle ?? 'Lease' };
    this.dialog.open(TerminateLeaseDialogComponent, { data, panelClass: 'rounded-dialog', autoFocus: false })
      .afterClosed().subscribe(result => {
        if (result) {
          this.router.navigate(this.backLink(), { queryParams: this.backQueryParams() });
        }
      });
  }
}
