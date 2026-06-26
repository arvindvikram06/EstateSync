import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LeaseService } from '../../../core/services/lease.service';
import { AuthService } from '../../../core/services/auth.service';
import { LeaseDetailResponse } from '../../../core/models/lease.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { formatDate, formatCurrency } from '../../../core/utils/format.utils';

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
  private readonly svc        = inject(LeaseService);
  private readonly authService= inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly lease   = signal<LeaseDetailResponse | null>(null);
  readonly loading = signal(true);
  readonly error   = signal<string | null>(null);

  readonly formatDate     = formatDate;
  readonly formatCurrency = formatCurrency;

  private get userId(): string { return this.authService.currentUser()?.userId ?? ''; }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.svc.getLeaseDetail(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.lease.set(res.data); this.loading.set(false); },
      error: () => { this.error.set('Could not load lease.'); this.loading.set(false); }
    });
  }

  get canSign(): boolean {
    const l = this.lease();
    if (!l) return false;
    return l.status === 'PartialSigned' && !l.tenantSignedAt;
  }
}
