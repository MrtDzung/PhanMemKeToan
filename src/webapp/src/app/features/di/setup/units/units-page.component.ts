import { Component, OnInit, DestroyRef, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { MessageService, ConfirmationService } from 'primeng/api';
import { LookupsApiService } from '../services/lookups-api.service';
import { UnitDto } from '../../models/master-data.models';

@Component({
  selector: 'app-units-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [MessageService, ConfirmationService],
  imports: [
    CommonModule, FormsModule, TableModule, ButtonModule,
    InputTextModule, ToggleSwitchModule, ConfirmDialogModule, ToastModule
  ],
  template: `
    <p-toast />
    <p-confirmDialog [style]="{width: '400px'}" />
    <div class="page-wrapper p-4 h-full flex flex-column bg-[var(--surface-ground)]">
      <div class="toolbar flex align-items-center justify-content-between mb-4 p-3 bg-[var(--surface-card)] border-round shadow-1">
        <p-button label="Thêm mới" icon="pi pi-plus" size="small" (onClick)="addRow()" />
      </div>
      <div class="table-container flex-1 bg-[var(--surface-card)] border-round border-1 border-solid border-[var(--surface-border)] overflow-hidden">
        <p-table [value]="items()" [loading]="loading()" dataKey="id" editMode="row" styleClass="p-datatable-sm p-datatable-gridlines p-datatable-striped" [scrollable]="true" scrollHeight="flex">
          <ng-template pTemplate="header">
            <tr>
              <th style="width:150px; background: var(--surface-ground); font-weight: 600; text-align: left;">Mã ĐVT *</th>
              <th style="background: var(--surface-ground); font-weight: 600; text-align: left;">Tên đơn vị tính *</th>
              <th style="width:100px; background: var(--surface-ground); font-weight: 600; text-align: center;">Đang dùng</th>
              <th style="width:100px; background: var(--surface-ground); font-weight: 600; text-align: center;">Thao tác</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-row let-editing="editing" let-ri="rowIndex">
            <tr [pEditableRow]="row" style="height: 32px;">
              <td>
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <input pInputText type="text" [(ngModel)]="row.unitCode" class="w-full p-inputtext-sm" style="font-size: 12px;" maxlength="20" />
                  </ng-template>
                  <ng-template pTemplate="output">{{row.unitCode}}</ng-template>
                </p-cellEditor>
              </td>
              <td>
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <input pInputText type="text" [(ngModel)]="row.unitName" class="w-full p-inputtext-sm" style="font-size: 12px;" />
                  </ng-template>
                  <ng-template pTemplate="output">{{row.unitName}}</ng-template>
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
  `]
})
export class UnitsPageComponent implements OnInit {
  private readonly api = inject(LookupsApiService);
  private readonly confirmService = inject(ConfirmationService);
  private readonly msgService = inject(MessageService);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<UnitDto[]>([]);
  readonly loading = signal(false);

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.api.getUnits().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.msgService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải danh sách ĐVT' });
      }
    });
  }

  addRow() {
    const list = this.items();
    if (list.length > 0 && list[0].id === '') return;
    this.items.set([
      { id: '', unitCode: '', unitName: '', isActive: true },
      ...list
    ]);
  }

  cancelEdit(row: UnitDto, index: number) {
    if (!row.id) {
      const list = [...this.items()];
      list.splice(index, 1);
      this.items.set(list);
    } else {
      this.load();
    }
  }

  saveRow(row: UnitDto) {
    if (!row.unitCode?.trim() || !row.unitName?.trim()) {
      this.msgService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Mã và tên không được để trống' });
      if(!row.id) this.load();
      return;
    }

    const payload = {
      id: row.id || undefined,
      unitCode: row.unitCode,
      unitName: row.unitName,
      isActive: row.isActive
    };

    this.api.upsertUnit(payload).subscribe({
      next: () => {
        this.msgService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã lưu đơn vị tính' });
        this.load();
      },
      error: () => {
        this.msgService.add({ severity: 'error', summary: 'Lỗi', detail: 'Lỗi lưu dữ liệu' });
        this.load();
      }
    });
  }

  deleteRow(row: UnitDto) {
    if (!row.id) return;
    this.confirmService.confirm({
      message: 'Bạn có chắc muốn xóa?',
      accept: () => {
        this.api.deleteUnit(row.id).subscribe({
          next: () => {
            this.msgService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã xóa' });
            this.load();
          }
        });
      }
    });
  }
}
