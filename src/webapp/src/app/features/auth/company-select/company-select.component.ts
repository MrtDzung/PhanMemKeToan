import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AuthStore } from '../../../core/stores/auth.store';
import { CompanyInfo } from '../../../core/models/auth.models';

@Component({
  selector: 'app-company-select',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule, TagModule, ProgressSpinnerModule],
  template: `
    <div class="select-container">
      <div class="select-card">
        <div class="select-header">
          <h1 class="select-title">Chọn Công Ty</h1>
          <p class="select-subtitle">Chọn công ty để tiếp tục làm việc</p>
        </div>

        @if (authStore.isLoading()) {
          <div class="loading-wrapper">
            <p-progressSpinner [style]="{ width: '40px', height: '40px' }" />
            <span class="loading-text">Đang xử lý...</span>
          </div>
        }

        @if (authStore.error()) {
          <div class="error-banner">{{ authStore.error() }}</div>
        }

        <div class="company-list">
          @for (company of authStore.companies(); track company.tenantId) {
            <div
              class="company-item"
              [class.default]="company.isDefault"
              (click)="onSelect(company)"
            >
              <div class="company-info">
                <div class="company-name">{{ company.name }}</div>
                <div class="company-meta">
                  <span class="company-code">{{ company.code }}</span>
                  @if (company.displayRole) {
                    <span class="company-role">{{ company.displayRole }}</span>
                  }
                </div>
              </div>
              <div class="company-badges">
                @if (company.dbStatus === 'Online') {
                  <p-tag value="Online" severity="success" />
                } @else if (company.dbStatus === 'Migrating') {
                  <p-tag value="Đang nâng cấp" severity="warn" />
                } @else {
                  <p-tag value="Offline" severity="danger" />
                }
                @if (company.isDefault) {
                  <p-tag value="Mặc định" severity="info" />
                }
              </div>
            </div>
          }
        </div>

        <div class="select-footer">
          <p-button
            label="Đăng xuất"
            variant="text"
            size="small"
            icon="pi pi-sign-out"
            (onClick)="onLogout()"
          />
        </div>
      </div>
    </div>
  `,
  styles: [`
    .select-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background-color: var(--surface-ground);
    }
    .select-card {
      background: var(--surface-card);
      border: 1px solid var(--surface-border);
      border-radius: 8px;
      padding: var(--spacing-xl, 32px);
      width: 100%;
      max-width: 480px;
      box-shadow: var(--shadow-card, 0 2px 8px rgba(0,0,0,0.08));
    }
    .select-header { text-align: center; margin-bottom: var(--spacing-lg, 24px); }
    .select-title { font-size: 20px; font-weight: 700; color: var(--primary); margin: 0 0 4px; }
    .select-subtitle { font-size: 13px; color: var(--text-secondary); margin: 0; }
    .loading-wrapper { display: flex; align-items: center; justify-content: center; gap: 8px; padding: 16px 0; }
    .loading-text { font-size: 13px; color: var(--text-secondary); }
    .error-banner { background: var(--red-50, #fef2f2); color: var(--error); padding: 8px 12px; border-radius: 4px; font-size: 13px; margin-bottom: 16px; }
    .company-list { display: flex; flex-direction: column; gap: 8px; }
    .company-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 12px 16px;
      border: 1px solid var(--surface-border);
      border-radius: 6px;
      cursor: pointer;
      transition: border-color 0.15s, background 0.15s;
    }
    .company-item:hover { border-color: var(--primary); background: var(--primary-light, #E8F0FE); }
    .company-item.default { border-color: var(--primary); }
    .company-info { flex: 1; }
    .company-name { font-size: 14px; font-weight: 600; color: var(--text-primary); }
    .company-meta { display: flex; gap: 8px; margin-top: 2px; }
    .company-code { font-size: 12px; color: var(--text-secondary); font-family: 'JetBrains Mono', 'Consolas', monospace; }
    .company-role { font-size: 12px; color: var(--text-secondary); }
    .company-badges { display: flex; gap: 4px; align-items: center; }
    .select-footer { text-align: center; margin-top: var(--spacing-md, 16px); }
  `]
})
export class CompanySelectComponent implements OnInit {
  readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  ngOnInit(): void {
    // If no tempToken, redirect to login
    if (!this.authStore.hasTempToken() && !this.authStore.isAuthenticated()) {
      this.router.navigate(['/login']);
    }
  }

  async onSelect(company: CompanyInfo): Promise<void> {
    if (company.dbStatus !== 'Online') return;
    try {
      await this.authStore.selectCompany(company.tenantId);
    } catch {
      // Error handled by store
    }
  }

  async onLogout(): Promise<void> {
    await this.authStore.logout();
  }
}
