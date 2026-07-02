import {
  Component, ChangeDetectionStrategy, inject, computed,
  signal, HostListener,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { NotificationPanelComponent } from '../notification-panel/notification-panel.component';

@Component({
  selector: 'app-navbar',
  standalone: true,
  host: { 'style': 'position: fixed; top: 0; left: 0; right: 0; z-index: 1000; display: block;' },
  imports: [
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatInputModule,
    MatFormFieldModule,
    MatDividerModule,
    NotificationPanelComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  readonly authService = inject(AuthService);
  readonly notificationService = inject(NotificationService);
  // Injecting SignalRService triggers its constructor effect, which auto-connects on login
  private readonly _signalR = inject(SignalRService);
  private readonly router = inject(Router);

  readonly panelOpen = signal(false);

  readonly isSearchVisible = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => {
        const url = e.urlAfterRedirects.split('?')[0];
        return url === '/' || url === '/properties';
      })
    ),
    { initialValue: this.router.url.split('?')[0] === '/' || this.router.url.split('?')[0] === '/properties' }
  );

  readonly user     = this.authService.currentUser;
  readonly isOwner  = this.authService.isOwner;
  readonly initials = computed(() => {
    const n = this.authService.currentUser()?.fullName ?? '';
    return n.split(' ').map((w: string) => w[0]).join('').slice(0, 2).toUpperCase();
  });
  readonly isKycApproved = computed(() => this.authService.currentUser()?.isKycVerified ?? false);

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.panelOpen()) {
      this.panelOpen.set(false);
    }
  }

  togglePanel(event: MouseEvent): void {
    event.stopPropagation();
    this.panelOpen.update(v => !v);
  }

  onMarkAllRead(): void {
    this.notificationService.markAllRead().subscribe();
  }

  doSearch(value: string): void {
    const q = value.trim();
    this.router.navigate(['/properties'], q ? { queryParams: { state: q } } : {});
  }

  logout() {
    this.authService.logout();
  }
}
