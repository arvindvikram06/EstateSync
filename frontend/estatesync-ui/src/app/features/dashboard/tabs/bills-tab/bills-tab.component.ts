import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef, output
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog } from '@angular/material/dialog';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { PaymentService } from '../../../../core/services/payment.service';
import { LeaseBillResponse } from '../../../../core/models/lease-bill.model';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { BillDetailDialogComponent } from './bill-detail-dialog.component';

type StatusFilter = 'All' | 'Pending' | 'Paid' | 'Failed';
type TypeFilter   = 'All' | 'Rent' | 'Deposit' | 'Penalty' | 'Maintenance' | 'Other';

const BILL_ICONS: Record<string, string> = {
  Rent: 'home', Deposit: 'account_balance_wallet',
  Penalty: 'gavel', Maintenance: 'build', Other: 'receipt',
};
const BILL_COLORS: Record<string, string> = {
  Rent: '#6750A4', Deposit: '#0891B2',
  Penalty: '#DC2626', Maintenance: '#D97706', Other: '#6B7280',
};

@Component({
  selector: 'app-bills-tab',
  standalone: true,
  imports: [
    CurrencyPipe, DatePipe,
    MatIconModule, MatButtonModule, MatProgressSpinnerModule,
    MatSelectModule, MatFormFieldModule,
    EmptyStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bills-tab.component.html',
  styleUrl: './bills-tab.component.css',
})
export class BillsTabComponent implements OnInit {
  private readonly svc        = inject(PaymentService);
  private readonly dialog     = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly pendingCountChange = output<number>();

  readonly bills      = signal<LeaseBillResponse[]>([]);
  readonly loading    = signal(true);
  readonly payingId   = signal<string | null>(null);
  readonly statusFilter = signal<StatusFilter>('All');
  readonly typeFilter   = signal<TypeFilter>('All');

  readonly statusTabs: { id: StatusFilter; label: string }[] = [
    { id: 'All',     label: 'All' },
    { id: 'Pending', label: 'Pending' },
    { id: 'Paid',    label: 'Paid' },
    { id: 'Failed',  label: 'Failed' },
  ];

  readonly typeOptions: TypeFilter[] = ['All', 'Rent', 'Deposit', 'Penalty', 'Maintenance', 'Other'];

  readonly filtered = computed(() => {
    const s = this.statusFilter();
    const t = this.typeFilter();
    return this.bills().filter(b =>
      (s === 'All' || b.status === s) &&
      (t === 'All' || b.billType === t)
    );
  });

  readonly summary = computed(() => {
    const all = this.bills();
    return {
      totalPaid:    all.filter(b => b.status === 'Paid').reduce((s, b) => s + b.amount, 0),
      totalPending: all.filter(b => b.status === 'Pending').reduce((s, b) => s + b.pendingAmount, 0),
      pendingCount: all.filter(b => b.status === 'Pending').length,
    };
  });

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

  setStatus(s: StatusFilter): void { this.statusFilter.set(s); }
  setType(t: TypeFilter): void { this.typeFilter.set(t); }

  openDetail(bill: LeaseBillResponse): void {
    this.dialog.open(BillDetailDialogComponent, {
      data: bill,
      maxWidth: '500px',
      width: '100%',
      panelClass: 'rounded-dialog',
      autoFocus: false,
    }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
  }

  billIcon(type: string): string  { return BILL_ICONS[type]  ?? 'receipt'; }
  billColor(type: string): string { return BILL_COLORS[type] ?? '#6B7280'; }
  billBg(type: string): string {
    return `color-mix(in srgb, ${this.billColor(type)} 12%, white)`;
  }

  daysUntilDue(dueDate: string): number {
    return Math.ceil((new Date(dueDate).getTime() - Date.now()) / 86400000);
  }
}
