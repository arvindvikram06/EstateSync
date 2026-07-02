import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { roleGuard } from './role.guard';
import { TokenService } from '../services/token.service';

describe('roleGuard', () => {
  const loginTree = { __tag: 'login' } as unknown as UrlTree;
  const homeTree = { __tag: 'home' } as unknown as UrlTree;

  let tokenService: { isAuthenticated: ReturnType<typeof vi.fn>; decodeRoles: ReturnType<typeof vi.fn> };
  let router: { createUrlTree: ReturnType<typeof vi.fn> };

  function run(...roles: string[]) {
    return TestBed.runInInjectionContext(() => roleGuard(...roles)(null as never, null as never));
  }

  beforeEach(() => {
    tokenService = { isAuthenticated: vi.fn(), decodeRoles: vi.fn() };
    router = {
      createUrlTree: vi.fn((commands: string[]) =>
        commands[0] === '/auth/login' ? loginTree : homeTree
      ),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: TokenService, useValue: tokenService },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('redirects to login when not authenticated', () => {
    tokenService.isAuthenticated.mockReturnValue(false);
    expect(run('Admin')).toBe(loginTree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/auth/login']);
  });

  it('allows activation when a required role is present', () => {
    tokenService.isAuthenticated.mockReturnValue(true);
    tokenService.decodeRoles.mockReturnValue(['Tenant', 'Admin']);
    expect(run('Admin')).toBe(true);
  });

  it('allows when any one of several required roles matches', () => {
    tokenService.isAuthenticated.mockReturnValue(true);
    tokenService.decodeRoles.mockReturnValue(['Owner']);
    expect(run('Owner', 'Admin')).toBe(true);
  });

  it('redirects home when authenticated but missing the role', () => {
    tokenService.isAuthenticated.mockReturnValue(true);
    tokenService.decodeRoles.mockReturnValue(['Tenant']);
    expect(run('Admin')).toBe(homeTree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/']);
  });
});
