import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TreeTableModule } from 'primeng/treetable';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TreeNode } from 'primeng/api';
import { AccountTreeNodeDto, AccountCategoryKind } from '../../../models/account.models';

@Component({
  selector: 'app-account-tree',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TreeTableModule, ProgressSpinnerModule],
  template: `
    @if (loading()) {
      <div class="flex justify-center items-center h-full p-4">
        <p-progressSpinner strokeWidth="4" [style]="{ width: '40px', height: '40px' }" />
      </div>
    } @else {
      <p-treeTable
        [value]="treeNodes()"
        [scrollable]="true"
        scrollHeight="flex"
        styleClass="account-tree-table"
        [tableStyle]="{ 'min-width': '520px' }"
        (onNodeExpand)="onNodeExpand($event)"
        (onNodeCollapse)="onNodeCollapse($event)"
      >
        <ng-template pTemplate="header">
          <tr>
            <th class="col-num">SỐ TK</th>
            <th>Tên tài khoản</th>
            <th class="col-category">Tính chất</th>
            <th class="col-object">Đối tượng</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-rowNode let-rowData="rowData">
          <tr
            [ttRow]="rowNode"
            (click)="onRowClick(rowData)"
            [class.row-selected]="rowData.accountId === selectedAccountId()"
            [class.row-inactive]="rowData.inactive"
          >
            <td class="cell-num">
              <p-treeTableToggler [rowNode]="rowNode" />
              <span class="acct-num" [class.parent]="rowData.isParent">{{ rowData.accountNumber }}</span>
            </td>
            <td>
              <span
                class="acct-name"
                [class.grade1]="rowData.grade === 1"
                [class.grade2]="rowData.grade === 2"
              >{{ rowData.accountName }}</span>
            </td>
            <td>
              @if (rowData.accountCategoryKind === AccountCategoryKind.Mixed) {
                <span class="cat-badge mixed">Lưỡng tính</span>
              } @else if (rowData.accountCategoryKind === AccountCategoryKind.Debit) {
                <span class="cat-badge debit">Dư Nợ</span>
              } @else {
                <span class="cat-badge credit">Dư Có</span>
              }
            </td>
            <td class="cell-center">
              <span class="dash">—</span>
            </td>
          </tr>
        </ng-template>
      </p-treeTable>
    }
  `,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
    }

    :host ::ng-deep .p-treetable {
      flex: 1;
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
    }

    :host ::ng-deep .p-treetable-scrollable-wrapper,
    :host ::ng-deep .p-treetable-wrapper {
      flex: 1;
      overflow: auto;
    }

    :host ::ng-deep .account-tree-table .p-treetable-thead > tr > th {
      background: var(--surface-ground);
      color: var(--text-secondary);
      font-size: 11px;
      font-weight: 700;
      padding: 6px 10px;
      border-bottom: 2px solid var(--surface-border);
      white-space: nowrap;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    /* Offset header text to align with account numbers (toggler = 16px + gap = 2px + btn padding ~4px) */
    :host ::ng-deep .account-tree-table .p-treetable-thead > tr > th:first-child {
      padding-left: 32px;
    }

    .col-num { width: 110px; }
    .col-category { width: 120px; }
    .col-object { width: 90px; }

    :host ::ng-deep .account-tree-table .p-treetable-tbody > tr {
      height: 32px;
      cursor: pointer;
      transition: background 0.1s;
    }

    :host ::ng-deep .account-tree-table .p-treetable-tbody > tr:hover {
      background: var(--primary-light) !important;
    }

    :host ::ng-deep .account-tree-table .p-treetable-tbody > tr.row-selected {
      background: #dce8f8 !important;
    }

    :host ::ng-deep .account-tree-table .p-treetable-tbody > tr.row-inactive {
      opacity: 0.55;
    }

    :host ::ng-deep .account-tree-table .p-treetable-tbody > tr > td {
      padding: 0 10px;
      font-size: 13px;
      border-bottom: 1px solid var(--surface-border);
      vertical-align: middle;
    }

    .cell-num {
      display: flex;
      align-items: center;
      gap: 2px;
    }

    .cell-center {
      text-align: center;
    }

    .acct-num {
      font-family: 'JetBrains Mono', 'Fira Code', 'Consolas', monospace;
      font-size: 12px;
      color: var(--text-secondary);
      font-variant-numeric: tabular-nums;
    }

    .acct-num.parent {
      font-weight: 700;
      color: var(--text-primary);
    }

    .acct-name {
      font-size: 13px;
      color: var(--text-primary);
    }

    .acct-name.grade1 {
      font-weight: 700;
      text-transform: uppercase;
      font-size: 12px;
      letter-spacing: 0.04em;
    }

    .acct-name.grade2 {
      font-weight: 600;
    }

    .cat-badge {
      font-size: 11px;
      padding: 2px 8px;
      border-radius: 3px;
      font-weight: 500;
      white-space: nowrap;
    }

    .cat-badge.mixed {
      color: var(--text-secondary);
      border: 1px solid var(--surface-border);
      background: var(--surface-ground);
    }

    .cat-badge.debit {
      color: var(--debit);
      border: 1px solid var(--debit);
      background: rgba(21, 101, 192, 0.06);
    }

    .cat-badge.credit {
      color: var(--credit);
      border: 1px solid var(--credit);
      background: rgba(198, 40, 40, 0.06);
    }

    .dash {
      color: var(--text-disabled);
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

  onRowClick(data: AccountTreeNodeDto): void {
    this.accountSelected.emit(data);
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

