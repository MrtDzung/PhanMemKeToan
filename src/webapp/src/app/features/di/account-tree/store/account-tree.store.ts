import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { TreeNode } from 'primeng/api';
import { AccountApiService } from '../services/account-api.service';
import { AccountTreeBuilderService } from '../services/account-tree-builder.service';
import { AuthStore } from '../../../../core/stores/auth.store';
import {
  AccountTreeNodeDto,
  AccountDetailDto,
  CreateAccountCommand,
  UpdateAccountCommand,
  ImportCoaResultDto,
} from '../../models/account.models';

export type FormMode = 'view' | 'create' | 'edit';
export type StatusFilter = 'all' | 'active' | 'inactive';

export interface AccountTreeState {
  accounts: AccountTreeNodeDto[];
  selectedAccountId: string | null;
  selectedAccountDetail: AccountDetailDto | null;
  formMode: FormMode;
  loading: boolean;
  saving: boolean;
  searchQuery: string;
  statusFilter: StatusFilter;
  expandedNodeIds: Set<string>;
  error: string | null;
  parentIdForCreate: string | null;
}

const initialState: AccountTreeState = {
  accounts: [],
  selectedAccountId: null,
  selectedAccountDetail: null,
  formMode: 'view',
  loading: false,
  saving: false,
  searchQuery: '',
  statusFilter: 'all',
  expandedNodeIds: new Set(),
  error: null,
  parentIdForCreate: null,
};

function filterByStatus(accounts: AccountTreeNodeDto[], filter: StatusFilter): AccountTreeNodeDto[] {
  if (filter === 'all') return accounts;

  // Get matching leaf accounts first
  const matchingIds = new Set<string>();
  for (const acc of accounts) {
    const matches = filter === 'active' ? !acc.inactive : acc.inactive;
    if (matches) {
      matchingIds.add(acc.accountId);
      // include ancestors so the tree can be built correctly
      let parentId = acc.parentId;
      while (parentId) {
        matchingIds.add(parentId);
        const parent = accounts.find((a) => a.accountId === parentId);
        parentId = parent?.parentId ?? null;
      }
    }
  }
  return accounts.filter((a) => matchingIds.has(a.accountId));
}

function loadExpandedFromStorage(tenantId: string): Set<string> {
  try {
    const raw = localStorage.getItem(`account-tree-expanded-${tenantId}`);
    return raw ? new Set<string>(JSON.parse(raw)) : new Set<string>();
  } catch {
    return new Set<string>();
  }
}

function saveExpandedToStorage(tenantId: string, ids: Set<string>): void {
  try {
    localStorage.setItem(`account-tree-expanded-${tenantId}`, JSON.stringify([...ids]));
  } catch {
    // ignore storage errors
  }
}

