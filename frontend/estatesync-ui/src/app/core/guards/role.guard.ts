import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { TokenService } from '@core/services/auth/token.service';
import { AuthService } from '@core/services/auth/auth.service';
import { getRoleHome } from './role-home.util';

export function roleGuard(...roles: string[]): CanActivateFn {
  return () => {
    const tokenService = inject(TokenService);
    const authService = inject(AuthService);
    const router = inject(Router);

    // Valid token: check role immediately.
    if (tokenService.isAuthenticated()) {
      return checkRoles(roles, tokenService, router);
    }

    // Token absent or expired — attempt a silent refresh before redirecting to login.
    return authService.bootstrap().pipe(
      map(() => {
        if (!tokenService.isAuthenticated()) return router.createUrlTree(['/auth/login']);
        return checkRoles(roles, tokenService, router);
      })
    );
  };
}

function checkRoles(roles: string[], tokenService: TokenService, router: Router): true | ReturnType<Router['createUrlTree']> {
  const userRoles = tokenService.decodeRoles();
  if (roles.some(r => userRoles.includes(r))) return true;
  return router.createUrlTree([getRoleHome(userRoles)]);
}
