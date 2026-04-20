import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState } from '@ngrx/signals';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { CompanyInfo, CurrentUser } from '../models/auth.models';

export interface AuthState {
  accessToken: string | null;
  tokenExpiresAt: string | null;
  currentUser: CurrentUser | null;
  tempToken: string | null;
  companies: CompanyInfo[];
  selectedCompany: CompanyInfo | null;
  rememberMe: boolean;
  isLoading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  accessToken: null,
  tokenExpiresAt: null,
  currentUser: null,
  tempToken: null,
  companies: [],
  selectedCompany: null,
  rememberMe: false,
  isLoading: false,
  error: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((state) => ({
    isAuthenticated: computed(() => state.accessToken() !== null),
    hasTempToken: computed(() => state.tempToken() !== null),
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
          tempToken: result.tempToken,
          companies: result.companies,
          rememberMe: result.rememberMe,
          isLoading: false,
        });

        // Auto-select if only 1 company
        if (result.companies.length === 1) {
          await this.selectCompany(result.companies[0].tenantId);
        } else {
          await router.navigate(['/select-company']);
        }
      } catch (err: unknown) {
        const message = (err as { error?: { detail?: string } })?.error?.detail ?? 'Đăng nhập thất bại';
        patchState(store, {
          isLoading: false,
          error: message,
        });
        throw err;
      }
    },

    async selectCompany(tenantId: string): Promise<void> {
      patchState(store, { isLoading: true, error: null });
      try {
        const tempToken = store.tempToken();
        if (!tempToken) throw new Error('No temp token');

        const result = await authService.selectCompany(tempToken, tenantId, store.rememberMe());
        const selected = store.companies().find(c => c.tenantId === tenantId) ?? null;

        patchState(store, {
          accessToken: result.accessToken,
          tokenExpiresAt: result.expiresAt,
          tempToken: null,
          selectedCompany: selected,
          isLoading: false,
        });

        const user = await authService.getCurrentUser();
        patchState(store, { currentUser: user });
        await router.navigate(['/dashboard']);
      } catch (err: unknown) {
        const message = (err as { error?: { detail?: string } })?.error?.detail ?? 'Không thể chọn công ty';
        patchState(store, { isLoading: false, error: message });
        throw err;
      }
    },

    async switchCompany(targetTenantId: string): Promise<void> {
      patchState(store, { isLoading: true, error: null });
      try {
        const result = await authService.switchCompany(targetTenantId);
        patchState(store, {
          accessToken: result.accessToken,
          tokenExpiresAt: result.expiresAt,
          isLoading: false,
        });

        const user = await authService.getCurrentUser();
        const selected = user.companies?.find(c => c.tenantId === targetTenantId) ?? null;
        patchState(store, {
          currentUser: user,
          companies: user.companies ?? [],
          selectedCompany: selected,
        });
        await router.navigate(['/dashboard']);
      } catch (err: unknown) {
        const message = (err as { error?: { detail?: string } })?.error?.detail ?? 'Không thể chuyển công ty';
        patchState(store, { isLoading: false, error: message });
        throw err;
      }
    },

    async loadCurrentUser(): Promise<void> {
      try {
        const user = await authService.getCurrentUser();
        const selected = user.currentCompany ?? null;
        patchState(store, {
          currentUser: user,
          companies: user.companies ?? [],
          selectedCompany: selected,
        });
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