export const AccountTreeStore = signalStore(
  withState(initialState),
  withComputed((state, builderService = inject(AccountTreeBuilderService)) => ({
    filteredAccounts: computed(() => {
      let accounts = state.accounts();
      const query = state.searchQuery();
      const filter = state.statusFilter();

      accounts = filterByStatus(accounts, filter);

      if (query.trim()) {
        accounts = builderService.filterTree(accounts, query);
      }

      return accounts;
    }),
    treeNodes: computed((): TreeNode[] => {
      let accounts = state.accounts();
      const query = state.searchQuery();
      const filter = state.statusFilter();

      accounts = filterByStatus(accounts, filter);

      if (query.trim()) {
        accounts = builderService.filterTree(accounts, query);
      }

      return builderService.buildTree(accounts, state.expandedNodeIds());
    }),
    selectedAccount: computed(() => {
      const id = state.selectedAccountId();
      return state.accounts().find((a) => a.accountId === id) ?? null;
    }),
  })),
  withMethods(
    (
      store,
      apiService = inject(AccountApiService),
      authStore = inject(AuthStore)
    ) => ({
      async loadTree(): Promise<void> {
        patchState(store, { loading: true, error: null });
        try {
          const res = await firstValueFrom(apiService.getAccountTree(true, 'flat'));
          const tenantId = authStore.currentUser()?.tenantId ?? 'default';
          const expandedNodeIds = loadExpandedFromStorage(tenantId);
          patchState(store, { accounts: res.data, loading: false, expandedNodeIds });
        } catch (err: unknown) {
          const msg = (err as { error?: { detail?: string } })?.error?.detail ?? 'Không thể tải danh mục tài khoản';
          patchState(store, { loading: false, error: msg });
        }
      },

      async selectAccount(id: string): Promise<void> {
        patchState(store, { selectedAccountId: id, formMode: 'view', saving: false });
        try {
          const res = await firstValueFrom(apiService.getAccountById(id));
          patchState(store, { selectedAccountDetail: res.data });
        } catch {
          patchState(store, { selectedAccountDetail: null });
        }
      },

      setFormMode(mode: FormMode): void {
        patchState(store, { formMode: mode });
      },

      setCreateMode(parentId: string | null): void {
        patchState(store, { formMode: 'create', parentIdForCreate: parentId, selectedAccountId: null, selectedAccountDetail: null });
      },

      async createAccount(cmd: CreateAccountCommand): Promise<void> {
        patchState(store, { saving: true, error: null });
        try {
          await firstValueFrom(apiService.createAccount(cmd));
          patchState(store, { saving: false, formMode: 'view', selectedAccountId: null });
          await this.loadTree();
        } catch (err: unknown) {
          const msg = (err as any)?.error?.message ?? (err as any)?.error?.detail ?? 'Tạo tài khoản thất bại';
          patchState(store, { saving: false, error: msg });
          throw err;
        }
      },

      async updateAccount(id: string, cmd: UpdateAccountCommand): Promise<void> {
        patchState(store, { saving: true, error: null });
        try {
          await firstValueFrom(apiService.updateAccount(id, cmd));
          patchState(store, { saving: false, formMode: 'view' });
          await this.loadTree();
          await this.selectAccount(id);
        } catch (err: unknown) {
          const msg = (err as any)?.error?.message ?? (err as any)?.error?.detail ?? 'Cập nhật tài khoản thất bại';
          patchState(store, { saving: false, error: msg });
          throw err;
        }
      },

      async deleteAccount(id: string, rowVersion: number): Promise<void> {
        patchState(store, { saving: true, error: null });
        try {
          await firstValueFrom(apiService.deleteAccount(id, rowVersion));
          patchState(store, {
            saving: false,
            selectedAccountId: null,
            selectedAccountDetail: null,
            formMode: 'view',
          });
          await this.loadTree();
        } catch (err: unknown) {
          const msg = (err as any)?.error?.message ?? (err as any)?.error?.detail ?? 'Xóa tài khoản thất bại';
          patchState(store, { saving: false, error: msg });
          throw err;
        }
      },

      async importCoa(
        standard: 'TT99' | 'TT133',
        conflictResolution: 'skip' | 'overwrite'
      ): Promise<ImportCoaResultDto> {
        patchState(store, { saving: true, error: null });
        try {
          const res = await firstValueFrom(apiService.importCoa(standard, conflictResolution));
          patchState(store, { saving: false });
          await this.loadTree();
          return res.data;
        } catch (err: unknown) {
          const msg = (err as { error?: { detail?: string } })?.error?.detail ?? 'Nhập danh mục thất bại';
          patchState(store, { saving: false, error: msg });
          throw err;
        }
      },

      setSearchQuery(q: string): void {
        patchState(store, { searchQuery: q });
      },

      clearSelection(): void {
        patchState(store, { selectedAccountId: null, selectedAccountDetail: null, formMode: 'view' });
      },

      setStatusFilter(f: StatusFilter): void {
        patchState(store, { statusFilter: f });
      },

      toggleExpanded(id: string): void {
        const current = new Set(store.expandedNodeIds());
        if (current.has(id)) {
          current.delete(id);
        } else {
          current.add(id);
        }
        const tenantId = authStore.currentUser()?.tenantId ?? 'default';
        saveExpandedToStorage(tenantId, current);
        patchState(store, { expandedNodeIds: current });
      },

      expandAll(): void {
        const allParentIds = new Set(
          store.accounts().filter((a) => a.isParent).map((a) => a.accountId)
        );
        const tenantId = authStore.currentUser()?.tenantId ?? 'default';
        saveExpandedToStorage(tenantId, allParentIds);
        patchState(store, { expandedNodeIds: allParentIds });
      },

      collapseAll(): void {
        const empty = new Set<string>();
        const tenantId = authStore.currentUser()?.tenantId ?? 'default';
        saveExpandedToStorage(tenantId, empty);
        patchState(store, { expandedNodeIds: empty });
      },
    })
  )
);
