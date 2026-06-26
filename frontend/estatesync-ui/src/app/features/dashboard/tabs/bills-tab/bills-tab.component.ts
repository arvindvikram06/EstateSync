import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef, output
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { PaymentService } from '../../../../core/services/payment.service';
import { LeaseBillResponse } from '../../../../core/models/lease-bill.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { LoadingButtonComponent } from '../../../../shared/components/loading-button/loading-button.component';
import { formatDate, formatCurrency } from '../../../../core/utils/format.utils';
import { PayConfirmDialogComponent } from './pay-confirm-dialog.component';

@Component({
  selector: 'app-bills-tab',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent, LoadingButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bills-tab.component.html',
})
export class BillsTabComponent implements OnInit {
  private readonly svc        = inject(PaymentService);
  private readonly dialog     = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly pendingCountChange = output<number>();

  readonly bills      = signal<LeaseBillResponse[]>([]);
  readonly loading    = signal(true);
  readonly payingId   = signal<string | null>(null);

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  ngOnInit(): void {
    this.svc.getTenantBills().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        const data = res.data ?? [];
        this.bills.set(data);
        this.loading.set(false);
        this.pendingCountChange.emit(data.filter(b => b.status === 'Pending').length);
      },
      error: () => this.loading.set(false),
    });
  }

  pay(bill: LeaseBillResponse): void {
    const ref = this.dialog.open(PayConfirmDialogComponent, {
      data: { bill },
      maxWidth: 420,
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.payingId.set(bill.leaseBillId);
      this.svc.createCheckoutSession(bill.leaseBillId).subscribe({
        next: res => { window.location.href = res.data.checkoutUrl; },
        error: () => this.payingId.set(null),
      });
    });
  }
}
