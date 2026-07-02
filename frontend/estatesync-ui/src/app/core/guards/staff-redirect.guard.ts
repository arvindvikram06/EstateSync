import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenService } from '../services/token.service';
import { getRoleHome } from './role-home.util';

export const staffRedirectGuard: CanActivateFn = () => {
  const tokenService = inject(TokenService);
  const router = inject(Router);

  if (!tokenService.isAuthenticated()) return true;

  const roles = tokenService.decodeRoles();
  if (roles.includes('Admin') || roles.includes('Verifier')) {
    return router.createUrlTree([getRoleHome(roles)]);
  }

  return true;
};
