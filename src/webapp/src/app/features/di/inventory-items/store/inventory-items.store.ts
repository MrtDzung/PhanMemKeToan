import { computed, inject } from '@angular/core';
import { signalStore, withState, withComputed, withMethods, patchState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { InventoryItemsApiService } from '../services/inventory-items-api.service';
import {
  CategoryTreeNode,
  InventoryItemListItem,
  CreateInventoryItemDto,
  UpdateInventoryItemDto,
} from '../../models/master-data.models';

export interface InventoryItemsFilters {
  itemType: number;
  search: string;
  isActive: boolean | null;
  categoryId: string | null;
}

export interface InventoryItemsPagination {
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface InventoryItemsState {
  items: InventoryItemListItem[];
  categories: CategoryTreeNode[];
  selectedCategoryId: string | null;
  selectedId: string | null;
  formMode: 'create' | 'edit' | null;
  loading: boolean;
  saving: boolean;
  filters: InventoryItemsFilters;
  pagination: InventoryItemsPagination;
  error: string | null;
}

const initialState: InventoryItemsState = {
  items: [],
  categories: [],
  selectedCategoryId: null,
  selectedId: null,
  formMode: null,
  loading: false,
  saving: false,
  filters: { itemType: 0, search: '', isActive: null, categoryId: null },
  pagination: { page: 1, pageSize: 25, totalCount: 0 },
  error: null,
};

export const InventoryItemsStore = signalStore(
  withState(initialState),

  withComputed((state) => ({
    hasSelection: computed(() => state.selectedId() !== null),
    selectedCount: computed(() => (state.selectedId() ? 1 : 0)),
  })),

  withMethods((store) => {
    const api = inject(InventoryItemsApiService);

    async function loadList(): Promise<void> {
      patchState(store, { loading: true, error: null });
      try {
        const filters = store.filters();
        const apiFilters: any = {};
        if (filters.itemType !== 0) apiFilters.itemType = filters.itemType;
        if (filters.search.trim()) apiFilters.search = filters.search;
        if (filters.isActive !== null) apiFilters.isActive = filters.isActive;
        if (filters.categoryId) apiFilters.categoryId = filters.categoryId;

        const result = await firstValueFrom(
          api.getList(apiFilters, store.pagination().page, store.pagination().pageSize)
        );
        patchState(store, {
          items: result.items,
          loading: false,
          pagination: { ...store.pagination(), totalCount: result.totalCount },
        });
      } catch (err: any) {
        patchState(store, {
          loading: false,
          error: err?.message ?? 'Không thể tải danh sách hàng tồn kho',
        });
      }
    }

    return {
      loadList,

      async loadCategories(): Promise<void> {
        try {
          const categories = await firstValueFrom(api.getCategories());
          patchState(store, { categories });
        } catch {
          // Categories load failure is non-critical
        }
      },

      setCategory(id: string | null): void {
        patchState(store, {
          selectedCategoryId: id,
          filters: { ...store.filters(), categoryId: id },
          pagination: { ...store.pagination(), page: 1 },
        });
        loadList();
      },

      setFilters(partial: Partial<InventoryItemsFilters>): void {
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

      async createItem(dto: CreateInventoryItemDto): Promise<void> {
        patchState(store, { saving: true, error: null });
        try {
          await firstValueFrom(api.create(dto));
          patchState(store, { saving: false, formMode: null });
          await loadList();
        } catch (err: any) {
          patchState(store, {
            saving: false,
            error: err?.message ?? 'Không thể tạo hàng tồn kho',
          });
          throw err;
        }
      },

      async updateItem(id: string, dto: UpdateInventoryItemDto): Promise<void> {
        patchState(store, { saving: true, error: null });
        try {
          await firstValueFrom(api.update(id, dto));
          patchState(store, { saving: false, formMode: null });
          await loadList();
        } catch (err: any) {
          patchState(store, {
            saving: false,
            error: err?.message ?? 'Không thể cập nhật hàng tồn kho',
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
            error: err?.message ?? 'Không thể xóa hàng tồn kho',
          });
          throw err;
        }
      },
    };
  })
);
