import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
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

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatInputModule,
    MatFormFieldModule,
    MatDividerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  readonly authService = inject(AuthService);
  private readonly router = inject(Router);

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

  get user() { return this.authService.currentUser(); }
  get initials() {
    const n = this.user?.fullName ?? '';
    return n.split(' ').map((w: string) => w[0]).join('').slice(0, 2).toUpperCase();
  }
  get isOwner() { return this.authService.isOwner(); }
  get isAdmin() { return this.authService.isAdmin(); }
  get isKycApproved() { return this.user?.isKycVerified ?? false; }

  doSearch(value: string): void {
    const q = value.trim();
    this.router.navigate(['/properties'], q ? { queryParams: { state: q } } : {});
  }

  logout() {
    this.authService.logout();
  }
}
