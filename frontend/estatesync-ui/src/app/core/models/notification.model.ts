import { NotificationType } from './enums';

export interface NotificationResponse {
  notificationId: string;
  type: NotificationType;
  title: string;
  message: string;
  entityId?: string;
  entityType?: string;
  isRead: boolean;
  createdAt: string;
}
