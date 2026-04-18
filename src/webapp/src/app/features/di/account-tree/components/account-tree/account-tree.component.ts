import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TreeTableModule } from 'primeng/treetable';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { TreeNode } from 'primeng/api';
import { AccountTreeNodeDto, AccountCategoryKind } from '../../../models/account.models';

@Component({
  selector: 'app-account-tree',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TreeTableModule, ProgressSpinnerModule, TooltipModule],
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
        [tableStyle]="{ 'min-width': '700px' }"
        selectionMode="single"
        (onNodeExpand)="onNodeExpand($event)"
        (onNodeCollapse)="onNodeCollapse($event)"
        (onNodeSelect)="onNodeSelect($event)"
      >
        <ng-template pTemplate="colgroup">
          <colgroup>
            <col style="width: 140px">
            <col>
            <col style="width: 110px">
            <col style="width: 150px">
            <col style="width: 110px">
          </colgroup>
        </ng-template>
        <ng-template pTemplate="header">
          <tr>
            <th>SỐ TK</th>
            <th>Tên tài khoản</th>
            <th>Tính chất</th>
            <th>Đối tượng</th>
            <th style="text-align: center">Thao tác</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-rowNode let-rowData="rowData">
          <tr
            [ttSelectableRow]="rowNode"
            [class.row-selected]="rowData.accountId === selectedAccountId()"
            [class.row-inactive]="rowData.inactive"
          >
            <td>
              <div class="cell-num">
                <p-treeTableToggler [rowNode]="rowNode" />
                <span class="acct-num" [class.parent]="rowData.isParent">{{ rowData.accountNumber }}</span>
              </div>
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
            <td>
              <div class="cell-actions">
                <button
                  type="button"
                  class="row-action-btn add"
                  pTooltip="Thêm tài khoản con"
                  tooltipPosition="left"
                  (click)="$event.stopPropagation(); addAccount.emit(rowData)"
                  aria-label="Thêm tài khoản con"
                ><i class="pi pi-plus"></i></button>
                <button
                  type="button"
                  class="row-action-btn"
                  pTooltip="Sửa tài khoản"
                  tooltipPosition="left"
                  (click)="$event.stopPropagation(); editAccount.emit(rowData)"
                  aria-label="Sửa tài khoản"
                ><i class="pi pi-pencil"></i></button>
                <button
                  type="button"
                  class="row-action-btn delete"
                  pTooltip="Xóa tài khoản"
                  tooltipPosition="left"
                  (click)="$event.stopPropagation(); deleteAccount.emit(rowData)"
                  aria-label="Xóa tài khoản"
                ><i class="pi pi-trash"></i></button>
              </div>
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

    :host ::ng-deep .account-tree-table table {
      border-collapse: collapse;
      border-spacing: 0;
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

    .cell-actions {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 2px;
    }

    .row-action-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 26px;
      height: 26px;
      border: none;
      border-radius: 4px;
      background: transparent;
      color: var(--text-secondary);
      cursor: pointer;
      transition: background 0.15s, color 0.15s;
      padding: 0;
    }

    .row-action-btn:hover {
      background: var(--primary-light);
      color: var(--primary);
    }

    .row-action-btn.add:hover {
      background: rgba(46, 125, 50, 0.08);
      color: var(--success);
    }

    .row-action-btn.delete:hover {
      background: rgba(211, 47, 47, 0.08);
      color: var(--error);
    }

    .row-action-btn i {
      font-size: 13px;
    }

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
      border-left: none;
      border-right: none;
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
  addAccount = output<AccountTreeNodeDto>();
  editAccount = output<AccountTreeNodeDto>();
  deleteAccount = output<AccountTreeNodeDto>();
  nodeExpanded = output<string>();
  nodeCollapsed = output<string>();

  onNodeSelect(event: { node?: TreeNode }): void {
    const data = event.node?.data as AccountTreeNodeDto;
    if (data) this.accountSelected.emit(data);
  }

  onNodeExpand(event: { node?: TreeNode }): void {
    const data = event.node?.data as AccountTreeNodeDto;
    if (data) this.nodeExpanded.emit(data.accountId);
  }

  onNodeCollapse(event: { node?: TreeNode }): void {
    const data = event.node?.data as AccountTreeNodeDto;
    if (data) this.nodeCollapsed.emit(data.accountId);
  }
}

