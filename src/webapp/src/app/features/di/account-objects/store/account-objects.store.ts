import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { AccountObjectsApiService } from '../services/account-objects-api.service';
import { AccountObjectListItem } from '../../models/master-data.models';
import { CreateAccountObjectDto, UpdateAccountObjectDto } from '../models/account-object-request.models';

export interface AccountObjectsFilters {
  typeFilter: number;
  status: string;
  search: string;
}

export interface AccountObjectsPagination {
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface AccountObjectsState {
  items: AccountObjectListItem[];
  selectedId: string | null;
  formMode: 'view' | 'edit' | 'create' | null;
  loading: boolean;
  saving: boolean;
  filters: AccountObjectsFilters;
  pagination: AccountObjectsPagination;
  error: string | null;
}

const initialState: AccountObjectsState = {
  items: [],
  selectedId: null,
  formMode: null,
  loading: false,
  saving: false,
  filters: { typeFilter: 0, status: 'all', search: '' },
  pagination: { page: 1, pageSize: 25, totalCount: 0 },
  error: null,
};

export const AccountObjectsStore = signalStore(
  withState(initialState),

  withComputed((state) => ({
    hasSelection: computed(() => state.selectedId() !== null),
    selectedCount: computed(() => (state.selectedId() ? 1 : 0)),
  })),

  withMethods((store) => {
    const api = inject(AccountObjectsApiService);

    async function loadList(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const result = await firstValueFrom(
          api.getList(store.filters(), store.pagination().page, store.pagination().pageSize)
        );
        patchState(store, {
          items: result.items,
          loading: false,
          pagination: {
            ...store.pagination(),
            totalCount: result.totalCount,
          },
        });
      } catch (err: any) {
        patchState(store, {
          loading: false,
          error: err?.message ?? 'Không thể tải danh sách đối tượng',
        });
      }
    }

    return {
      loadList,

      setFilters(partial: Partial<AccountObjectsFilters>): void {
        patchState(store, {
          filters: { ...store.filters(), ...partial },
          pagination: { ...store.pagination(), page: 1 },
        });
        loadList();
      },

      setPage(page: number): void {
        patchState(store, {
          pagination: { ...store.pagination(), page },
        });
        loadList();
      },

      selectItem(id: string | null): void {
        patchState(store, { selectedId: id });
      },

      openCreate(): void {
        patchState(store, { formMode: 'create', selectedId: null });
      },

      openEdit(id: string): void {
        patchState(store, { formMode: 'edit', selectedId: id });
      },

      closeForm(): void {
        patchState(store, { formMode: null });
      },

      async createItem(dto: CreateAccountObjectDto): Promise<void> {
        patchState(store, { saving: true, error: null });
        try {
          await firstValueFrom(api.create(dto));
          patchState(store, { saving: false, formMode: null });
          await loadList();
        } catch (err: any) {
          patchState(store, {
            saving: false,
            error: err?.message ?? 'Không thể tạo đối tượng',
          });
          throw err;
        }
      },

      async updateItem(id: string, dto: UpdateAccountObjectDto): Promise<void> {
        patchState(store, { saving: true, error: null });
        try {
          await firstValueFrom(api.update(id, dto));
          patchState(store, { saving: false, formMode: null });
          await loadList();
        } catch (err: any) {
          patchState(store, {
            saving: false,
            error: err?.message ?? 'Không thể cập nhật đối tượng',
          });
          throw err;
        }
      },

      async deleteItem(id: string): Promise<void> {
        patchState(store, { loading: true, error: null });
        try {
          await firstValueFrom(api.delete(id));
          patchState(store, { loading: false, selectedId: null });
          await loadList();
        } catch (err: any) {
          patchState(store, {
            loading: false,
            error: err?.message ?? 'Không thể xóa đối tượng',
          });
          throw err;
        }
      },
    };
  })
);
