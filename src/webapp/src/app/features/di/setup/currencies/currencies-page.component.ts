import { Component, OnInit, DestroyRef, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { MessageService, ConfirmationService } from 'primeng/api';
import { LookupsApiService } from '../services/lookups-api.service';
import { CurrencyDto } from '../../models/master-data.models';
import { NumberFormatService } from '../../../../core/services/number-format.service';

@Component({
  selector: 'app-currencies-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [MessageService, ConfirmationService],
  imports: [
    CommonModule, FormsModule, TableModule, ButtonModule,
    InputTextModule, InputNumberModule, ToggleSwitchModule,
    ConfirmDialogModule, ToastModule
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
              <th style="width:120px; background: var(--surface-ground); font-weight: 600; text-align: left;">Mã tiền tệ *</th>
              <th style="background: var(--surface-ground); font-weight: 600; text-align: left;">Tên tiền tệ *</th>
              <th style="width:200px; background: var(--surface-ground); font-weight: 600; text-align: left;">Tên tiếng Anh</th>
              <th style="width:100px; background: var(--surface-ground); font-weight: 600; text-align: left;">Ký hiệu</th>
              <th style="width:160px; background: var(--surface-ground); font-weight: 600; text-align: right;">Tỷ giá mặc định</th>
              <th style="width:100px; background: var(--surface-ground); font-weight: 600; text-align: center;">Đang dùng</th>
              <th style="width:100px; background: var(--surface-ground); font-weight: 600; text-align: center;">Thao tác</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-row let-editing="editing" let-ri="rowIndex">
            <tr [pEditableRow]="row" style="height: 32px;">
              <td>
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <input pInputText type="text" [(ngModel)]="row.currencyCode" class="w-full p-inputtext-sm" style="font-size: 12px;" maxlength="10" />
                  </ng-template>
                  <ng-template pTemplate="output">{{row.currencyCode}}</ng-template>
                </p-cellEditor>
              </td>
              <td>
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <input pInputText type="text" [(ngModel)]="row.currencyName" class="w-full p-inputtext-sm" style="font-size: 12px;" />
                  </ng-template>
                  <ng-template pTemplate="output">{{row.currencyName}}</ng-template>
                </p-cellEditor>
              </td>
              <td>
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <input pInputText type="text" [(ngModel)]="row.currencyNameEnglish" class="w-full p-inputtext-sm" style="font-size: 12px;" />
                  </ng-template>
                  <ng-template pTemplate="output">{{row.currencyNameEnglish}}</ng-template>
                </p-cellEditor>
              </td>
              <td>
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <input pInputText type="text" [(ngModel)]="row.symbol" class="w-full p-inputtext-sm" style="font-size: 12px;" maxlength="10" />
                  </ng-template>
                  <ng-template pTemplate="output">{{row.symbol}}</ng-template>
                </p-cellEditor>
              </td>
              <td class="text-right font-mono" style="font-variant-numeric: tabular-nums;">
                <p-cellEditor>
                  <ng-template pTemplate="input">
                    <p-inputNumber [(ngModel)]="row.exchangeRate" [minFractionDigits]="2" [maxFractionDigits]="2" inputStyleClass="text-right w-full p-inputtext-sm font-mono text-xs" class="w-full" />
                  </ng-template>
                  <ng-template pTemplate="output">{{formatRate(row.exchangeRate)}}</ng-template>
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
export class CurrenciesPageComponent implements OnInit {
  private readonly api = inject(LookupsApiService);
  private readonly confirmService = inject(ConfirmationService);
  private readonly msgService = inject(MessageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly numFmt = inject(NumberFormatService);

  readonly items = signal<CurrencyDto[]>([]);
  readonly loading = signal(false);

  formatRate(value: number | null | undefined): string {
    return this.numFmt.formatExchangeRate(value);
  }

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.api.getCurrencies().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.msgService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải danh sách tiền tệ' });
      }
    });
  }

  addRow() {
    const list = this.items();
    if (list.length > 0 && list[0].id === '') return;
    this.items.set([
      { id: '', currencyCode: '', currencyName: '', currencyNameEnglish: '', symbol: '', exchangeRate: 1, isActive: true },
      ...list
    ]);
  }

  cancelEdit(row: CurrencyDto, index: number) {
    if (!row.id) {
      const list = [...this.items()];
      list.splice(index, 1);
      this.items.set(list);
    } else {
      this.load();
    }
  }

  saveRow(row: CurrencyDto) {
    if (!row.currencyCode?.trim() || !row.currencyName?.trim()) {
      this.msgService.add({ severity: 'warn', summary: 'Cảnh báo', detail: 'Mã và tên không được để trống' });
      if(!row.id) this.load(); // Refresh if invalid new row, simplest handling
      return;
    }

    const payload = {
      id: row.id || undefined,
      currencyCode: row.currencyCode,
      currencyName: row.currencyName,
      currencyNameEnglish: row.currencyNameEnglish,
      symbol: row.symbol || '',
      exchangeRate: row.exchangeRate || 1,
      isActive: row.isActive
    };

    this.api.upsertCurrency(payload).subscribe({
      next: () => {
        this.msgService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã lưu tiền tệ' });
        this.load();
      },
      error: (e) => {
        this.msgService.add({ severity: 'error', summary: 'Lỗi', detail: 'Lỗi lưu dữ liệu' });
        this.load();
      }
    });
  }

  deleteRow(row: CurrencyDto) {
    if (!row.id) return;
    this.confirmService.confirm({
      message: 'Bạn có chắc muốn xóa?',
      accept: () => {
        this.api.deleteCurrency(row.id).subscribe({
          next: () => {
            this.msgService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã xóa' });
            this.load();
          }
        });
      }
    });
  }
}
