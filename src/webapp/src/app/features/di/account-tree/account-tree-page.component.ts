import { Component, inject, OnInit, ChangeDetectionStrategy, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SplitterModule } from 'primeng/splitter';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
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
  providers: [AccountTreeStore, MessageService],
  imports: [
    CommonModule,
    SplitterModule,
    ToastModule,
    AccountTreeComponent,
    AccountTreeToolbarComponent,
    AccountDetailPanelComponent,
    ImportCoaDialogComponent,
  ],
  template: `
    <p-toast />
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

      <div class="page-body">
        <p-splitter
          [panelSizes]="[40, 60]"
          [minSizes]="[20, 30]"
          styleClass="full-height-splitter"
        >
          <ng-template pTemplate>
            <div class="tree-panel">
              <app-account-tree-toolbar
                (addNew)="onAddNew()"
                (importCoa)="onOpenImportDialog()"
              />
              <app-account-tree
                [treeNodes]="store.treeNodes()"
                [loading]="store.loading()"
                [selectedAccountId]="store.selectedAccountId()"
                (accountSelected)="onAccountSelected($event)"
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
  readonly importDialog = viewChild.required(ImportCoaDialogComponent);

  ngOnInit(): void {
    this.store.loadTree();
  }

  onAccountSelected(account: AccountTreeNodeDto): void {
    this.store.selectAccount(account.accountId);
  }

  onAddNew(): void {
    this.store.setFormMode('create');
  }

  onOpenImportDialog(): void {
    this.importDialog().open();
  }
}
