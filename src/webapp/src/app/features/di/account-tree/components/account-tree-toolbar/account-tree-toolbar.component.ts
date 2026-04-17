import { Component, inject, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { AccountTreeStore } from '../../store/account-tree.store';

interface StatusOption {
  label: string;
  value: 'all' | 'active' | 'inactive';
}

@Component({
  selector: 'app-account-tree-toolbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, InputTextModule, SelectModule, ButtonModule],
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
      </div>

      <div class="toolbar-right">
        <p-button
          icon="pi pi-plus"
          label="Thêm mới"
          severity="primary"
          size="small"
          (onClick)="addNew.emit()"
          aria-label="Thêm tài khoản mới"
        />
        <p-button
          icon="pi pi-upload"
          label="Nhập danh mục"
          severity="secondary"
          size="small"
          (onClick)="importCoa.emit()"
          aria-label="Nhập danh mục tài khoản"
        />
        <p-button
          icon="pi pi-download"
          label="Xuất Excel"
          severity="secondary"
          size="small"
          [disabled]="true"
          aria-label="Xuất danh sách tài khoản (chưa hỗ trợ)"
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
      padding: 8px 12px;
      border-bottom: 1px solid var(--surface-border);
      background: var(--surface-card);
    }

    .toolbar-left {
      display: flex;
      align-items: center;
      gap: 8px;
      flex: 1;
    }

    .toolbar-right {
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .search-wrap {
      flex: 1;
      max-width: 320px;
    }

    .search-input {
      width: 100%;
      font-size: 13px;
    }

    :host ::ng-deep .status-filter {
      min-width: 130px;
      font-size: 13px;
    }
  `],
})
export class AccountTreeToolbarComponent {
  readonly store = inject(AccountTreeStore);

  addNew = output<void>();
  importCoa = output<void>();

  readonly statusOptions: StatusOption[] = [
    { label: 'Tất cả', value: 'all' },
    { label: 'Đang dùng', value: 'active' },
    { label: 'Ngừng dùng', value: 'inactive' },
  ];
}
