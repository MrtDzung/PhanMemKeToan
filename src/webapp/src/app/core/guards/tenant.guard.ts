import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../stores/auth.store';

export const tenantGuard: CanActivateFn = (_route, _state) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  const user = authStore.currentUser();
  console.log('[tenantGuard] currentUser:', JSON.stringify(user ? { id: user.id, tenantId: user.tenantId, fullName: user.fullName } : null));
  if (user?.tenantId) {
    return true;
  }

  // Authenticated but no tenant — redirect to a safe error page or dashboard
  return router.createUrlTree(['/dashboard']);
};
