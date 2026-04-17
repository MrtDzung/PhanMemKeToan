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
    path: '',
    redirectTo: 'accounts',
    pathMatch: 'full',
  },
];
