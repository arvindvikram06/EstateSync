import { Injectable, inject, effect, OnDestroy, NgZone } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../../environments/environment';
import { NotificationService } from './notification.service';
import { TokenService } from '@core/services/auth/token.service';
import { AuthService } from '@core/services/auth/auth.service';
import { NotificationResponse } from '../../models/notification.model';
import { DataRefreshService, refreshDomainsFor } from './data-refresh.service';

@Injectable({ providedIn: 'root' })
export class SignalRService implements OnDestroy {
  private readonly notificationService = inject(NotificationService);
  private readonly tokenService = inject(TokenService);
  private readonly authService = inject(AuthService);
  private readonly dataRefresh = inject(DataRefreshService);
  private readonly ngZone = inject(NgZone);

  private connection: signalR.HubConnection | null = null;

  constructor() {
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

  private readonly onNotification = (notification: NotificationResponse): void => {
    this.ngZone.run(() => {
      this.notificationService.addNotification(notification);
      for (const domain of refreshDomainsFor(notification.type, notification.entityType)) {
        this.dataRefresh.trigger(domain);
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
            try {
              const res = await firstValueFrom(this.authService.refreshToken());
              token = res.data.accessToken;
            } catch {
              token = null;
            }
          }
          if (!token) {
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

    this.connection.onreconnected(() => {
      this.ngZone.run(() => this.notificationService.fetchAllIfStale());
    });

    this.connection
      .start()
      .then(() => {
        this.notificationService.fetchAllIfStale();
      })
      .catch(() => { /* hub retries via withAutomaticReconnect */ });
  }

  private disconnect(): void {
    if (this.connection) {
      this.connection.off('ReceiveNotification', this.onNotification);
      this.connection.stop().catch(() => { /* already closed */ });
      this.connection = null;
    }
  }
}
