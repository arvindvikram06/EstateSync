import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { LeaseService } from '@core/services/leasing/lease.service';
import { PaymentService } from '@core/services/payment/payment.service';
import { AuthService } from '@core/services/auth/auth.service';
import { LeaseDetailResponse, LeaseVacateNoticeResponse } from '../../../core/models/lease.model';
import { LeaseBillDetail, PaymentRecord } from '../../../core/models/lease-bill.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
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
  imports: [DecimalPipe, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './lease-detail.component.html',
  styleUrl: './lease-detail.component.css'
})
export class LeaseDetailComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly svc        = inject(LeaseService);
  private readonly paymentSvc = inject(PaymentService);
  private readonly auth       = inject(AuthService);
  private readonly dialog     = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly lease   = signal<LeaseDetailResponse | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  readonly bills           = signal<LeaseBillDetail[] | null>(null);
  readonly payments        = signal<PaymentRecord[] | null>(null);
  readonly billsLoading    = signal(true);
  readonly paymentsLoading = signal(true);
  readonly payingBillId    = signal<string | null>(null);
  readonly payError        = signal<string | null>(null);
  readonly vacateNotice    = signal<LeaseVacateNoticeResponse | null>(null);
  readonly vacateActionId  = signal<'accept' | 'reject' | null>(null);
  readonly vacateError     = signal<string | null>(null);

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  private readonly uid = computed(() => this.auth.currentUser()?.userId ?? null);

  readonly canTenantSign = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.tenantId?.toLowerCase() === uid.toLowerCase() &&
      (l.status === 'PendingApproval' || l.status === 'PartialSigned') &&
      !l.tenantSignedAt;
  });

  readonly canOwnerSign = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.ownerId?.toLowerCase() === uid.toLowerCase() &&
      (l.status === 'PendingApproval' || l.status === 'PartialSigned') &&
      !l.ownerSignedAt;
  });

  readonly canVoid = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.ownerId?.toLowerCase() === uid.toLowerCase() &&
      (l.status === 'PendingApproval' || l.status === 'PartialSigned');
  });

  readonly canTerminate = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.status === 'Active' &&
      (l.ownerId?.toLowerCase() === uid.toLowerCase() || this.auth.isAdmin());
  });

  readonly hasPendingVacateNotice = computed(() => {
    const n = this.vacateNotice();
    return !!n && n.status === 'Pending';
  });

  readonly isOwnerViewer = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.ownerId?.toLowerCase() === uid.toLowerCase();
  });

  readonly backLink = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    if (this.auth.isAdmin()) return ['/admin/dashboard'];
    if (l && l.ownerId?.toLowerCase() === uid?.toLowerCase()) return ['/owner/dashboard'];
    return ['/dashboard'];
  });

  readonly backQueryParams = computed(() => {
    return { tab: 'leases' };
  });

  readonly isTenantViewer = computed(() => {
    const l = this.lease();
    const uid = this.uid();
    return !!l && !!uid && l.tenantId?.toLowerCase() === uid.toLowerCase();
  });

  goBack(): void {
    this.router.navigate(this.backLink(), { queryParams: this.backQueryParams() });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loadLease(id);
    this.loadBills(id);
    this.loadPayments(id);
    this.loadVacateNotice(id);
  }

  private loadLease(id: string): void {
    this.svc.getLeaseDetail(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.lease.set(res.data); this.loading.set(false); },
      error: () => { this.error.set('Could not load lease.'); this.loading.set(false); }
    });
  }

  private loadBills(id: string): void {
    this.svc.getLeaseBills(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.bills.set(res.data ?? []); this.billsLoading.set(false); },
      error: () => { this.bills.set([]); this.billsLoading.set(false); }
    });
  }

  private loadPayments(id: string): void {
    this.svc.getLeasePayments(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.payments.set(res.data ?? []); this.paymentsLoading.set(false); },
      error: () => { this.payments.set([]); this.paymentsLoading.set(false); }
    });
  }

  private loadVacateNotice(id: string): void {
    this.svc.getVacateNotice(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => this.vacateNotice.set(res.data ?? null),
      error: () => {},
    });
  }

  acceptVacateNotice(): void {
    const notice = this.vacateNotice();
    if (!notice) return;
    this.vacateActionId.set('accept');
    this.vacateError.set(null);
    this.svc.acceptVacateNotice(notice.vacateNoticeId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.vacateActionId.set(null);
        this.vacateNotice.update(n => n ? { ...n, status: 'Accepted' } : n);
        this.lease.update(l => l ? { ...l, status: 'Terminated' } : l);
      },
      error: err => {
        this.vacateActionId.set(null);
        this.vacateError.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to accept notice.');
      },
    });
  }

  rejectVacateNotice(): void {
    const notice = this.vacateNotice();
    if (!notice) return;
    const reason = prompt('Reason for rejection (required):');
    if (!reason?.trim()) return;
    this.vacateActionId.set('reject');
    this.vacateError.set(null);
    this.svc.rejectVacateNotice(notice.vacateNoticeId, { reason }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.vacateActionId.set(null);
        this.vacateNotice.update(n => n ? { ...n, status: 'Rejected' } : n);
      },
      error: err => {
        this.vacateActionId.set(null);
        this.vacateError.set(err?.error?.detail ?? err?.error?.message ?? 'Failed to reject notice.');
      },
    });
  }

  billIcon(type: string): string {
    const icons: Record<string, string> = {
      Rent: 'home', Deposit: 'account_balance_wallet',
      Penalty: 'gavel', Maintenance: 'build', Other: 'receipt',
    };
    return icons[type] ?? 'receipt';
  }

  isOverdue(bill: LeaseBillDetail): boolean {
    return bill.status === 'Pending' && new Date(bill.dueDate).getTime() < Date.now();
  }

  payBill(bill: LeaseBillDetail): void {
    this.payingBillId.set(bill.leaseBillId);
    this.payError.set(null);
    this.paymentSvc.createCheckoutSession(bill.leaseBillId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { window.location.href = res.data.url; },
        error: () => {
          this.payingBillId.set(null);
          this.payError.set('Failed to start payment. Please try again.');
        },
      });
  }

  openVoidDialog(): void {
    const l = this.lease();
    if (!l) return;
    const data: VoidLeaseDialogData = { leaseId: l.leaseId, propertyTitle: l.propertyTitle ?? 'Lease' };
    this.dialog.open(VoidLeaseDialogComponent, { data, width: '100%', maxWidth: '460px', panelClass: 'rounded-dialog', autoFocus: false })
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
    this.dialog.open(TerminateLeaseDialogComponent, { data, width: '100%', maxWidth: '460px', panelClass: 'rounded-dialog', autoFocus: false })
      .afterClosed().subscribe(result => {
        if (result) {
          this.router.navigate(this.backLink(), { queryParams: this.backQueryParams() });
        }
      });
  }
}
