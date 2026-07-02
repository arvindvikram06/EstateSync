import { Component, ChangeDetectionStrategy, inject, signal, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule, DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { PaymentService } from '../../../../core/services/payment.service';
import { OwnerEarningsSummaryResponse, OwnerSettlementResponse } from '../../../../core/models/lease-bill.model';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { PageSpinnerComponent } from '../../../../shared/components/page-spinner/page-spinner.component';

@Component({
  selector: 'app-owner-earnings-tab',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
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

  readonly summary = signal<OwnerEarningsSummaryResponse | null>(null);
  readonly settlements = signal<OwnerSettlementResponse[]>([]);
  readonly isLoading = signal(true);
  readonly stripeLoading = signal(false);
  readonly statusFilter = signal<string>('');
  readonly page = signal(0);
  readonly limit = signal(20);
  readonly total = signal(0);

  readonly statusOptions = [
    { label: 'All', value: '' },
    { label: 'Paid', value: 'paid' },
    { label: 'Pending', value: 'pending' },
    { label: 'Failed', value: 'failed' },
  ];

  ngOnInit(): void {
    this.loadSummary();
    this.loadSettlements();
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
