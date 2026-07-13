import {
  Component, ChangeDetectionStrategy, inject, computed,
  signal, HostListener, DestroyRef,
} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Router, NavigationEnd } from '@angular/router';
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, map, Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { AuthService } from '@core/services/auth/auth.service';
import { NotificationService } from '@core/services/shared/notification.service';
import { SignalRService } from '@core/services/shared/signalr.service';
import { NotificationPanelComponent } from '../notification-panel/notification-panel.component';
import { getInitials } from '../../../core/utils/format.utils';

@Component({
  selector: 'app-navbar',
  standalone: true,
  host: { 'style': 'position: fixed; top: 0; left: 0; right: 0; z-index: 1000; display: block;' },
  imports: [
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatInputModule,
    MatFormFieldModule,
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
  readonly mobileMenuOpen = signal(false);

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
  readonly initials = computed(() => getInitials(this.authService.currentUser()?.fullName ?? ''));
  readonly isKycApproved = this.authService.isKycVerified;

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

  // Fire-and-forget: navbar lives for the app lifetime and the one-shot
  // HTTP observable completes on its own, so no cleanup operator is needed.
  onMarkAllRead(): void {
    this.notificationService.markAllRead().subscribe();
  }

  private readonly searchTerm$ = new Subject<string>();
  private readonly _searchSub = this.searchTerm$.pipe(
    debounceTime(400),
    map(v => v.trim().replace(/\s+/g, ' ')),
    distinctUntilChanged(),
    takeUntilDestroyed(inject(DestroyRef)),
  ).subscribe(q => this.navigateToResults(q));

  onSearchInput(value: string): void {
    this.searchTerm$.next(value);
  }

  doSearch(value: string): void {
    this.navigateToResults(value.trim().replace(/\s+/g, ' '));
  }

  private navigateToResults(q: string): void {
    this.router.navigate(['/properties'], q ? { queryParams: { search: q } } : {});
  }

  toggleMobileMenu(): void { this.mobileMenuOpen.update(v => !v); }
  closeMobileMenu(): void  { this.mobileMenuOpen.set(false); }

  openMobileNotifications(): void {
    this.closeMobileMenu();
    setTimeout(() => this.panelOpen.set(true), 260);
  }

  logout() {
    this.closeMobileMenu();
    this.authService.logout();
  }
}
