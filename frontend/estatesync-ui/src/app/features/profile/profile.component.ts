import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '@core/services/auth/auth.service';
import { UserService } from '@core/services/admin/user.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { formatDate, getInitials } from '../../core/utils/format.utils';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [MatIconModule, MatProgressSpinnerModule, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile.component.html',
})
export class ProfileComponent {
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);

  // Fresh profile from the API; null while loading
  readonly profile = toSignal(
    this.userService.getProfile().pipe(map(res => res.data)),
    { initialValue: null },
  );

  readonly roles = computed(() => this.authService.currentUser()?.roles ?? []);
  readonly initials = computed(() => getInitials(this.profile()?.fullName ?? ''));

  readonly formatDate = formatDate;
}
