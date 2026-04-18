import { Component, inject, OnInit, ChangeDetectionStrategy, viewChild, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SplitterModule } from 'primeng/splitter';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { AccountTreeStore } from './store/account-tree.store';
import { AccountTreeComponent } from './components/account-tree/account-tree.component';
import { AccountTreeToolbarComponent } from './components/account-tree-toolbar/account-tree-toolbar.component';
import { AccountDetailPanelComponent } from './components/account-detail-panel/account-detail-panel.component';
import { ImportCoaDialogComponent } from './components/import-coa-dialog/import-coa-dialog.component';
import { AccountTreeNodeDto } from '../models/account.models';

@Component({
  selector: 'app-account-tree-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AccountTreeStore, MessageService, ConfirmationService],
  imports: [
    CommonModule,
    RouterLink,
    SplitterModule,
    ToastModule,
    ConfirmDialogModule,
    AccountTreeComponent,
    AccountTreeToolbarComponent,
    AccountDetailPanelComponent,
    ImportCoaDialogComponent,
  ],
  template: `
    <p-toast />
    <p-confirmDialog [style]="{ width: '450px' }" contentStyleClass="p-4" headerStyleClass="p-4 pb-0" footerStyleClass="p-4 pt-0 gap-2" />
    <div class="page-layout">
      <div class="page-header">
        <h1 class="page-title">Hệ thống Tài khoản Kế toán</h1>
        @if (store.error()) {
          <div class="error-banner">
            <i class="pi pi-exclamation-circle"></i>
            {{ store.error() }}
          </div>
        }
      </div>

      <nav class="breadcrumb-bar">
        <a routerLink="/dashboard" class="breadcrumb-link"><i class="pi pi-home"></i></a>
        <i class="pi pi-angle-right breadcrumb-sep"></i>
        <span class="breadcrumb-item">Danh mục</span>
        <i class="pi pi-angle-right breadcrumb-sep"></i>
        <span class="breadcrumb-item active">Hệ thống tài khoản</span>
      </nav>

      <div class="page-body">
        <p-splitter
          [panelSizes]="[72, 28]"
          [minSizes]="[30, 20]"
          styleClass="full-height-splitter"
        >
          <ng-template pTemplate>
            <div class="tree-panel">
              <app-account-tree-toolbar
                (importCoaStandard)="onOpenImportDialog()"
                (expandAll)="store.expandAll()"
                (collapseAll)="store.collapseAll()"
              />
              <app-account-tree
                [treeNodes]="store.treeNodes()"
                [loading]="store.loading()"
                [selectedAccountId]="store.selectedAccountId()"
                (accountSelected)="onAccountSelected($event)"
                (addAccount)="onAddAccount($event)"
                (editAccount)="onEditAccount($event)"
                (deleteAccount)="onDeleteAccount($event)"
                (nodeExpanded)="store.toggleExpanded($event)"
                (nodeCollapsed)="store.toggleExpanded($event)"
                class="tree-container"
              />
            </div>
          </ng-template>

          <ng-template pTemplate>
            <div class="detail-panel-wrap">
              <app-account-detail-panel />
            </div>
          </ng-template>
        </p-splitter>
      </div>

      <footer class="status-bar">
        <span>Tổng: <strong>{{ totalCount() }}</strong> tài khoản</span>
        <span class="status-sep">|</span>
        <span>Đang dùng: <strong class="active-count">{{ activeCount() }}</strong></span>
        <span class="status-sep">|</span>
        <span>Ngừng dùng: <strong class="inactive-count">{{ inactiveCount() }}</strong></span>
      </footer>
    </div>

    <app-import-coa-dialog #importDialog />
  `,
  styles: [`
    .page-layout {
      height: 100%;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      background: var(--surface-ground);
    }

    .page-header {
      padding: 12px 16px 8px;
      background: var(--surface-card);
      border-bottom: 1px solid var(--surface-border);
    }

    .page-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin: 0 0 4px;
    }

    .error-banner {
      display: flex;
      align-items: center;
      gap: 6px;
      color: var(--error);
      font-size: 12px;
      padding: 4px 0;
    }

    .breadcrumb-bar {
      height: 32px;
      display: flex;
      align-items: center;
      gap: 4px;
      padding: 0 16px;
      background: var(--surface-card);
      border-bottom: 1px solid var(--surface-border);
      font-size: 12px;
      color: var(--text-secondary);
      flex-shrink: 0;
    }

    .breadcrumb-link { color: var(--primary); text-decoration: none; }
    .breadcrumb-sep { font-size: 10px; color: var(--text-disabled); margin: 0 2px; }
    .breadcrumb-item { color: var(--text-secondary); }
    .breadcrumb-item.active { color: var(--text-primary); font-weight: 500; }

    .status-bar {
      height: 24px;
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 0 16px;
      background: var(--surface-card);
      border-top: 1px solid var(--surface-border);
      font-size: 12px;
      color: var(--text-secondary);
      flex-shrink: 0;
    }

    .status-sep { color: var(--surface-border); }
    .active-count { color: var(--success); font-weight: 600; }
    .inactive-count { color: var(--text-disabled); font-weight: 600; }

    .page-body {
      flex: 1;
      overflow: hidden;
    }

    .tree-panel {
      height: 100%;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      background: var(--surface-card);
    }

    .tree-container {
      flex: 1;
      overflow-y: auto;
    }

    .detail-panel-wrap {
      height: 100%;
      overflow: hidden;
    }

    :host ::ng-deep .full-height-splitter {
      height: 100%;
    }

    :host ::ng-deep .p-splitter {
      height: 100%;
      border: none;
      border-radius: 0;
    }

    :host ::ng-deep .p-splitter-panel {
      overflow: hidden;
    }
  `],
})
export class AccountTreePageComponent implements OnInit {
  readonly store = inject(AccountTreeStore);
  readonly confirmationService = inject(ConfirmationService);
  readonly messageService = inject(MessageService);
  readonly importDialog = viewChild.required(ImportCoaDialogComponent);
  readonly totalCount = computed(() => this.store.accounts().length);
  readonly activeCount = computed(() => this.store.accounts().filter(a => !a.inactive).length);
  readonly inactiveCount = computed(() => this.store.accounts().filter(a => a.inactive).length);

