import { Component, OnInit, DestroyRef, inject, signal, ChangeDetectionStrategy, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { SelectModule } from 'primeng/select';
import { MessageService, ConfirmationService } from 'primeng/api';
import { LookupsApiService } from '../services/lookups-api.service';
import { DepartmentDto } from '../../models/master-data.models';

@Component({
  selector: 'app-departments-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [MessageService, ConfirmationService],
  imports: [
    CommonModule, FormsModule, TableModule, ButtonModule,
    InputTextModule, ToggleSwitchModule, ConfirmDialogModule, ToastModule, SelectModule
  ],
  template: `
    <p-toast />
    <p-confirmDialog [style]="{width: '400px'}" />
    <div class="page-wrapper p-4 h-full flex flex-column" style="background: var(--surface-ground)">
      <div class="toolbar flex align-items-center justify-content-between mb-4 p-3 border-round shadow-1" style="background: var(--surface-card)">
        <p-button label="Thêm mới" icon="pi pi-plus" size="small" (onClick)="addRow()" />
      </div>
      <div class="table-container flex-1 border-round border-1 border-solid overflow-hidden" style="background: var(--surface-card); border-color: var(--surface-border)">
        <p-table [value]="items()" [loading]="loading()" dataKey="id" editMode="row" styleClass="p-datatable-sm p-datatable-gridlines p-datatable-striped" [scrollable]="true" scrollHeight="flex">
          <ng-template pTemplate="header">
            <tr>
              <th style="width:150px; background: var(--surface-ground); font-weight: 600; text-align: left;">Mã phòng *</th>
              <th style="width:250px; background: var(--surface-ground); font-weight: 600; text-align: left;">Tên phòng ban *</th>
              <th style="background: var(--surface-ground); font-weight: 600; text-align: left;">Phòng ban cha</th>
              <th style="width:100px; background: var(--surface-ground); font-weight: 600; text-align: center;">Đang hoạt động</th>
              <th style="width:100px; background: var(--surface-ground); font-weight: 600; text-align: center;">Thao tác</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-row let-editing="editing" let-ri="rowIndex">
            <tr [pEditableRow]="row" style="height: 32px;">
              <td>
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <input pInputText type="text" [(ngModel)]="row.departmentCode" class="w-full p-inputtext-sm" style="font-size: 12px;" maxlength="20" />
                  </ng-template>
                  <ng-template pTemplate="output">{{row.departmentCode}}</ng-template>
                </p-cellEditor>
              </td>
              <td>
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <input pInputText type="text" [(ngModel)]="row.departmentName" class="w-full p-inputtext-sm" style="font-size: 12px;" />
                  </ng-template>
                  <ng-template pTemplate="output">{{row.departmentName}}</ng-template>
                </p-cellEditor>
              </td>
              <td>
                <!-- Parent Dropdown -->
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <p-select 
                      [options]="parentOptions()" 
                      [(ngModel)]="row.parentId" 
                      optionLabel="departmentName" 
                      optionValue="id" 
                      [showClear]="true"
                      appendTo="body"
                      placeholder="Chọn cha..."
                      styleClass="w-full p-dropdown-sm text-xs"
                      [filter]="true" filterBy="departmentCode,departmentName">
                    </p-select>
                  </ng-template>
                  <ng-template pTemplate="output">{{ getParentName(row.parentId) }}</ng-template>
                </p-cellEditor>
              </td>
              <td class="text-center">
                <p-toggleswitch [(ngModel)]="row.isActive" styleClass="scale-75" (onChange)="!editing && saveRow(row)" />
              </td>
              <td class="text-center">
                <div class="flex align-items-center justify-content-center gap-1">
                  <button *ngIf="!editing" pButton pRipple type="button" pInitEditableRow icon="pi pi-pencil" aria-label="Chỉnh sửa" class="p-button-rounded p-button-text p-button-sm w-2rem h-2rem"></button>
                  <button *ngIf="!editing" pButton pRipple type="button" icon="pi pi-trash" aria-label="Xóa" class="p-button-rounded p-button-text p-button-danger p-button-sm w-2rem h-2rem" (click)="deleteRow(row)"></button>
                  
                  <button *ngIf="editing" pButton pRipple type="button" pSaveEditableRow icon="pi pi-check" aria-label="Lưu" class="p-button-rounded p-button-text p-button-success p-button-sm w-2rem h-2rem" (click)="saveRow(row)"></button>
                  <button *ngIf="editing" pButton pRipple type="button" pCancelEditableRow icon="pi pi-times" aria-label="Hủy" class="p-button-rounded p-button-text p-button-danger p-button-sm w-2rem h-2rem" (click)="cancelEdit(row, ri)"></button>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>
  `,
  styles: [`
    ::ng-deep .p-datatable .p-datatable-tbody > tr { height: 32px; font-size: 13px; }
    ::ng-deep .p-datatable .p-datatable-thead > tr > th { font-size: 13px; }
    ::ng-deep .p-dropdown .p-dropdown-label { font-size: 13px; padding: 0.25rem 0.5rem; }
  `]
})
export class DepartmentsPageComponent implements OnInit {
  private readonly api = inject(LookupsApiService);
  private readonly confirmService = inject(ConfirmationService);
  private readonly msgService = inject(MessageService);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<DepartmentDto[]>([]);
  readonly loading = signal(false);

  // Exclude current row forming circular parent
  readonly parentOptions = computed(() => {
    return this.items().filter(x => x.id); // For new rows, we can just show all existing
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.api.getDepartments().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.msgService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải phòng ban' });
      }
    });
  }

  getParentName(parentId?: string): string {
    if (!parentId) return '';
    const parent = this.items().find(x => x.id === parentId);
    return parent ? parent.departmentName : '';
  }

  addRow() {
    const list = this.items();
    if (list.length > 0 && list[0].id === '') return;
    this.items.set([
      { id: '', departmentCode: '', departmentName: '', parentId: undefined, level: 0, isActive: true },
      ...list
    ]);
  }

  cancelEdit(row: DepartmentDto, index: number) {
    if (!row.id) {
      const list = [...this.items()];
      list.splice(index, 1);
      this.items.set(list);
    } else {
      this.load();
    }
  }

  saveRow(row: DepartmentDto) {
    if (!row.departmentCode?.trim() || !row.departmentName?.trim()) {
      this.msgService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Mã và tên không được để trống' });
      if(!row.id) this.load();
      return;
    }
    
    // Prevent self as parent
    if (row.id && row.parentId === row.id) {
      this.msgService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể chọn chính nó làm phòng ban cha' });
      this.load();
      return;
    }

    const payload = {
      id: row.id || undefined,
      departmentCode: row.departmentCode,
      departmentName: row.departmentName,
      parentId: row.parentId || undefined,
      isActive: row.isActive
    };

    this.api.upsertDepartment(payload).subscribe({
      next: () => {
        this.msgService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã lưu phòng ban' });
        this.load();
      },
      error: () => {
        this.msgService.add({ severity: 'error', summary: 'Lỗi', detail: 'Lỗi lưu dữ liệu' });
        this.load();
      }
    });
  }

  deleteRow(row: DepartmentDto) {
    if (!row.id) return;
    this.confirmService.confirm({
      message: 'Bạn có chắc muốn xóa?',
      accept: () => {
        this.api.deleteDepartment(row.id).subscribe({
          next: () => {
            this.msgService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã xóa' });
            this.load();
          }
        });
      }
    });
  }
}
