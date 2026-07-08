import { Component, ChangeDetectionStrategy, computed, input, output, inject } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { NotificationResponse } from '../../../core/models/notification.model';
import { NotificationType } from '../../../core/models/enums';
import {
  NotificationDetailDialogComponent,
  NotificationDetailDialogData,
} from './notification-detail-dialog.component';

function iconForType(type: NotificationType): string {
  switch (type) {
    case NotificationType.PaymentReceived:
    case NotificationType.PaymentFailed:
    case NotificationType.RentDue:
      return 'payments';
    case NotificationType.LeaseCreated:
    case NotificationType.LeaseApprovedByAdmin:
    case NotificationType.LeasePendingAdminApproval:
    case NotificationType.LeaseExpiring:
    case NotificationType.LeaseExpired:
    case NotificationType.LeaseTerminated:
    case NotificationType.LeaseRejectedByAdmin:
    case NotificationType.LeaseVoided:
      return 'article';
    case NotificationType.ApplicationReceived:
    case NotificationType.ApplicationApproved:
    case NotificationType.ApplicationRejected:
      return 'mail';
    case NotificationType.ComplaintRaised:
    case NotificationType.ComplaintUpdated:
    case NotificationType.ComplaintResolved:
      return 'report_problem';
    case NotificationType.VerificationApproved:
    case NotificationType.VerificationRejected:
      return 'verified_user';
    default:
      return 'notifications';
  }
}

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  const days = Math.floor(hrs / 24);
  return `${days}d ago`;
}

function routeFor(n: NotificationResponse): { path: string[]; queryParams?: Record<string, string> } | null {
  // 1. If we have a specific entity view that handles all roles, use it directly!
  if (n.entityType === 'Lease' && n.entityId) {
    return { path: ['/leases', n.entityId] };
  }
  if (n.entityType === 'Property' && n.entityId) {
    return { path: ['/properties', n.entityId] };
  }

  // 2. Otherwise, route to the correct dashboard tab based on who the notification is INTENDED for.
  switch (n.type as NotificationType) {
    // ---- OWNER-FACING NOTIFICATIONS ----
    case NotificationType.ApplicationReceived:
      return { path: ['/owner/dashboard'], queryParams: { tab: 'applications' } };
    case NotificationType.ComplaintRaised:
      return { path: ['/owner/dashboard'], queryParams: { tab: 'complaints' } };
    case NotificationType.PropertyApproved:
    case NotificationType.PropertyRejected:
      return { path: ['/owner/dashboard'], queryParams: { tab: 'properties' } };
    case NotificationType.PaymentReceived:
      return { path: ['/owner/dashboard'], queryParams: { tab: 'overview' } };

    // ---- ADMIN-FACING NOTIFICATIONS ----
    case NotificationType.LeasePendingAdminApproval:
      return { path: ['/admin/dashboard'], queryParams: { tab: 'leases' } };

    // ---- TENANT-FACING NOTIFICATIONS ----
    case NotificationType.ApplicationApproved:
    case NotificationType.ApplicationRejected:
      return { path: ['/dashboard'], queryParams: { tab: 'applications' } };
    case NotificationType.RentDue:
    case NotificationType.PaymentFailed:
      return { path: ['/dashboard'], queryParams: { tab: 'bills' } };
    case NotificationType.ComplaintUpdated:
    case NotificationType.ComplaintResolved:
      return { path: ['/dashboard'], queryParams: { tab: 'complaints' } };
    case NotificationType.VerificationApproved:
    case NotificationType.VerificationRejected:
      return { path: ['/dashboard'], queryParams: { tab: 'verifications' } };

    // ---- AMBIGUOUS / FALLBACK ----
    // For anything else without an entityId, we make a best-effort guess
    case NotificationType.LeaseCreated:
    case NotificationType.LeaseApprovedByAdmin:
    case NotificationType.LeaseRejectedByAdmin:
    case NotificationType.LeaseVoided:
      return { path: ['/dashboard'], queryParams: { tab: 'leases' } };

    default:
      return null;
  }
}

@Component({
  selector: 'app-notification-panel',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, MatDialogModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './notification-panel.component.html',
  styleUrl: './notification-panel.component.css',
})
export class NotificationPanelComponent {
  readonly notifications = input.required<NotificationResponse[]>();
  readonly markAllRead = output<void>();

  private readonly dialog = inject(MatDialog);

  readonly hasUnread = computed(() => this.notifications().some(n => !n.isRead));

  // Pre-compute per-item values so the template reads signals, not function calls.
  readonly enriched = computed(() =>
    this.notifications().map(n => ({
      ...n,
      _icon: iconForType(n.type),
      _timeAgo: timeAgo(n.createdAt),
    }))
  );

  onClickNotification(n: NotificationResponse, event: MouseEvent): void {
    event.stopPropagation();
    const data: NotificationDetailDialogData = {
      notification: n,
      icon: iconForType(n.type),
      timeAgo: timeAgo(n.createdAt),
      route: routeFor(n),
    };
    this.dialog.open(NotificationDetailDialogComponent, {
      data,
      width: '100%',
      maxWidth: '460px',
      panelClass: 'notif-dialog-panel',
      autoFocus: false,
    });
  }

  onMarkAllRead(): void {
    this.markAllRead.emit();
  }
}