  ngOnInit(): void {
    this.store.loadTree();
  }

  onAccountSelected(account: AccountTreeNodeDto): void {
    this.store.selectAccount(account.accountId);
  }

  onAddAccount(parentAccount: AccountTreeNodeDto): void {
    this.store.setCreateMode(parentAccount.accountId);
  }

  onEditAccount(account: AccountTreeNodeDto): void {
    this.store.selectAccount(account.accountId);
    this.store.setFormMode('edit');
  }

  onDeleteAccount(account: AccountTreeNodeDto): void {
    this.confirmationService.confirm({
      message: `Xóa tài khoản <strong>${account.accountNumber} — ${account.accountName}</strong>?<br><small style="color:var(--text-secondary)">Hành động này không thể hoàn tác.</small>`,
      header: 'Xác nhận xóa',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Xóa',
      rejectLabel: 'Hủy',
      acceptButtonStyleClass: 'p-button-danger p-button-sm',
      rejectButtonStyleClass: 'p-button-text p-button-secondary p-button-sm',
      accept: async () => {
        await this.store.selectAccount(account.accountId);
        const detail = this.store.selectedAccountDetail();
        if (!detail) {
          this.messageService.add({ severity: 'error', summary: 'Không thể tải thông tin tài khoản' });
          return;
        }
        try {
          await this.store.deleteAccount(account.accountId, detail.rowVersion);
          this.messageService.add({ severity: 'success', summary: 'Đã xóa', detail: `Tài khoản ${account.accountNumber} đã được xóa.` });
        } catch {
          // error already in store
        }
      }
    });
  }

  onOpenImportDialog(): void {
    this.importDialog().open();
  }
}
