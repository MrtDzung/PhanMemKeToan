import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TabsModule } from 'primeng/tabs';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TenantService, TenantDetail, TenantUserItem, UpdateTenantPayload } from '../tenant.service';

@Component({
  selector: 'app-tenant-detail',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    TabsModule, ButtonModule, TagModule, InputTextModule,
    TableModule, DialogModule, SelectModule, CheckboxModule,
    ToastModule, ConfirmDialogModule, ProgressSpinnerModule, TooltipModule
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <div class="detail-container">

      <!-- Header -->
      <div class="detail-header">
        <div class="header-left">
          <a routerLink="/system/tenants" class="back-link">
            <i class="pi pi-arrow-left"></i> Quản lý công ty
          </a>
          @if (tenant()) {
            <h1 class="page-title">{{ tenant()!.name }}</h1>
            <code class="tenant-code">{{ tenant()!.code }}</code>
          }
        </div>
        @if (tenant()) {
          <div class="header-actions">
            <p-tag [value]="tenant()!.isActive ? 'Hoạt động' : 'Vô hiệu'"
                   [severity]="tenant()!.isActive ? 'success' : 'danger'" />
            <p-button
              [label]="tenant()!.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'"
              [icon]="tenant()!.isActive ? 'pi pi-ban' : 'pi pi-check'"
              [severity]="tenant()!.isActive ? 'warn' : 'success'"
              size="small" variant="outlined"
              [loading]="togglingStatus()"
              (onClick)="toggleStatus()" />
          </div>
        }
      </div>

      <!-- Loading -->
      @if (loading()) {
        <div class="loading-center"><p-progress-spinner strokeWidth="4" style="width:40px;height:40px" /></div>
      }

      @if (!loading() && tenant()) {
        <p-tabs [(value)]="activeTab">
          <p-tablist>
            <p-tab value="info"><i class="pi pi-info-circle"></i> Thông tin</p-tab>
            <p-tab value="users"><i class="pi pi-users"></i> Người dùng truy cập ({{ tenant()!.users.length }})</p-tab>
          </p-tablist>

          <!-- TAB: THÔNG TIN -->
          <p-tabpanels>
            <p-tabpanel value="info">
              <div class="info-grid">
                <div class="info-section">
                  <h3 class="section-title">Thông tin cơ bản</h3>
                  <div class="info-rows">
                    <div class="info-row">
                      <span class="info-label">Mã công ty</span>
                      <code class="info-value-code">{{ tenant()!.code }}</code>
                    </div>
                    <div class="info-row">
                      <span class="info-label">Tên công ty</span>
                      <span class="info-value">{{ tenant()!.name }}</span>
                    </div>
                    <div class="info-row">
                      <span class="info-label">Trạng thái</span>
                      <p-tag [value]="tenant()!.isActive ? 'Hoạt động' : 'Vô hiệu'"
                             [severity]="tenant()!.isActive ? 'success' : 'danger'" />
                    </div>
                    <div class="info-row">
                      <span class="info-label">Ngày tạo</span>
                      <span class="info-value">{{ tenant()!.createdAt | date:'dd/MM/yyyy HH:mm' }}</span>
                    </div>
                  </div>
                </div>

                <div class="info-section">
                  <h3 class="section-title">Cấu hình Database</h3>
                  <div class="info-rows">
                    <div class="info-row">
                      <span class="info-label">Chế độ DB</span>
                      <p-tag [value]="tenant()!.databaseMode === 0 ? 'Cloud Managed' : 'On-Premise'"
                             [severity]="tenant()!.databaseMode === 0 ? 'info' : 'secondary'" />
                    </div>
                    <div class="info-row">
                      <span class="info-label">Trạng thái DB</span>
                      <p-tag [value]="dbStatusLabel(tenant()!.dbStatus)"
                             [severity]="dbStatusSeverity(tenant()!.dbStatus)" />
                    </div>
                    @if (tenant()!.databaseSchemaName) {
                      <div class="info-row">
                        <span class="info-label">Schema</span>
                        <code class="info-value-code">{{ tenant()!.databaseSchemaName }}</code>
                      </div>
                    }
                    @if (tenant()!.dbHost) {
                      <div class="info-row">
                        <span class="info-label">DB Host</span>
                        <code class="info-value-code">{{ tenant()!.dbHost }}</code>
                      </div>
                    }
                    @if (tenant()!.cloudflareSubdomain) {
                      <div class="info-row">
                        <span class="info-label">Subdomain</span>
                        <code class="info-value-code">{{ tenant()!.cloudflareSubdomain }}</code>
                      </div>
                    }
                    <div class="info-row">
                      <span class="info-label">Connection String</span>
                      <span class="info-value" [style.color]="tenant()!.hasConnectionString ? 'var(--positive)' : 'var(--text-secondary)'">
                        {{ tenant()!.hasConnectionString ? '✓ Đã cấu hình' : '— Chưa cấu hình' }}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              <div class="edit-section">
                <p-button label="Chỉnh sửa thông tin" icon="pi pi-pencil" size="small" (onClick)="openEdit()" />
              </div>
            </p-tabpanel>

            <!-- TAB: NGƯỜI DÙNG -->
            <p-tabpanel value="users">
              <div class="users-header">
                <span class="users-count">{{ tenant()!.users.length }} người dùng có quyền truy cập</span>
                <p-button label="Cấp quyền truy cập" icon="pi pi-user-plus" size="small" (onClick)="openGrantDialog()" />
              </div>

              <p-table
                [value]="tenant()!.users"
                styleClass="p-datatable-sm p-datatable-gridlines"
                [tableStyle]="{'min-width':'600px'}"
                [rowHover]="true"
              >
                <ng-template pTemplate="header">
                  <tr>
                    <th>Họ tên</th>
                    <th>Email</th>
                    <th style="width:120px;text-align:center">Vai trò hiển thị</th>
                    <th style="width:90px;text-align:center">Mặc định</th>
                    <th style="width:140px;text-align:center">Ngày tham gia</th>
                    <th style="width:80px;text-align:center">Thao tác</th>
                  </tr>
                </ng-template>
                <ng-template pTemplate="body" let-u>
                  <tr>
                    <td>{{ u.fullName }}</td>
                    <td>{{ u.email }}</td>
                    <td style="text-align:center">{{ u.displayRole ?? '—' }}</td>
                    <td style="text-align:center">
                      @if (u.isDefault) {
                        <i class="pi pi-check-circle" style="color:var(--positive)"></i>
                      } @else {
                        <span style="color:var(--text-secondary)">—</span>
                      }
                    </td>
                    <td style="text-align:center;font-size:12px">{{ u.joinedAt | date:'dd/MM/yyyy' }}</td>
                    <td style="text-align:center">
                      <p-button
                        icon="pi pi-user-minus" size="small" variant="text" severity="danger"
                        pTooltip="Thu hồi quyền truy cập"
                        (onClick)="confirmRevoke(u)" />
                    </td>
                  </tr>
                </ng-template>
                <ng-template pTemplate="emptymessage">
                  <tr><td colspan="6" style="text-align:center;color:var(--text-secondary);padding:24px">
                    Chưa có người dùng nào được cấp quyền
                  </td></tr>
                </ng-template>
              </p-table>
            </p-tabpanel>
          </p-tabpanels>
        </p-tabs>
      }
    </div>

    <!-- Edit dialog -->
    <p-dialog [(visible)]="showEditDialog" header="Chỉnh sửa thông tin công ty"
              [modal]="true" [style]="{width:'500px'}">
      <div class="dialog-form">
        <div class="form-field">
          <label>Tên công ty <span class="required">*</span></label>
          <input pInputText [(ngModel)]="editForm.name" style="width:100%" maxlength="200" />
        </div>
        <div class="form-field">
          <label>Chế độ cơ sở dữ liệu</label>
          <p-select [(ngModel)]="editForm.databaseMode" [options]="dbModeOptions"
                    optionLabel="label" optionValue="value" style="width:100%" />
        </div>
        @if (editForm.databaseMode === 1) {
          <div class="form-field">
            <label>Chuỗi kết nối (đã mã hóa)</label>
            <input pInputText [(ngModel)]="editForm.connectionStringEncrypted" style="width:100%" />
          </div>
          <div class="form-field">
            <label>Host DB</label>
            <input pInputText [(ngModel)]="editForm.dbHost" placeholder="db.server.local" style="width:100%" />
          </div>
        }
        @if (editForm.databaseMode === 0) {
          <div class="form-field">
            <label>Cloudflare Subdomain</label>
            <input pInputText [(ngModel)]="editForm.cloudflareSubdomain" placeholder="tenantcode.example.com" style="width:100%" />
          </div>
        }
      </div>
      <ng-template pTemplate="footer">
        <p-button label="Hủy" severity="secondary" variant="outlined" size="small" (onClick)="showEditDialog = false" />
        <p-button label="Lưu thay đổi" size="small" [loading]="saving()" (onClick)="saveEdit()" />
      </ng-template>
    </p-dialog>

    <!-- Grant access dialog -->
    <p-dialog [(visible)]="showGrantDialog" header="Cấp quyền truy cập"
              [modal]="true" [style]="{width:'440px'}">
      <div class="dialog-form">
        <div class="form-field">
          <label>Master User ID <span class="required">*</span></label>
          <input pInputText [(ngModel)]="grantForm.masterUserId"
                 placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" style="width:100%" />
          <small class="hint">UUID của MasterUser cần cấp quyền</small>
        </div>
        <div class="form-field">
          <label>Vai trò hiển thị</label>
          <input pInputText [(ngModel)]="grantForm.displayRole" placeholder="VD: Admin, Kế toán trưởng" style="width:100%" />
        </div>
        <div class="form-field-row">
          <p-checkbox [(ngModel)]="grantForm.isDefault" [binary]="true" inputId="isDefault" />
          <label for="isDefault">Đặt làm công ty mặc định</label>
        </div>
      </div>
      <ng-template pTemplate="footer">
        <p-button label="Hủy" severity="secondary" variant="outlined" size="small" (onClick)="showGrantDialog = false" />
        <p-button label="Cấp quyền" size="small" [loading]="granting()" (onClick)="grantAccess()" />
      </ng-template>
    </p-dialog>

    <p-toast />
    <p-confirmDialog />
  `,
  styles: [`
    .detail-container { padding: var(--spacing-lg, 24px); }

    .detail-header {
      display: flex; justify-content: space-between; align-items: flex-start;
      margin-bottom: 20px;
    }
    .header-left { display: flex; flex-direction: column; gap: 4px; }
    .header-actions { display: flex; align-items: center; gap: 12px; }

    .back-link {
      display: inline-flex; align-items: center; gap: 6px;
      color: var(--primary); font-size: 13px; text-decoration: none;
      &:hover { text-decoration: underline; }
    }
    .page-title { font-size: 16px; font-weight: 600; color: var(--text-primary); margin: 0; }
    .tenant-code { font-family: monospace; background: var(--surface-ground); padding: 2px 8px; border-radius: 4px; font-size: 12px; color: var(--text-secondary); }

    .loading-center { display: flex; justify-content: center; padding: 60px; }

    .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 20px; }
    .info-section { background: var(--surface-card); border: 1px solid var(--surface-border); border-radius: 8px; padding: 16px; }
    .section-title { font-size: 13px; font-weight: 600; color: var(--text-primary); margin: 0 0 12px; }
    .info-rows { display: flex; flex-direction: column; gap: 10px; }
    .info-row { display: flex; align-items: center; gap: 12px; }
    .info-label { font-size: 12px; color: var(--text-secondary); min-width: 140px; }
    .info-value { font-size: 13px; color: var(--text-primary); }
    .info-value-code { font-family: monospace; font-size: 12px; background: var(--surface-ground); padding: 2px 6px; border-radius: 3px; }

    .edit-section { margin-top: 4px; }

    .users-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
    .users-count { font-size: 13px; color: var(--text-secondary); }

    .dialog-form { display: flex; flex-direction: column; gap: 12px; padding: 4px 0; }
    .form-field { display: flex; flex-direction: column; gap: 4px; }
    .form-field label { font-size: 13px; font-weight: 500; color: var(--text-primary); }
    .form-field-row { display: flex; align-items: center; gap: 8px; }
    .form-field-row label { font-size: 13px; color: var(--text-primary); cursor: pointer; }
    .required { color: var(--error); }
    .hint { color: var(--text-secondary); font-size: 11px; }

    @media (max-width: 768px) {
      .info-grid { grid-template-columns: 1fr; }
      .detail-header { flex-direction: column; gap: 12px; }
    }
  `]
})
export class TenantDetailComponent implements OnInit {
  private readonly svc = inject(TenantService);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  tenant = signal<TenantDetail | null>(null);
  loading = signal(false);
  saving = signal(false);
  granting = signal(false);
  togglingStatus = signal(false);

  activeTab = 'info';
  showEditDialog = false;
  showGrantDialog = false;

  editForm: {
    name: string;
    databaseMode: number;
    connectionStringEncrypted?: string | null;
    cloudflareSubdomain?: string | null;
    dbHost?: string | null;
  } = { name: '', databaseMode: 0 };

  grantForm: {
    masterUserId: string;
    displayRole: string;
    isDefault: boolean;
  } = { masterUserId: '', displayRole: '', isDefault: false };

  readonly dbModeOptions = [
    { label: 'Cloud Managed', value: 0 },
    { label: 'On-Premise', value: 1 }
  ];

  private get id(): string {
    return this.route.snapshot.paramMap.get('id')!;
  }

  ngOnInit(): void {
    this.loadDetail();
  }

  async loadDetail(): Promise<void> {
    this.loading.set(true);
    try {
      const data = await this.svc.get(this.id);
      this.tenant.set(data);
    } catch {
      this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải thông tin công ty' });
    } finally {
      this.loading.set(false);
    }
  }

  openEdit(): void {
    const t = this.tenant()!;
    this.editForm = {
      name: t.name,
      databaseMode: t.databaseMode,
      connectionStringEncrypted: null,
      cloudflareSubdomain: t.cloudflareSubdomain,
      dbHost: t.dbHost
    };
    this.showEditDialog = true;
  }

  async saveEdit(): Promise<void> {
    if (!this.editForm.name?.trim()) {
      this.messageService.add({ severity: 'warn', summary: 'Thiếu thông tin', detail: 'Vui lòng nhập tên công ty' });
      return;
    }
    this.saving.set(true);
    try {
      const payload: UpdateTenantPayload = {
        name: this.editForm.name,
        databaseMode: this.editForm.databaseMode,
        connectionStringEncrypted: this.editForm.connectionStringEncrypted ?? null,
        cloudflareSubdomain: this.editForm.cloudflareSubdomain ?? null,
        dbHost: this.editForm.dbHost ?? null
      };
      await this.svc.update(this.id, payload);
      this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Cập nhật thành công' });
      this.showEditDialog = false;
      await this.loadDetail();
    } catch {
      this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Cập nhật thất bại' });
    } finally {
      this.saving.set(false);
    }
  }

  async toggleStatus(): Promise<void> {
    const t = this.tenant()!;
    this.confirmationService.confirm({
      message: t.isActive
        ? `Vô hiệu hóa công ty "${t.name}"? Tất cả phiên đăng nhập sẽ bị thu hồi.`
        : `Kích hoạt lại công ty "${t.name}"?`,
      header: 'Xác nhận',
      icon: 'pi pi-exclamation-triangle',
      accept: async () => {
        this.togglingStatus.set(true);
        try {
          if (t.isActive) {
            await this.svc.deactivate(this.id);
          } else {
            await this.svc.activate(this.id);
          }
          this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Cập nhật trạng thái thành công' });
          await this.loadDetail();
        } catch {
          this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Thao tác thất bại' });
        } finally {
          this.togglingStatus.set(false);
        }
      }
    });
  }

  openGrantDialog(): void {
    this.grantForm = { masterUserId: '', displayRole: '', isDefault: false };
    this.showGrantDialog = true;
  }

  async grantAccess(): Promise<void> {
    if (!this.grantForm.masterUserId?.trim()) {
      this.messageService.add({ severity: 'warn', summary: 'Thiếu thông tin', detail: 'Vui lòng nhập Master User ID' });
      return;
    }
    this.granting.set(true);
    try {
      await this.svc.grantAccess(
        this.id,
        this.grantForm.masterUserId.trim(),
        this.grantForm.isDefault,
        this.grantForm.displayRole || undefined
      );
      this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Cấp quyền truy cập thành công' });
      this.showGrantDialog = false;
      await this.loadDetail();
    } catch (err: unknown) {
      const msg = (err as { error?: { message?: string } })?.error?.message ?? 'Có lỗi xảy ra';
      this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: msg });
    } finally {
      this.granting.set(false);
    }
  }

  confirmRevoke(u: TenantUserItem): void {
    this.confirmationService.confirm({
      message: `Thu hồi quyền truy cập của "${u.fullName}" (${u.email})?`,
      header: 'Xác nhận thu hồi',
      icon: 'pi pi-exclamation-triangle',
      accept: async () => {
        try {
          await this.svc.revokeAccess(this.id, u.userId);
          this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Đã thu hồi quyền truy cập' });
          await this.loadDetail();
        } catch {
          this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Thu hồi quyền thất bại' });
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
