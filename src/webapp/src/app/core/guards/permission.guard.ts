import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../stores/auth.store';

/**
 * Route-level permission guard.
 * Usage in route config:
 *   { path: 'di', canActivate: [permissionGuard('DI')], ... }
 *
 * Checks if the current user has at least one permission starting with the given module prefix.
 */
export function permissionGuard(modulePrefix: string): CanActivateFn {
  return (_route, _state) => {
    const authStore = inject(AuthStore);
    const router = inject(Router);

    const user = authStore.currentUser();
    if (!user?.permissions?.length) {
      return router.createUrlTree(['/dashboard']);
    }

    const hasModuleAccess = user.permissions.some(
      (p) => p.startsWith(`${modulePrefix}.`)
    );

    return hasModuleAccess || router.createUrlTree(['/dashboard']);
  };
}
