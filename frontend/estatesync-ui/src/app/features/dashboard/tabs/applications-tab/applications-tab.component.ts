import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RentalApplicationService } from '../../../../core/services/rental-application.service';
import { RentalApplicationResponse } from '../../../../core/models/rental-application.model';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { formatDate } from '../../../../core/utils/format.utils';

type FilterStatus = 'All' | 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn';

@Component({
  selector: 'app-applications-tab',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, MatProgressSpinnerModule, StatusBadgeComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './applications-tab.component.html',
})
export class ApplicationsTabComponent implements OnInit {
  private readonly svc        = inject(RentalApplicationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly all         = signal<RentalApplicationResponse[]>([]);
  readonly loading     = signal(true);
  readonly activeFilter= signal<FilterStatus>('All');

  readonly filters: FilterStatus[] = ['All', 'Pending', 'Approved', 'Rejected', 'Withdrawn'];

  readonly formatDate = formatDate;

  get filtered(): RentalApplicationResponse[] {
    const f = this.activeFilter();
    if (f === 'All') return this.all();
    return this.all().filter(a => a.status === f);
  }

  ngOnInit(): void {
    this.svc.getMyApplications().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.all.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  setFilter(f: FilterStatus): void { this.activeFilter.set(f); }
}
