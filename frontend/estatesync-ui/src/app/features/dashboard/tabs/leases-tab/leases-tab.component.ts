import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LeaseService } from '../../../../core/services/lease.service';
import { LeaseSummaryResponse } from '../../../../core/models/lease.model';
import { AuthService } from '../../../../core/services/auth.service';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { formatDate, formatCurrency } from '../../../../core/utils/format.utils';

@Component({
  selector: 'app-leases-tab',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './leases-tab.component.html',
})
export class LeasesTabComponent implements OnInit {
  private readonly svc        = inject(LeaseService);
  private readonly authService= inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly leases  = signal<LeaseSummaryResponse[]>([]);
  readonly loading = signal(true);

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  private get userId(): string { return this.authService.currentUser()?.userId ?? ''; }

  ngOnInit(): void {
    this.svc.getMyLeases().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.leases.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  canSign(lease: LeaseSummaryResponse): boolean {
    return lease.status === 'PartialSigned' && !lease.tenantSignedAt;
  }
}
