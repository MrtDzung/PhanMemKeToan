import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { AuthStore } from '../../core/stores/auth.store';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, ButtonModule, AvatarModule, SelectModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="shell-layout">
      <header class="top-bar">
          <div class="top-bar-left">
            <div class="app-logo">
              <i class="pi pi-chart-bar"></i>
              <span>Kế Toán</span>
            </div>
            @if (authStore.companies().length > 1) {
              <span class="topbar-sep">|</span>
              <p-select
                [options]="authStore.companies()"
                [ngModel]="authStore.selectedCompany()?.tenantId"
                (ngModelChange)="onCompanyChange($event)"
                optionLabel="name"
                optionValue="tenantId"
                placeholder="Chọn công ty"
                styleClass="company-select"
                [style]="{ minWidth: '200px' }"
              />
            } @else if (authStore.selectedCompany()) {
              <span class="topbar-sep">|</span>
              <span class="company-name-label">{{ authStore.selectedCompany()?.name }}</span>
            }
          </div>
          <div class="top-bar-right">
            <span class="user-name">{{ authStore.currentUser()?.fullName }}</span>
            <p-button
              icon="pi pi-sign-out"
              variant="text"
              size="small"
              pTooltip="Đăng xuất"
              (onClick)="logout()"
            />
          </div>
      </header>

      <div class="shell-body">
        <nav class="sidebar">
          <ul class="nav-menu">
          <li>
            <a routerLink="/dashboard" routerLinkActive="active" class="nav-item">
              <i class="pi pi-home"></i>
              <span>Trang chủ</span>
            </a>
          </li>
          <li class="nav-group-label">Danh mục</li>
          <li>
            <a routerLink="/di/accounts" routerLinkActive="active" class="nav-item">
              <i class="pi pi-list"></i>
              <span>Tài khoản kế toán</span>
            </a>
          </li>
          <li>
            <a routerLink="/di/account-objects" routerLinkActive="active" class="nav-item">
              <i class="pi pi-users"></i>
              <span>Đối tượng kế toán</span>
            </a>
          </li>
          <li>
            <a routerLink="/di/inventory-items" routerLinkActive="active" class="nav-item">
              <i class="pi pi-box"></i>
              <span>Hàng tồn kho</span>
            </a>
          </li>
          <li class="nav-group-label">Danh mục phụ</li>
          <li>
            <a routerLink="/di/setup/currencies" routerLinkActive="active" class="nav-item">
              <i class="pi pi-dollar"></i>
              <span>Tiền tệ</span>
            </a>
          </li>
          <li>
            <a routerLink="/di/setup/units" routerLinkActive="active" class="nav-item">
              <i class="pi pi-box"></i>
              <span>Đơn vị tính</span>
            </a>
          </li>
          <li>
            <a routerLink="/di/setup/warehouses" routerLinkActive="active" class="nav-item">
              <i class="pi pi-warehouse"></i>
              <span>Kho hàng</span>
            </a>
          </li>
          <li>
            <a routerLink="/di/setup/departments" routerLinkActive="active" class="nav-item">
              <i class="pi pi-sitemap"></i>
              <span>Phòng ban</span>
            </a>
          </li>
          <li>
            <a routerLink="/di/setup/expense-items" routerLinkActive="active" class="nav-item">
              <i class="pi pi-tags"></i>
              <span>Khoản mục chi phí</span>
            </a>
          </li>
          <li class="nav-group-label">Hệ thống</li>
          <li>
            <a routerLink="/system/users" routerLinkActive="active" class="nav-item">
              <i class="pi pi-users"></i>
              <span>Người dùng</span>
            </a>
          </li>
          <li>
            <a routerLink="/system/roles" routerLinkActive="active" class="nav-item">
              <i class="pi pi-shield"></i>
              <span>Vai trò</span>
            </a>
          </li>
          <li>
            <a routerLink="/system/tenants" routerLinkActive="active" class="nav-item">
              <i class="pi pi-building"></i>
              <span>Công ty</span>
            </a>
          </li>
        </ul>
        </nav>
        <main class="content-area">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [`
    .shell-layout { display: flex; flex-direction: column; min-height: 100vh; background: var(--surface-ground); }
    .shell-body { display: flex; flex: 1; overflow: hidden; min-height: 0; }
    .sidebar { width: 220px; background: var(--surface-card); border-right: 1px solid var(--surface-border); flex-shrink: 0; padding: 0; overflow-y: auto; }
    .nav-menu { list-style: none; margin: 0; padding: 8px 0; }
    .nav-group-label { font-size: 11px; color: var(--text-disabled); padding: 16px 16px 4px; text-transform: uppercase; letter-spacing: 0.05em; }
    .nav-item { display: flex; align-items: center; gap: 10px; padding: 8px 16px; color: var(--text-secondary); text-decoration: none; font-size: 13px; transition: background 0.15s; }
    .nav-item:hover { background: var(--surface-ground); color: var(--text-primary); }
    .nav-item.active { background: var(--primary-light); color: var(--primary-dark); font-weight: 600; border-right: 3px solid var(--primary); }
    .top-bar { height: 48px; background: var(--primary); display: flex; align-items: center; justify-content: space-between; padding: 0 16px; gap: 12px; flex-shrink: 0; }
    .app-logo { display: flex; align-items: center; gap: 8px; color: white; font-size: 16px; font-weight: 700; flex-shrink: 0; }
    .topbar-sep { color: var(--color-on-primary-dim); margin: 0 10px; font-size: 18px; line-height: 1; }
    .top-bar-left { display: flex; align-items: center; }
    .top-bar-right { display: flex; align-items: center; gap: 12px; }
    .user-name { font-size: 13px; color: var(--color-on-primary-muted); }
    .company-name-label { font-size: 13px; font-weight: 500; color: var(--color-on-primary-muted); }
    .content-area { flex: 1; overflow: auto; }
    :host ::ng-deep .top-bar .p-button { color: white !important; }
    :host ::ng-deep .top-bar .p-button:hover { background: var(--color-on-primary-hover) !important; }
    :host ::ng-deep .company-select { background: var(--color-on-primary-hover); border-color: var(--color-on-primary-border); }
    :host ::ng-deep .company-select .p-select-label { color: var(--color-on-primary-muted); font-size: 13px; }
  `]
})
export class ShellComponent {
  readonly authStore = inject(AuthStore);

  async onCompanyChange(tenantId: string): Promise<void> {
    if (tenantId && tenantId !== this.authStore.selectedCompany()?.tenantId) {
      await this.authStore.switchCompany(tenantId);
    }
  }

  async logout(): Promise<void> {
    await this.authStore.logout();
  }
}

