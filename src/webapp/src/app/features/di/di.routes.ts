import { Routes } from '@angular/router';
import { accountTreeDirtyGuard } from './account-tree/guards/account-tree-dirty.guard';

export const diRoutes: Routes = [
  {
    path: 'accounts',
    loadComponent: () =>
      import('./account-tree/account-tree-page.component').then(
        (m) => m.AccountTreePageComponent
      ),
    title: 'Hệ thống Tài khoản Kế toán',
    canDeactivate: [accountTreeDirtyGuard],
  },
  {
    path: 'setup/currencies',
    loadComponent: () =>
      import('./setup/currencies/currencies-page.component').then(
        (m) => m.CurrenciesPageComponent
      ),
    title: 'Tiền tệ',
  },
  {
    path: 'setup/units',
    loadComponent: () =>
      import('./setup/units/units-page.component').then(
        (m) => m.UnitsPageComponent
      ),
    title: 'Đơn vị tính',
  },
  {
    path: 'setup/warehouses',
    loadComponent: () =>
      import('./setup/warehouses/warehouses-page.component').then(
        (m) => m.WarehousesPageComponent
      ),
    title: 'Kho hàng',
  },
  {
    path: 'setup/departments',
    loadComponent: () =>
      import('./setup/departments/departments-page.component').then(
        (m) => m.DepartmentsPageComponent
      ),
    title: 'Phòng ban',
  },
  {
    path: 'setup/expense-items',
    loadComponent: () =>
      import('./setup/expense-items/expense-items-page.component').then(
        (m) => m.ExpenseItemsPageComponent
      ),
    title: 'Khoản mục chi phí',
  },
  {
    path: 'account-objects',
    loadComponent: () =>
      import('./account-objects/account-objects-page.component').then(
        (m) => m.AccountObjectsPageComponent
      ),
    title: 'Đối tượng kế toán',
  },
  {
    path: 'inventory-items',
    loadComponent: () =>
      import('./inventory-items/inventory-items-page.component').then(
        (m) => m.InventoryItemsPageComponent
      ),
    title: 'Hàng tồn kho',
  },
  {
    path: '',
    redirectTo: 'accounts',
    pathMatch: 'full',
  },
];
