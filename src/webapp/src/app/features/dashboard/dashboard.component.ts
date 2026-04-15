import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CardModule } from 'primeng/card';
import { AuthStore } from '../../core/stores/auth.store';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, CardModule],
  template: `
    <div class="dashboard-container">
      <h1 class="page-title">Trang chủ</h1>
      <p class="welcome-text">
        Xin chào, <strong>{{ authStore.currentUser()?.fullName }}</strong>
        — {{ authStore.currentUser()?.tenantName }}
      </p>

      <div class="module-grid">
        <p-card header="Hệ thống" styleClass="module-card">
          <p>Quản lý người dùng, vai trò và phân quyền</p>
          <a routerLink="/system/users" class="module-link">Người dùng</a>
          <a routerLink="/system/roles" class="module-link">Vai trò</a>
        </p-card>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container { padding: var(--spacing-lg, 24px); }
    .page-title { font-size: 16px; font-weight: 600; color: var(--text-primary); margin: 0 0 8px; }
    .welcome-text { color: var(--text-secondary); font-size: 13px; margin: 0 0 24px; }
    .module-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; }
    .module-card { height: 100%; }
    .module-link { display: inline-block; margin-right: 12px; color: var(--primary); font-size: 13px; text-decoration: none; }
    .module-link:hover { text-decoration: underline; }
  `]
})
export class DashboardComponent {
  readonly authStore = inject(AuthStore);
}
