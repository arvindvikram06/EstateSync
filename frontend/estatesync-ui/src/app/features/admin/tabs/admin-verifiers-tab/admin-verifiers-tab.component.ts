import {
  Component, ChangeDetectionStrategy, inject, signal, OnInit, DestroyRef
} from '@angular/core';
import { FormControl, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SlicePipe } from '@angular/common';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { AdminService } from '@core/services/admin/admin.service';
import { StaffResponse } from '../../../../core/models/admin.model';
import { AdminCreateVerifierDialogComponent } from '../../dialogs/admin-create-verifier-dialog.component';

@Component({
  selector: 'app-admin-verifiers-tab',
  standalone: true,
  imports: [
    SlicePipe, ReactiveFormsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatDialogModule,
    EmptyStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-verifiers-tab.component.html',
  styleUrl: '../admin-kyc-tab/admin-kyc-tab.component.css',
})
export class AdminVerifiersTabComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly dialog       = inject(MatDialog);
  private readonly destroyRef   = inject(DestroyRef);

  readonly loading      = signal(true);
  readonly items        = signal<StaffResponse[]>([]);
  readonly deactivating = signal<string | null>(null);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.adminService.getStaff().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { this.items.set(res.data ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  openCreate(): void {
    const ref = this.dialog.open(AdminCreateVerifierDialogComponent, { width: '100%', maxWidth: '480px' });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(r => { if (r) this.load(); });
  }

  deactivate(userId: string): void {
    this.deactivating.set(userId);
    this.adminService.deactivateVerifier(userId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.deactivating.set(null); this.load(); },
      error: () => this.deactivating.set(null),
    });
  }
}
