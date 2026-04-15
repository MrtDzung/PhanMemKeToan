import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { tenantGuard } from './core/guards/tenant.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent),
    title: 'Đăng nhập'
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/shell/shell.component').then(m => m.ShellComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
        title: 'Trang chủ'
      },
      {
        path: 'system',
        canActivate: [tenantGuard],
        children: [
          {
            path: 'users',
            loadComponent: () =>
              import('./features/system/users/user-list/user-list.component').then(m => m.UserListComponent),
            title: 'Người dùng'
          },
          {
            path: 'roles',
            loadComponent: () =>
              import('./features/system/roles/role-list/role-list.component').then(m => m.RoleListComponent),
            title: 'Vai trò'
          }
        ]
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];

