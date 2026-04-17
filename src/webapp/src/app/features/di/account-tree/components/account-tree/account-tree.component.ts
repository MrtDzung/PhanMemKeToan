import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TreeModule } from 'primeng/tree';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TreeNode } from 'primeng/api';
import { AccountTreeNodeDto, AccountCategoryKind } from '../../../models/account.models';

@Component({
  selector: 'app-account-tree',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TreeModule, ProgressSpinnerModule],
  template: `
    @if (loading()) {
      <div class="flex justify-center items-center h-full p-4">
        <p-progressSpinner strokeWidth="4" [style]="{ width: '40px', height: '40px' }" />
      </div>
    } @else {
      <p-tree
        [value]="treeNodes()"
        selectionMode="single"
        [(selection)]="selectedNode"
        (onNodeSelect)="onNodeSelect($event)"
        (onNodeExpand)="onNodeExpand($event)"
        (onNodeCollapse)="onNodeCollapse($event)"
        [style]="{ border: 'none', background: 'transparent' }"
        styleClass="account-tree"
        role="tree"
      >
        <ng-template pTemplate="default" let-node>
          <span class="account-tree-node" [attr.aria-level]="node.data?.grade" [attr.aria-selected]="node.data?.accountId === selectedAccountId()">
            <span class="account-number">{{ node.data?.accountNumber }}</span>
            <span class="account-name">{{ node.data?.accountName }}</span>
            @if (!node.data?.inactive) {
              <span class="category-badge" [class.debit]="node.data?.accountCategoryKind === AccountCategoryKind.Debit" [class.credit]="node.data?.accountCategoryKind === AccountCategoryKind.Credit">
                {{ node.data?.accountCategoryKind === AccountCategoryKind.Debit ? 'Nợ' : 'Có' }}
              </span>
            }
            @if (node.data?.inactive) {
              <span class="inactive-badge">Ngừng dùng</span>
            }
            @if (node.data?.hasTransactions) {
              <span class="has-tx-badge" title="Có phát sinh">
                <i class="pi pi-database"></i>
              </span>
            }
          </span>
        </ng-template>
      </p-tree>
    }
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
    }

    .account-tree-node {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 13px;
    }

    .account-number {
      font-family: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
      font-size: 12px;
      color: var(--text-secondary);
      min-width: 60px;
      font-variant-numeric: tabular-nums;
    }

    .account-name {
      flex: 1;
      color: var(--text-primary);
    }

    .category-badge {
      font-size: 11px;
      padding: 1px 6px;
      border-radius: 3px;
      font-weight: 600;
    }
    .category-badge.debit {
      color: var(--debit);
      border: 1px solid var(--debit);
      background: var(--debit-bg);
    }
    .category-badge.credit {
      color: var(--credit);
      border: 1px solid var(--credit);
      background: var(--credit-bg);
    }

    .inactive-badge {
      font-size: 11px;
      padding: 1px 6px;
      border-radius: 3px;
      color: var(--text-disabled);
      border: 1px solid var(--text-disabled);
    }

    .has-tx-badge {
      color: var(--info);
      font-size: 11px;
    }

    :host ::ng-deep .p-tree {
      flex: 1;
      overflow-y: auto;
      font-size: 13px;
    }

    :host ::ng-deep .p-tree-node-content {
      height: 32px;
      align-items: center;
    }
  `],
})
export class AccountTreeComponent {
  readonly AccountCategoryKind = AccountCategoryKind;

  treeNodes = input.required<TreeNode[]>();
  loading = input<boolean>(false);
  selectedAccountId = input<string | null>(null);

  accountSelected = output<AccountTreeNodeDto>();
  nodeExpanded = output<string>();
  nodeCollapsed = output<string>();

  selectedNode: TreeNode | null = null;

  onNodeSelect(event: { node: TreeNode }): void {
    const data = event.node.data as AccountTreeNodeDto;
    if (data) {
      this.accountSelected.emit(data);
    }
  }

  onNodeExpand(event: { node: TreeNode }): void {
    const data = event.node.data as AccountTreeNodeDto;
    if (data) this.nodeExpanded.emit(data.accountId);
  }

  onNodeCollapse(event: { node: TreeNode }): void {
    const data = event.node.data as AccountTreeNodeDto;
    if (data) this.nodeCollapsed.emit(data.accountId);
  }
}
