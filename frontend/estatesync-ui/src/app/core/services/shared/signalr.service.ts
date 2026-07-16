import { Injectable, inject, effect, OnDestroy, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../../environments/environment';
import { NotificationService } from './notification.service';
import { TokenService } from '@core/services/auth/token.service';
import { AuthService } from '@core/services/auth/auth.service';
import { NotificationResponse } from '../../models/notification.model';
import { KycStore } from '../../stores/kyc.store';
import { PropertyService } from '@core/services/property/property.service';

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private readonly notificationService = inject(NotificationService);
  private readonly tokenService = inject(TokenService);
  private readonly authService = inject(AuthService);
  private readonly kycStore = inject(KycStore);
  private readonly propertyService = inject(PropertyService);
  private readonly ngZone = inject(NgZone);

  private connection: signalR.HubConnection | null = null;

  constructor() {
    // Auto-connect/disconnect when auth state changes
    effect(() => {
      if (this.authService.isLoggedIn()) {
        this.connect();
      } else {
        this.disconnect();
      }
    });
  }

  ngOnDestroy(): void {
    this.disconnect();
  }

  // Stable reference so the handler can be detached on disconnect.
  // Wrapped in NgZone.run() because SignalR callbacks fire outside Angular's zone,
  // which prevents OnPush change detection from seeing signal/store updates.
  private readonly onNotification = (notification: NotificationResponse): void => {
    this.ngZone.run(() => {
      this.notificationService.addNotification(notification);

      switch (notification.type as string) {
        case 'VerificationApproved':
        case 'VerificationRejected':
          this.authService.syncProfile().subscribe();
          this.kycStore.loadExisting();
          break;
        case 'PropertyApproved':
        case 'PropertyRejected':
          this.propertyService.triggerRefreshMyProperties();
          break;
        case 'SiteVisitRequested':
        case 'SiteVisitApproved':
        case 'SiteVisitDeclined':
        case 'SiteVisitCancelled':
          // Supported types that trigger notification badge/popup
          break;
        default:
          if (notification.entityType === 'Property' && (notification.title?.includes('Approved') || notification.title?.includes('Rejected'))) {
            this.propertyService.triggerRefreshMyProperties();
          }
          break;
      }
    });
  };

  private connect(): void {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/notifications`, {
        accessTokenFactory: async () => {
          let token = this.tokenService.getToken();
          if (token && !this.tokenService.isAuthenticated()) {
            // Token is expired, try a quick refresh before passing it to SignalR
            try {
              const { firstValueFrom } = await import('rxjs');
              const res = await firstValueFrom(this.authService.refreshToken());
              token = res.data.accessToken;
            } catch {
              // Refresh failed, token will be cleared by interceptor
              token = null;
            }
          }
          if (!token) {
            // Forcefully stop the infinite retry loop if we are truly logged out
            setTimeout(() => this.disconnect(), 0);
            return '';
          }
          return token;
        },
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: ctx =>
          Math.min(1000 * 2 ** ctx.previousRetryCount, 30000),
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('ReceiveNotification', this.onNotification);

    // Refetch after a reconnect to recover notifications missed while
    // offline, but throttled so a flaky network doesn't hammer the API.
    this.connection.onreconnected(() => {
      this.ngZone.run(() => this.notificationService.fetchAllIfStale());
    });

    this.connection
      .start()
      .then(() => {
        this.notificationService.fetchAllIfStale();
      })
      .catch(() => {
        // Connection failure is non-fatal; hub will retry via withAutomaticReconnect
      });
  }

  private disconnect(): void {
    if (this.connection) {
      this.connection.off('ReceiveNotification', this.onNotification);
      this.connection.stop().catch(() => { /* already closed */ });
      this.connection = null;
    }
  }
}
