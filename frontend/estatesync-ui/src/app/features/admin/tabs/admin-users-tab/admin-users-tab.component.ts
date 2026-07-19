import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit, DestroyRef
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminService } from '@core/services/admin/admin.service';
import { AdminUserResponse } from '@core/models/admin.model';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader.component';
import { Subject, debounceTime } from 'rxjs';

@Component({
  selector: 'app-admin-users-tab',
  standalone: true,
  imports: [
    FormsModule, DatePipe,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatProgressSpinnerModule, MatTooltipModule,
    EmptyStateComponent, SkeletonLoaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-users-tab.component.html',
})
export class AdminUsersTabComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly destroyRef   = inject(DestroyRef);

  readonly loading      = signal(true);
  readonly users        = signal<AdminUserResponse[]>([]);
  readonly total        = signal(0);
  readonly page         = signal(1);
  readonly limit        = 20;
  readonly actionId     = signal<string | null>(null);

  readonly search   = signal('');
  readonly roleFilter   = signal('');
  readonly activeFilter = signal<string>('');

  readonly hasFilters = computed(() => !!this.search() || !!this.roleFilter() || this.activeFilter() !== '');
  readonly totalPages = computed(() => Math.ceil(this.total() / this.limit));

  private readonly search$ = new Subject<void>();

  readonly roleOptions = [
    { value: '', label: 'All Roles' },
    { value: 'Admin', label: 'Admin' },
    { value: 'Owner', label: 'Owner' },
    { value: 'Tenant', label: 'Tenant' },
    { value: 'Verifier', label: 'Verifier' },
  ];

  readonly activeOptions = [
    { value: '', label: 'All Status' },
    { value: 'true', label: 'Active' },
    { value: 'false', label: 'Disabled' },
  ];

  ngOnInit(): void {
    this.search$.pipe(debounceTime(400), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => { this.page.set(1); this.load(); });
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const isActive = this.activeFilter() === '' ? undefined : this.activeFilter() === 'true';
    this.adminService.getUsers({
      search: this.search() || undefined,
      role: this.roleFilter() || undefined,
      isActive,
    }, this.page(), this.limit)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.users.set(res.data ?? []);
          this.total.set(res.total);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  onSearchInput(): void { this.search$.next(); }

  onFilterChange(): void { this.page.set(1); this.load(); }

  clearFilters(): void {
    this.search.set('');
    this.roleFilter.set('');
    this.activeFilter.set('');
    this.page.set(1);
    this.load();
  }

  prevPage(): void { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
  nextPage(): void { if (this.page() < this.totalPages()) { this.page.update(p => p + 1); this.load(); } }

  toggleActive(user: AdminUserResponse): void {
    const newState = !user.isActive;
    this.actionId.set(user.userId);
    this.adminService.setUserActive(user.userId, newState)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => { this.actionId.set(null); this.load(); },
        error: () => this.actionId.set(null),
      });
  }

  primaryRole(user: AdminUserResponse): string {
    if (user.roles.includes('Admin'))    return 'Admin';
    if (user.roles.includes('Owner'))    return 'Owner';
    if (user.roles.includes('Verifier')) return 'Verifier';
    return 'Tenant';
  }
}
