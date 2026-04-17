import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TenantService, TenantListItem, CreateTenantPayload, UpdateTenantPayload } from '../tenant.service';

@Component({
  selector: 'app-tenant-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, TableModule, ButtonModule, TagModule,
    InputTextModule, DialogModule, SelectModule, CheckboxModule,
    ToastModule, ConfirmDialogModule, TooltipModule
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <div class="list-container">
      <div class="list-header">
        <h1 class="page-title">Quản lý công ty (Tenant)</h1>
        <p-button label="Thêm công ty" icon="pi pi-plus" size="small" (onClick)="openCreate()" />
      </div>

      <div class="search-bar">
        <input
          pInputText type="text"
          placeholder="Tìm kiếm theo tên, mã..."
          [(ngModel)]="searchQuery"
          (ngModelChange)="onSearch($event)"
          style="width:300px"
        />
      </div>

      <p-table
        [value]="tenants()"
        [rows]="pageSize"
        [totalRecords]="totalCount()"
        [lazy]="true"
        [loading]="loading()"
        (onLazyLoad)="onLazyLoad($event)"
        [paginator]="true"
        [rowHover]="true"
        styleClass="p-datatable-sm p-datatable-gridlines"
        [tableStyle]="{'min-width':'800px'}"
      >
        <ng-template pTemplate="header">
          <tr>
            <th style="width:120px">Mã</th>
            <th>Tên công ty</th>
            <th style="width:130px;text-align:center">Chế độ DB</th>
            <th style="width:110px;text-align:center">Trạng thái DB</th>
            <th style="width:80px;text-align:center">Người dùng</th>
            <th style="width:110px;text-align:center">Hoạt động</th>
              <th style="width:160px;text-align:center">Thao tác</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-t>
          <tr>
            <td><code>{{ t.code }}</code></td>
            <td>{{ t.name }}</td>
            <td style="text-align:center">
              <p-tag [value]="t.databaseMode === 0 ? 'Cloud' : 'On-Premise'"
                     [severity]="t.databaseMode === 0 ? 'info' : 'secondary'" />
            </td>
            <td style="text-align:center">
              <p-tag [value]="dbStatusLabel(t.dbStatus)"
                     [severity]="dbStatusSeverity(t.dbStatus)" />
            </td>
            <td style="text-align:center;font-family:monospace">{{ t.userCount }}</td>
            <td style="text-align:center">
              <p-tag [value]="t.isActive ? 'Hoạt động' : 'Vô hiệu'"
                     [severity]="t.isActive ? 'success' : 'danger'" />
            </td>
            <td style="text-align:center">
              <p-button icon="pi pi-eye" size="small" variant="text" pTooltip="Xem chi tiết"
                        (onClick)="viewDetail(t)" />
              <p-button icon="pi pi-pencil" size="small" variant="text" pTooltip="Chỉnh sửa"
                        (onClick)="openEdit(t)" />
              <p-button
                [icon]="t.isActive ? 'pi pi-ban' : 'pi pi-check'"
                size="small" variant="text"
                [severity]="t.isActive ? 'warn' : 'success'"
                [pTooltip]="t.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'"
                (onClick)="toggleStatus(t)" />
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr><td colspan="7" style="text-align:center;color:var(--text-secondary)">Không có dữ liệu</td></tr>
        </ng-template>
      </p-table>
    </div>

    <!-- Create / Edit dialog -->
    <p-dialog
      [(visible)]="showDialog"
      [header]="isEdit ? 'Chỉnh sửa công ty' : 'Thêm công ty mới'"
      [modal]="true"
      [style]="{width:'520px'}"
      [closable]="true"
    >
      <div class="dialog-form">
        @if (!isEdit) {
          <div class="form-field">
            <label>Mã công ty <span class="required">*</span></label>
            <input pInputText [(ngModel)]="form.code" placeholder="VD: CONG_TY_ABC"
                   style="width:100%;text-transform:uppercase" maxlength="50" />
            <small class="hint">Chỉ chứa A-Z, 0-9, dấu gạch dưới. Không thể thay đổi sau khi tạo.</small>
          </div>
        }
        <div class="form-field">
          <label>Tên công ty <span class="required">*</span></label>
          <input pInputText [(ngModel)]="form.name" placeholder="Tên đầy đủ của công ty" style="width:100%" maxlength="200" />
        </div>
        <div class="form-field">
          <label>Chế độ cơ sở dữ liệu</label>
          <p-select
            [(ngModel)]="form.databaseMode"
            [options]="dbModeOptions"
            optionLabel="label" optionValue="value"
            style="width:100%"
          />
        </div>
        @if (form.databaseMode === 1) {
          <div class="form-field">
            <label>Chuỗi kết nối (đã mã hóa)</label>
            <input pInputText [(ngModel)]="form.connectionStringEncrypted" placeholder="Encrypted connection string" style="width:100%" />
          </div>
          <div class="form-field">
            <label>Host DB</label>
            <input pInputText [(ngModel)]="form.dbHost" placeholder="db.server.local" style="width:100%" />
          </div>
        }
        @if (form.databaseMode === 0) {
          <div class="form-field">
            <label>Cloudflare Subdomain</label>
            <input pInputText [(ngModel)]="form.cloudflareSubdomain" placeholder="tenantcode.example.com" style="width:100%" />
          </div>
        }
      </div>
      <ng-template pTemplate="footer">
        <p-button label="Hủy" severity="secondary" variant="outlined" size="small" (onClick)="closeDialog()" />
        <p-button [label]="isEdit ? 'Lưu thay đổi' : 'Tạo công ty'" size="small"
                  [loading]="saving()" (onClick)="save()" />
      </ng-template>
    </p-dialog>

    <p-toast />
    <p-confirmDialog />
  `,
  styles: [`
    .list-container { padding: var(--spacing-lg, 24px); }
    .list-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:16px; }
    .page-title { font-size:16px; font-weight:600; color:var(--text-primary); margin:0; }
    .search-bar { margin-bottom:12px; }
    .dialog-form { display:flex; flex-direction:column; gap:12px; padding:4px 0; }
    .form-field { display:flex; flex-direction:column; gap:4px; }
    .form-field label { font-size:13px; font-weight:500; color:var(--text-primary); }
    .required { color:var(--error); }
    .hint { color:var(--text-secondary); font-size:11px; }
    code { font-family:monospace; background:var(--surface-ground); padding:2px 6px; border-radius:3px; font-size:12px; }
  `]
})
export class TenantListComponent implements OnInit {
  private readonly svc = inject(TenantService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  tenants = signal<TenantListItem[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  saving = signal(false);

  searchQuery = '';
  pageSize = 20;
  currentPage = 1;
  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  showDialog = false;
  isEdit = false;
  editId: string | null = null;

  form: {
    code: string;
    name: string;
    databaseMode: number;
    connectionStringEncrypted?: string;
    cloudflareSubdomain?: string;
    dbHost?: string;
  } = { code: '', name: '', databaseMode: 0 };

  readonly dbModeOptions = [
    { label: 'Cloud Managed', value: 0 },
    { label: 'On-Premise', value: 1 }
  ];

  ngOnInit(): void {
    this.fetchTenants();
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    this.currentPage = Math.floor((event.first ?? 0) / (event.rows ?? this.pageSize)) + 1;
    this.fetchTenants();
  }

  onSearch(value: string): void {
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => {
      this.searchQuery = value;
      this.currentPage = 1;
      this.fetchTenants();
    }, 300);
  }

  async fetchTenants(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await this.svc.list(this.searchQuery || undefined, this.currentPage, this.pageSize);
      this.tenants.set(result.items);
      this.totalCount.set(result.totalCount);
    } catch {
      this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải danh sách công ty' });
    } finally {
      this.loading.set(false);
    }
  }

  viewDetail(t: TenantListItem): void {
    this.router.navigate(['/system/tenants', t.id]);
  }

  openCreate(): void {
    this.isEdit = false;
    this.editId = null;
    this.form = { code: '', name: '', databaseMode: 0 };
    this.showDialog = true;
  }

  openEdit(t: TenantListItem): void {
    this.isEdit = true;
    this.editId = t.id;
    this.form = { code: t.code, name: t.name, databaseMode: t.databaseMode };
    this.showDialog = true;
  }

  closeDialog(): void {
    this.showDialog = false;
  }

  async save(): Promise<void> {
    if (!this.form.name?.trim() || (!this.isEdit && !this.form.code?.trim())) {
      this.messageService.add({ severity: 'warn', summary: 'Thiếu thông tin', detail: 'Vui lòng điền đầy đủ thông tin bắt buộc' });
      return;
    }
    this.saving.set(true);
    try {
      if (this.isEdit && this.editId) {
        const payload: UpdateTenantPayload = {
          name: this.form.name,
          databaseMode: this.form.databaseMode,
          connectionStringEncrypted: this.form.connectionStringEncrypted ?? null,
          cloudflareSubdomain: this.form.cloudflareSubdomain ?? null,
          dbHost: this.form.dbHost ?? null
        };
        await this.svc.update(this.editId, payload);
        this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Cập nhật công ty thành công' });
      } else {
        const payload: CreateTenantPayload = {
          code: this.form.code.toUpperCase(),
          name: this.form.name,
          databaseMode: this.form.databaseMode,
          ...(this.form.connectionStringEncrypted ? { connectionStringEncrypted: this.form.connectionStringEncrypted } : {}),
          ...(this.form.cloudflareSubdomain ? { cloudflareSubdomain: this.form.cloudflareSubdomain } : {}),
          ...(this.form.dbHost ? { dbHost: this.form.dbHost } : {})
        };
        await this.svc.create(payload);
        this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Tạo công ty thành công' });
      }
      this.showDialog = false;
      await this.fetchTenants();
    } catch (err: unknown) {
      const msg = (err as { error?: { message?: string } })?.error?.message ?? 'Có lỗi xảy ra';
      this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: msg });
    } finally {
      this.saving.set(false);
    }
  }

  toggleStatus(t: TenantListItem): void {
    const action = t.isActive ? 'vô hiệu hóa' : 'kích hoạt';
    this.confirmationService.confirm({
      message: `Bạn có chắc muốn ${action} công ty "${t.name}"?`,
      header: 'Xác nhận',
      icon: 'pi pi-exclamation-triangle',
      accept: async () => {
        try {
          if (t.isActive) {
            await this.svc.deactivate(t.id);
          } else {
            await this.svc.activate(t.id);
          }
          this.messageService.add({ severity: 'success', summary: 'Thành công', detail: `Đã ${action} công ty` });
          await this.fetchTenants();
        } catch {
          this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: `Không thể ${action} công ty` });
        }
      }
    });
  }

  dbStatusLabel(status: number): string {
    switch (status) {
      case 0: return 'Online';
      case 1: return 'Offline';
      case 2: return 'Migrating';
      default: return 'Unknown';
    }
  }

  dbStatusSeverity(status: number): 'success' | 'danger' | 'warn' | 'info' | 'secondary' {
    switch (status) {
      case 0: return 'success';
      case 1: return 'danger';
      case 2: return 'warn';
      default: return 'secondary';
    }
  }
}
