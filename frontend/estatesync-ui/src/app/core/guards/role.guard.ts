import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '@core/services/auth/token.service';
import { getRoleHome } from './role-home.util';

export function roleGuard(...roles: string[]): CanActivateFn {
  return () => {
    const tokenService = inject(TokenService);
    const router = inject(Router);

    if (!tokenService.isAuthenticated()) {
      return router.createUrlTree(['/auth/login']);
    }

    const userRoles = tokenService.decodeRoles();
    if (roles.some(r => userRoles.includes(r))) {
      return true;
    }

    // Wrong role — send to the user's own home, not a generic page
    return router.createUrlTree([getRoleHome(userRoles)]);
  };
}
