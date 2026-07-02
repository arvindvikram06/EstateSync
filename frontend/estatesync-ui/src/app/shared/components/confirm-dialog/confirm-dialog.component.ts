import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  destructive?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-7" style="min-width:360px;max-width:440px">
      <div class="flex items-center gap-3 mb-4">
        <div class="flex items-center justify-center w-10 h-10 rounded-full"
          [style.background]="data.destructive ? '#FEE2E2' : '#FEF3C7'">
          <mat-icon class="!text-[20px]"
            [style.color]="data.destructive ? '#991B1B' : '#92400E'">
            {{ data.destructive ? 'delete' : 'warning' }}
          </mat-icon>
        </div>
        <h2 class="text-[18px] font-bold text-es-text m-0">{{ data.title }}</h2>
      </div>
      <p class="text-sm text-es-muted mb-6">{{ data.message }}</p>
      <div class="flex gap-3 justify-end">
        <button mat-stroked-button class="!rounded-full !px-6" mat-dialog-close>Cancel</button>
        <button mat-flat-button
          class="!rounded-full !px-6 !font-semibold"
          [style.background]="data.destructive ? '#DC2626' : undefined"
          [style.color]="data.destructive ? 'white' : undefined"
          [color]="data.destructive ? undefined : 'primary'"
          [mat-dialog-close]="true">
          {{ data.confirmLabel ?? 'Confirm' }}
        </button>
      </div>
    </div>
  `
})
export class ConfirmDialogComponent {
  readonly data: ConfirmDialogData = inject(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
}
