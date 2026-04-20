import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../stores/auth.store';

export const authGuard: CanActivateFn = async (_route, state) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (authStore.isAuthenticated()) {
    return true;
  }

  // Try to load user using existing cookie (refresh token)
  try {
    const newToken = await authStore.refreshToken();
    if (newToken) {
      await authStore.loadCurrentUser();
      return true;
    }
  } catch {
    // Refresh failed � redirect to login
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const tempTokenGuard: CanActivateFn = (_route, _state) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (authStore.hasTempToken()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};
