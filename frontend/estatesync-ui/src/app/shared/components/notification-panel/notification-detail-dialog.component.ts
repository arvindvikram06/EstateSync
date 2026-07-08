import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { NotificationResponse } from '../../../core/models/notification.model';
import { NotificationService } from '@core/services/shared/notification.service';

export interface NotificationDetailDialogData {
  notification: NotificationResponse;
  icon: string;
  timeAgo: string;
  route: { path: string[]; queryParams?: Record<string, string> } | null;
}

@Component({
  selector: 'app-notification-detail-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="notif-dialog">
      <!-- Icon + title row -->
      <div class="notif-dialog__header">
        <div class="notif-dialog__icon-wrap">
          <mat-icon class="notif-dialog__icon">{{ data.icon }}</mat-icon>
        </div>
        <div class="notif-dialog__title-wrap">
          <h2 class="notif-dialog__title">{{ data.notification.title }}</h2>
          <span class="notif-dialog__time">{{ data.timeAgo }}</span>
        </div>
      </div>

      <!-- Full message -->
      <p class="notif-dialog__message">{{ data.notification.message }}</p>

      <!-- Actions -->
      <div class="notif-dialog__actions">
        <button mat-stroked-button class="!rounded-full !px-6" (click)="dismiss()">
          Dismiss
        </button>
        @if (data.route) {
          <button mat-flat-button color="primary" class="!rounded-full !px-6" (click)="view()">
            View
          </button>
        }
      </div>
    </div>
  `,
  styles: [`
    .notif-dialog {
      padding: 28px 28px 20px;
    }

    .notif-dialog__header {
      display: flex;
      align-items: flex-start;
      gap: 14px;
      margin-bottom: 16px;
    }

    .notif-dialog__icon-wrap {
      width: 44px;
      height: 44px;
      border-radius: 50%;
      background: color-mix(in srgb, var(--mat-sys-primary) 12%, white);
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .notif-dialog__icon {
      color: var(--mat-sys-primary);
      font-size: 22px;
      width: 22px;
      height: 22px;
    }

    .notif-dialog__title-wrap {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 3px;
    }

    .notif-dialog__title {
      font-size: 16px;
      font-weight: 700;
      color: #222;
      margin: 0;
      line-height: 1.3;
    }

    .notif-dialog__time {
      font-size: 12px;
      color: #999;
    }

    .notif-dialog__message {
      font-size: 14px;
      color: #555;
      line-height: 1.6;
      margin: 0 0 24px;
    }

    .notif-dialog__actions {
      display: flex;
      justify-content: flex-end;
      gap: 10px;
    }
  `],
})
export class NotificationDetailDialogComponent {
  readonly data: NotificationDetailDialogData = inject(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<NotificationDetailDialogComponent>);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  // markOneRead is deliberately fire-and-forget: the dialog closes immediately,
  // and the request must not be cancelled by the dialog's destruction.
  dismiss(): void {
    this.notificationService.markOneRead(this.data.notification.notificationId).subscribe();
    this.dialogRef.close();
  }

  view(): void {
    this.notificationService.markOneRead(this.data.notification.notificationId).subscribe();
    if (this.data.route) {
      this.router.navigate(this.data.route.path, this.data.route.queryParams ? { queryParams: this.data.route.queryParams } : {});
    }
    this.dialogRef.close();
  }
}
