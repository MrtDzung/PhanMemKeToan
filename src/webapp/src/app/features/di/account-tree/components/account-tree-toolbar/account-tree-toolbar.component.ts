import { Component, inject, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { AccountTreeStore } from '../../store/account-tree.store';

interface StatusOption {
  label: string;
  value: 'all' | 'active' | 'inactive';
}

@Component({
  selector: 'app-account-tree-toolbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, InputTextModule, SelectModule, ButtonModule, TooltipModule],
  template: `
    <div class="toolbar">
      <div class="toolbar-left">
        <span class="p-input-icon-left search-wrap">
          <i class="pi pi-search"></i>
          <input
            pInputText
            type="text"
            [ngModel]="store.searchQuery()"
            (ngModelChange)="store.setSearchQuery($event)"
            placeholder="Tìm kiếm tài khoản... (F3)"
            class="search-input"
            (keydown.f3)="$event.preventDefault(); searchInput.focus()"
            #searchInput
            aria-label="Tìm kiếm tài khoản"
          />
        </span>

        <p-select
          [options]="statusOptions"
          [ngModel]="store.statusFilter()"
          (ngModelChange)="store.setStatusFilter($event)"
          optionLabel="label"
          optionValue="value"
          placeholder="Trạng thái"
          styleClass="status-filter"
          aria-label="Lọc theo trạng thái"
        />

        <div class="expand-collapse-btns" role="group" aria-label="Mở rộng / Thu gọn cây tài khoản">
          <button
            type="button"
            class="icon-action-btn"
            pTooltip="Mở rộng tất cả"
            tooltipPosition="bottom"
            (click)="expandAll.emit()"
            aria-label="Mở rộng tất cả"
          ><i class="pi pi-angle-double-down"></i></button>
          <button
            type="button"
            class="icon-action-btn"
            pTooltip="Thu gọn tất cả"
            tooltipPosition="bottom"
            (click)="collapseAll.emit()"
            aria-label="Thu gọn tất cả"
          ><i class="pi pi-angle-double-up"></i></button>
        </div>

        <div class="tt-mode-toggle" aria-label="Chế độ hệ thống tài khoản">
          <button
            type="button"
            class="tt-btn"
            [class.active]="!isTT133Mode"
            (click)="isTT133Mode = false"
            title="Thông tư 200/2014 (doanh nghiệp lớn)"
          >TT200</button>
          <button
            type="button"
            class="tt-btn"
            [class.active]="isTT133Mode"
            (click)="isTT133Mode = true"
            title="Thông tư 133/2016 (doanh nghiệp vừa và nhỏ)"
          >TT133</button>
        </div>
      </div>

      <div class="toolbar-right">
        <!-- TODO: Add *appHasPermission directive when permission directive is implemented in src/app/core/ -->
        <p-button
          icon="pi pi-list"
          label="Nhập COA chuẩn"
          severity="secondary"
          size="small"
          (onClick)="importCoaStandard.emit()"
          aria-label="Nhập danh mục tài khoản chuẩn"
        />
        <p-button
          icon="pi pi-download"
          label="Xuất Excel"
          severity="secondary"
          size="small"
          [disabled]="isExporting()"
          [loading]="isExporting()"
          (onClick)="exportExcel.emit()"
          aria-label="Xuất danh sách tài khoản ra Excel"
        />
      </div>
    </div>
  `,
  styles: [`
    .toolbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      padding: 6px 12px;
      border-bottom: 1px solid var(--surface-border);
      background: var(--surface-card);
    }

    .toolbar-left {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .toolbar-right {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .search-wrap {
      width: 200px;
      flex-shrink: 0;
      height: 32px;
      display: inline-flex;
      align-items: center;
    }

    .search-input {
      width: 100%;
      font-size: 13px;
      height: 32px;
      padding-top: 0;
      padding-bottom: 0;
      box-sizing: border-box;
    }

    :host ::ng-deep .status-filter {
      width: 120px;
      flex-shrink: 0;
      font-size: 13px;
    }

    :host ::ng-deep .status-filter .p-select {
      height: 32px;
      align-items: center;
    }

    :host ::ng-deep .status-filter .p-select-label {
      padding-top: 0;
      padding-bottom: 0;
      line-height: 32px;
      font-size: 13px;
    }

    .expand-collapse-btns {
      display: flex;
      border: 1px solid var(--surface-border);
      border-radius: 4px;
      overflow: hidden;
    }

    .icon-action-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      border: none;
      background: transparent;
      color: var(--text-secondary);
      cursor: pointer;
      transition: background 0.15s, color 0.15s;
      font-size: 13px;
    }

    .icon-action-btn:hover {
      background: var(--surface-ground);
      color: var(--primary);
    }

    .icon-action-btn + .icon-action-btn {
      border-left: 1px solid var(--surface-border);
    }

    .tt-mode-toggle {
      display: flex;
      border: 1px solid var(--surface-border);
      border-radius: 4px;
      overflow: hidden;
    }

    .tt-btn {
      padding: 5px 10px;
      font-size: 12px;
      font-weight: 500;
      border: none;
      background: transparent;
      color: var(--text-secondary);
      cursor: pointer;
      transition: background 0.15s;
      font-family: inherit;
    }

    .tt-btn:hover { background: var(--surface-ground); }
    .tt-btn.active { background: var(--primary); color: white; }
    .tt-btn + .tt-btn { border-left: 1px solid var(--surface-border); }
  `],
})
export class AccountTreeToolbarComponent {
  readonly store = inject(AccountTreeStore);
  isTT133Mode = false;

  isExporting = input<boolean>(false);

  addNew = output<void>();
  importCoaStandard = output<void>();
  expandAll = output<void>();
  collapseAll = output<void>();
  exportExcel = output<void>();

  readonly statusOptions: StatusOption[] = [
    { label: 'Tất cả', value: 'all' },
    { label: 'Đang dùng', value: 'active' },
    { label: 'Ngừng dùng', value: 'inactive' },
  ];
}
