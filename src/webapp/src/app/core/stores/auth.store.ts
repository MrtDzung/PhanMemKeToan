import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState } from '@ngrx/signals';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { CurrentUser } from '../models/auth.models';

export interface AuthState {
  accessToken: string | null;
  tokenExpiresAt: string | null;
  currentUser: CurrentUser | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  accessToken: null,
  tokenExpiresAt: null,
  currentUser: null,
  isLoading: false,
  error: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((state) => ({
    isAuthenticated: computed(() => state.accessToken() !== null),
    hasPermission: computed(() => (permissionCode: string) =>
      state.currentUser()?.permissions.includes(permissionCode) ?? false
    ),
    hasRole: computed(() => (roleName: string) =>
      state.currentUser()?.roles.includes(roleName) ?? false
    ),
  })),
  withMethods((store, authService = inject(AuthService), router = inject(Router)) => ({
    async login(email: string, password: string, rememberMe: boolean): Promise<void> {
      patchState(store, { isLoading: true, error: null });
      try {
        const result = await authService.login(email, password, rememberMe);
        patchState(store, {
          accessToken: result.accessToken,
          tokenExpiresAt: result.expiresAt,
          isLoading: false,
        });
        await store.loadCurrentUser();
        await router.navigate(['/dashboard']);
      } catch (err: unknown) {
        const message = (err as { error?: { detail?: string } })?.error?.detail ?? 'Đăng nhập thất bại';
        patchState(store, {
          isLoading: false,
          error: message,
        });
        throw err;
      }
    },

    async loadCurrentUser(): Promise<void> {
      try {
        const user = await authService.getCurrentUser();
        patchState(store, { currentUser: user });
      } catch {
        patchState(store, { accessToken: null, currentUser: null });
      }
    },

    async refreshToken(): Promise<string> {
      const result = await authService.refreshToken();
      patchState(store, {
        accessToken: result.accessToken,
        tokenExpiresAt: result.expiresAt,
      });
      return result.accessToken;
    },

    async logout(): Promise<void> {
      try {
        await authService.logout();
      } finally {
        patchState(store, initialState);
        await router.navigate(['/login']);
      }
    },

    setTokenFromStorage(token: string, expiresAt: string): void {
      patchState(store, { accessToken: token, tokenExpiresAt: expiresAt });
    },
  }))
);
