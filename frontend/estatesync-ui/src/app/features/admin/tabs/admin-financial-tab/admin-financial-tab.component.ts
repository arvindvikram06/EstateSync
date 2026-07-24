import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { SlicePipe, TitleCasePipe } from '@angular/common';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { formatCurrency, formatUsd } from '../../../../core/utils/format.utils';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { AdminService } from '@core/services/admin/admin.service';
import {
  SettlementSummaryResponse, OwnerSettlementResponse,
  ConnectedAccountResponse, AdminPaymentResponse
} from '../../../../core/models/admin.model';

@Component({
  selector: 'app-admin-financial-tab',
  standalone: true,
  imports: [
    SlicePipe, TitleCasePipe, ReactiveFormsModule, MatButtonModule, MatIconModule,
    MatMenuModule, MatProgressSpinnerModule, MatTabsModule, MatTooltipModule,
    StatusBadgeComponent, EmptyStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-financial-tab.component.html',
  styleUrl: './admin-financial-tab.component.css',
})
export class AdminFinancialTabComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly destroyRef   = inject(DestroyRef);

  // Summary
  readonly summaryLoading = signal(true);
  readonly summaryError   = signal(false);
  readonly summary        = signal<SettlementSummaryResponse | null>(null);
  readonly platformFee = new FormControl<number | null>(null, {
    validators: [Validators.required, Validators.min(0), Validators.max(25)],
  });
  readonly platformFeeLoading = signal(true);
  readonly platformFeeSaving = signal(false);
  readonly platformFeeMessage = signal('');
  readonly platformFeeError = signal('');

  // Settlements
  readonly settlementsLoading = signal(true);
  readonly settlementsError   = signal(false);
  readonly settlements        = signal<OwnerSettlementResponse[]>([]);
  readonly settlementTotal    = signal(0);
  readonly settlementPage     = signal(1);
  readonly settlementStatus   = new FormControl<string>('');
  readonly settlementSearch   = new FormControl<string>('');
  private readonly settlementQ = signal('');
  private readonly settlementStatusValue = toSignal(this.settlementStatus.valueChanges, { initialValue: this.settlementStatus.value });
  readonly settlementStatusLabel = computed(() => this.settlementStatusValue() || 'All statuses');

  readonly filteredSettlements = computed(() => {
    const q = this.settlementQ().trim().toLowerCase();
    if (!q) return this.settlements();
    return this.settlements().filter(s =>
      s.ownerName.toLowerCase().includes(q) || s.ownerEmail.toLowerCase().includes(q)
    );
  });

  // Connected accounts
  readonly accountsLoading  = signal(true);
  readonly accountsError    = signal(false);
  readonly accounts         = signal<ConnectedAccountResponse[]>([]);
  readonly accountTotal     = signal(0);
  readonly accountPage      = signal(1);
  readonly accountSearch    = new FormControl<string>('');
  private readonly accountQ = signal('');
  readonly syncingId        = signal<string | null>(null);

  readonly filteredAccounts = computed(() => {
    const q = this.accountQ().trim().toLowerCase();
    if (!q) return this.accounts();
    return this.accounts().filter(a =>
      a.ownerName.toLowerCase().includes(q) || a.ownerEmail.toLowerCase().includes(q) ||
      a.stripeAccountId.toLowerCase().includes(q)
    );
  });

  // Payments
  readonly paymentsLoading = signal(true);
  readonly paymentsError   = signal(false);
  readonly payments        = signal<AdminPaymentResponse[]>([]);
  readonly paymentTotal    = signal(0);
  readonly paymentPage     = signal(1);
  readonly paymentStatus   = new FormControl<string>('');
  readonly paymentSearch   = new FormControl<string>('');
  private readonly paymentQ = signal('');
  private readonly paymentStatusValue = toSignal(this.paymentStatus.valueChanges, { initialValue: this.paymentStatus.value });
  readonly paymentStatusLabel = computed(() => this.paymentStatusValue() || 'All statuses');

  readonly filteredPayments = computed(() => {
    const q = this.paymentQ().trim().toLowerCase();
    if (!q) return this.payments();
    return this.payments().filter(p =>
      p.paidByName.toLowerCase().includes(q) || p.paidByEmail.toLowerCase().includes(q) ||
      p.transactionId.toLowerCase().includes(q)
    );
  });

  readonly limit = 20;

  ngOnInit(): void {
    this.loadAll();

    this.settlementStatus.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.settlementPage.set(1); this.loadSettlements();
    });
    this.settlementSearch.valueChanges.pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef)).subscribe(v => {
      this.settlementQ.set(v ?? '');
    });
    this.accountSearch.valueChanges.pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef)).subscribe(v => {
      this.accountQ.set(v ?? '');
    });
    this.paymentStatus.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.paymentPage.set(1); this.loadPayments();
    });
    this.paymentSearch.valueChanges.pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef)).subscribe(v => {
      this.paymentQ.set(v ?? '');
    });
  }

  loadAll(): void {
    this.loadSummary();
    this.loadPlatformFee();
    this.loadSettlements();
    this.loadAccounts();
    this.loadPayments();
  }

  loadPlatformFee(): void {
    this.platformFeeLoading.set(true);
    this.platformFeeError.set('');
    this.adminService.getPlatformFee().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => {
        this.platformFee.setValue(res.data?.percentage ?? null);
        this.platformFeeLoading.set(false);
      },
      error: () => {
        this.platformFeeLoading.set(false);
        this.platformFeeError.set('Could not load the platform charge.');
      },
    });
  }

  savePlatformFee(): void {
    this.platformFee.markAsTouched();
    if (this.platformFee.invalid || this.platformFee.value === null) return;

    this.platformFeeSaving.set(true);
    this.platformFeeMessage.set('');
    this.platformFeeError.set('');
    this.adminService.updatePlatformFee(this.platformFee.value)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: res => {
          this.platformFee.setValue(res.data?.percentage ?? this.platformFee.value);
          this.platformFeeSaving.set(false);
          this.platformFeeMessage.set('Platform charge updated. New checkouts will use this rate.');
        },
        error: () => {
          this.platformFeeSaving.set(false);
          this.platformFeeError.set('Could not update the platform charge.');
        },
      });
  }

  loadSummary(): void {
    this.summaryLoading.set(true);
    this.summaryError.set(false);
    this.adminService.getFinancialSummary().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.summary.set(res.data ?? null); this.summaryLoading.set(false); },
      error: () => { this.summaryLoading.set(false); this.summaryError.set(true); },
    });
  }

  loadSettlements(): void {
    this.settlementsLoading.set(true);
    this.settlementsError.set(false);
    const status = this.settlementStatus.value || undefined;
    this.adminService.getSettlements({ status }, this.settlementPage(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: res => {
          this.settlements.set((res.data as OwnerSettlementResponse[]) ?? []);
          this.settlementTotal.set(res.total);
          this.settlementsLoading.set(false);
        },
        error: () => { this.settlementsLoading.set(false); this.settlementsError.set(true); },
      });
  }

  loadAccounts(): void {
    this.accountsLoading.set(true);
    this.accountsError.set(false);
    this.adminService.getConnectedAccounts(this.accountPage(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: res => {
          this.accounts.set((res.data as ConnectedAccountResponse[]) ?? []);
          this.accountTotal.set(res.total);
          this.accountsLoading.set(false);
        },
        error: () => { this.accountsLoading.set(false); this.accountsError.set(true); },
      });
  }

  syncAccount(account: ConnectedAccountResponse): void {
    this.syncingId.set(account.id);
    this.adminService.syncConnectedAccount(account.id)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: res => {
          if (res.data) {
            this.accounts.update(list =>
              list.map(a => a.id === account.id ? res.data! : a)
            );
          }
          this.syncingId.set(null);
        },
        error: () => this.syncingId.set(null),
      });
  }

  loadPayments(): void {
    this.paymentsLoading.set(true);
    this.paymentsError.set(false);
    const status = this.paymentStatus.value || undefined;
    this.adminService.getPayments({ status }, this.paymentPage(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: res => {
          this.payments.set((res.data as AdminPaymentResponse[]) ?? []);
          this.paymentTotal.set(res.total);
          this.paymentsLoading.set(false);
        },
        error: () => { this.paymentsLoading.set(false); this.paymentsError.set(true); },
      });
  }

  readonly fmt    = formatUsd;
  readonly fmtInr = formatCurrency;

  settlementPages = computed(() => Math.ceil(this.settlementTotal() / this.limit));
  accountPages    = computed(() => Math.ceil(this.accountTotal() / this.limit));
  paymentPages    = computed(() => Math.ceil(this.paymentTotal() / this.limit));

  prevSettlementPage(): void { if (this.settlementPage() > 1) { this.settlementPage.update(p => p - 1); this.loadSettlements(); } }
  nextSettlementPage(): void { if (this.settlementPage() < this.settlementPages()) { this.settlementPage.update(p => p + 1); this.loadSettlements(); } }
  prevAccountPage(): void { if (this.accountPage() > 1) { this.accountPage.update(p => p - 1); this.loadAccounts(); } }
  nextAccountPage(): void { if (this.accountPage() < this.accountPages()) { this.accountPage.update(p => p + 1); this.loadAccounts(); } }
  prevPaymentPage(): void { if (this.paymentPage() > 1) { this.paymentPage.update(p => p - 1); this.loadPayments(); } }
  nextPaymentPage(): void { if (this.paymentPage() < this.paymentPages()) { this.paymentPage.update(p => p + 1); this.loadPayments(); } }
}
