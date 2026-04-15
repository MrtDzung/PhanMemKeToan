import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { AuthStore } from '../../core/stores/auth.store';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, ButtonModule, AvatarModule],
  template: `
    <div class="shell-layout">
      <nav class="sidebar">
        <div class="sidebar-logo">
          <span class="logo-text">Kế Toán</span>
        </div>
        <ul class="nav-menu">
          <li>
            <a routerLink="/dashboard" routerLinkActive="active" class="nav-item">
              <i class="pi pi-home"></i>
              <span>Trang chủ</span>
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
        </ul>
      </nav>

      <div class="main-area">
        <header class="top-bar">
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
        <main class="content-area">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [`
    .shell-layout { display: flex; min-height: 100vh; background: var(--surface-ground); }
    .sidebar { width: 220px; background: var(--primary-dark); color: white; flex-shrink: 0; padding: 0; }
    .sidebar-logo { padding: 20px 16px; border-bottom: 1px solid var(--sidebar-hover, rgba(255,255,255,0.1)); }
    .logo-text { font-size: 16px; font-weight: 700; color: white; }
    .nav-menu { list-style: none; margin: 0; padding: 8px 0; }
    .nav-group-label { font-size: 11px; color: var(--sidebar-text-muted, rgba(255,255,255,0.5)); padding: 16px 16px 4px; text-transform: uppercase; letter-spacing: 0.05em; }
    .nav-item { display: flex; align-items: center; gap: 10px; padding: 8px 16px; color: var(--sidebar-text, rgba(255,255,255,0.8)); text-decoration: none; font-size: 13px; border-radius: 0; transition: background 0.15s; }
    .nav-item:hover { background: var(--sidebar-hover, rgba(255,255,255,0.1)); color: white; }
    .nav-item.active { background: var(--primary); color: white; }
    .main-area { flex: 1; display: flex; flex-direction: column; min-width: 0; }
    .top-bar { height: 48px; background: var(--surface-card); border-bottom: 1px solid var(--surface-border); display: flex; align-items: center; justify-content: flex-end; padding: 0 16px; gap: 12px; }
    .user-name { font-size: 13px; color: var(--text-secondary); }
    .content-area { flex: 1; overflow: auto; }
  `]
})
export class ShellComponent {
  readonly authStore = inject(AuthStore);

  async logout(): Promise<void> {
    await this.authStore.logout();
  }
}

