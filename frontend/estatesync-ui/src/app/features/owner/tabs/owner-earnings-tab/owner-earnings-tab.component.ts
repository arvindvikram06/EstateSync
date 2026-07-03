import { Component, ChangeDetectionStrategy, inject, signal, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule, DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PaymentService } from '../../../../core/services/payment.service';
import { OwnerAvailableBalance, OwnerEarningsSummaryResponse, OwnerSettlementResponse, RequestPayoutResult } from '../../../../core/models/lease-bill.model';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { PageSpinnerComponent } from '../../../../shared/components/page-spinner/page-spinner.component';
import { PayoutConfirmDialogComponent, PayoutConfirmData } from './payout-confirm-dialog.component';

@Component({
  selector: 'app-owner-earnings-tab',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    EmptyStateComponent,
    PageSpinnerComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './owner-earnings-tab.component.html',
  styleUrl: './owner-earnings-tab.component.css',
})
export class OwnerEarningsTabComponent implements OnInit {
  private readonly paymentSvc = inject(PaymentService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog     = inject(MatDialog);

  readonly summary           = signal<OwnerEarningsSummaryResponse | null>(null);
  readonly settlements       = signal<OwnerSettlementResponse[]>([]);
  readonly isLoading         = signal(true);
  readonly stripeLoading     = signal(false);
  readonly statusFilter      = signal<string>('');
  readonly page              = signal(0);
  readonly limit             = signal(20);
  readonly total             = signal(0);

  readonly balance           = signal<OwnerAvailableBalance | null>(null);
  readonly balanceLoading    = signal(false);
  readonly requestingPayout  = signal<'standard' | 'instant' | null>(null);
  readonly payoutSuccess     = signal<RequestPayoutResult | null>(null);
  readonly payoutError       = signal<string | null>(null);

  readonly statusOptions = [
    { label: 'All', value: '' },
    { label: 'Paid', value: 'paid' },
    { label: 'Pending', value: 'pending' },
    { label: 'Failed', value: 'failed' },
  ];

  ngOnInit(): void {
    this.loadSummary();
    this.loadSettlements();
    this.loadBalance();
  }

  private loadBalance(): void {
    this.balanceLoading.set(true);
    this.paymentSvc.getOwnerAvailableBalance()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => { this.balance.set(res.data); this.balanceLoading.set(false); },
        error: ()  => this.balanceLoading.set(false),
      });
  }

  requestPayout(method: 'standard' | 'instant'): void {
    if (this.requestingPayout()) return;
    const b = this.balance();
    if (!b) return;

    const amount = method === 'instant' ? b.instantAvailable : b.available;
    const dialogData: PayoutConfirmData = { method, amount, currency: b.currency };

    const ref = this.dialog.open(PayoutConfirmDialogComponent, {
      width: '420px',
      data: dialogData,
      panelClass: 'es-dialog',
    });

    ref.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) return;
        this._doRequestPayout(method);
      });
  }

  private _doRequestPayout(method: 'standard' | 'instant'): void {
    this.requestingPayout.set(method);
    this.payoutError.set(null);
    this.payoutSuccess.set(null);

    this.paymentSvc.requestPayout(method)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.requestingPayout.set(null);
          this.payoutSuccess.set(res.data);
          this.loadBalance();
          // Optimistic: prepend payout as pending row without waiting for webhook
          const d = res.data;
          const optimistic: OwnerSettlementResponse = {
            ownerSettlementId: 'pending-' + d.stripePayoutId,
            ownerId: '',
            ownerName: '',
            ownerEmail: '',
            stripePayoutId: d.stripePayoutId,
            amount: d.amount,
            currency: d.currency,
            destinationId: '',
            arrivalDate: d.arrivalDate,
            status: 'pending',
            createdAt: new Date().toISOString(),
          };
          this.settlements.update(list => [optimistic, ...list]);
          this.total.update(t => t + 1);
        },
        error: (err) => {
          this.requestingPayout.set(null);
          this.payoutError.set(err?.error?.message ?? 'Failed to initiate payout. Please try again.');
        },
      });
  }

  private loadSummary(): void {
    this.paymentSvc.getOwnerEarningsSummary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: res => this.summary.set(res.data) });
  }

  loadSettlements(): void {
    this.isLoading.set(true);
    this.paymentSvc.getOwnerSettlements(this.statusFilter() || undefined, this.page() + 1, this.limit())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.settlements.set(res.data);
          this.total.set(res.total);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false),
      });
  }

  setStatus(value: string): void {
    this.statusFilter.set(value);
    this.page.set(0);
    this.loadSettlements();
  }

  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex);
    this.limit.set(e.pageSize);
    this.loadSettlements();
  }

  openStripeDashboard(): void {
    this.stripeLoading.set(true);
    this.paymentSvc.getStripeLoginLink()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.stripeLoading.set(false);
          window.open(res.data.url, '_blank');
        },
        error: () => this.stripeLoading.set(false),
      });
  }

  getBadgeClass(status: string): string {
    switch (status) {
      case 'paid': return 'es-badge--approved';
      case 'pending': return 'es-badge--pending';
      case 'failed': return 'es-badge--rejected';
      default: return 'es-badge--draft';
    }
  }
}
