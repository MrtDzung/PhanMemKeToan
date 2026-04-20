import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageService } from 'primeng/api';
import { environment } from '../../../../../environments/environment';

interface RoleItem {
  id: string;
  name: string;
  description: string | null;
  userCount: number;
  permissionCount: number;
}

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, ToastModule, ProgressSpinnerModule],
  providers: [MessageService],
  template: `
    <div class="list-container">
      <div class="list-header">
        <h1 class="page-title">Quản lý vai trò</h1>
        <p-button label="Thêm vai trò" icon="pi pi-plus" size="small" (onClick)="createRole()" />
      </div>

      @if (loading()) {
        <div class="loading-center">
          <p-progressSpinner strokeWidth="4" style="width:40px;height:40px;" />
        </div>
      } @else {
        <p-table
          [value]="roles()"
          [rowHover]="true"
          styleClass="p-datatable-sm p-datatable-gridlines"
        >
          <ng-template pTemplate="header">
            <tr>
              <th>Tên vai trò</th>
              <th>Mô tả</th>
              <th style="text-align:right">Số người dùng</th>
              <th style="text-align:right">Số quyền</th>
              <th style="text-align:center">Thao tác</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-role>
            <tr>
              <td>{{ role.name }}</td>
              <td>{{ role.description ?? '—' }}</td>
              <td style="text-align:right; font-family:monospace; font-variant-numeric:tabular-nums">{{ role.userCount }}</td>
              <td style="text-align:right; font-family:monospace; font-variant-numeric:tabular-nums">{{ role.permissionCount }}</td>
              <td style="text-align:center">
                <p-button icon="pi pi-pencil" size="small" variant="text" (onClick)="editRole(role)" />
                <p-button icon="pi pi-trash" size="small" variant="text" severity="danger" (onClick)="deleteRole(role)" />
              </td>
            </tr>
          </ng-template>
          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="5" style="text-align:center;color:var(--text-secondary)">Không có dữ liệu</td>
            </tr>
          </ng-template>
        </p-table>
      }
    </div>
    <p-toast />
  `,
  styles: [`
    .list-container { padding: var(--spacing-lg, 24px); }
    .list-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
    .page-title { font-size: 16px; font-weight: 600; color: var(--text-primary); margin: 0; }
    .loading-center { display: flex; justify-content: center; padding: 40px; }
  `]
})
export class RoleListComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messageService = inject(MessageService);

  roles = signal<RoleItem[]>([]);
  loading = signal(false);

  ngOnInit(): void {
    this.fetchRoles();
  }

  async fetchRoles(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await firstValueFrom(
        this.http.get<RoleItem[]>(`${environment.apiBaseUrl}/api/roles`, { withCredentials: true })
      );
      this.roles.set(result);
    } catch {
      this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải danh sách vai trò' });
    } finally {
      this.loading.set(false);
    }
  }

  createRole(): void {
    this.messageService.add({ severity: 'info', summary: 'Thông báo', detail: 'Chức năng đang phát triển' });
  }

  editRole(role: RoleItem): void {
    this.messageService.add({ severity: 'info', summary: 'Thông báo', detail: `Chỉnh sửa: ${role.name}` });
  }

  deleteRole(role: RoleItem): void {
    this.messageService.add({ severity: 'info', summary: 'Thông báo', detail: `Xóa: ${role.name} (đang phát triển)` });
  }
}
