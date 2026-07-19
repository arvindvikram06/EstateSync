import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { NavbarComponent } from '../navbar/navbar.component';
import { FooterComponent } from './footer.component';
import { ChatbotComponent } from '../chatbot/chatbot.component';
import { AuthService } from '@core/services/auth/auth.service';

const DASHBOARD_PREFIXES = [
  '/dashboard',
  '/owner/dashboard',
  '/kyc',
  '/leases',
  '/profile',
];

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent, ChatbotComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.css'
})
export class MainLayoutComponent {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  readonly isLoggedIn = this.authService.isLoggedIn;

  private readonly navEnd = toSignal(
    this.router.events.pipe(filter(e => e instanceof NavigationEnd))
  );

  readonly showFooter = computed(() => {
    this.navEnd();
    const url = this.router.url.split('?')[0];
    const isPropertyForm = url === '/properties/create' || /^\/properties\/[^/]+\/edit$/.test(url);
    return !isPropertyForm && !DASHBOARD_PREFIXES.some(prefix => url.startsWith(prefix));
  });
}
