import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { NavbarComponent } from '../navbar/navbar.component';
import { FooterComponent } from './footer.component';
import { ChatbotComponent } from '../chatbot/chatbot.component';

const DASHBOARD_PREFIXES = [
  '/dashboard',
  '/owner/dashboard',
  '/kyc',
  '/leases',
  '/profile',
  '/properties/',
];

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent, ChatbotComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './main-layout.component.html'
})
export class MainLayoutComponent {
  private readonly router = inject(Router);

  private readonly navEnd = toSignal(
    this.router.events.pipe(filter(e => e instanceof NavigationEnd))
  );

  readonly showFooter = computed(() => {
    this.navEnd();
    const url = this.router.url.split('?')[0];
    return !DASHBOARD_PREFIXES.some(prefix => url.startsWith(prefix));
  });
}
