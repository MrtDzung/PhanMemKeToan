import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { RouterLink } from '@angular/router';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ToastModule } from 'primeng/toast';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { environment } from '../../../../../environments/environment';

interface UserItem {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
}

interface PagedResult {
  items: UserItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink, TableModule, ButtonModule, TagModule,
    InputTextModule, ProgressSpinnerModule, ToastModule, ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <div class="list-container">
      <div class="list-header">
        <h1 class="page-title">Quản lý người dùng</h1>
        <p-button
          label="Thêm người dùng"
          icon="pi pi-plus"
          size="small"
          (onClick)="createUser()"
        />
      </div>

      <div class="search-bar">
        <input
          pInputText
          type="text"
          placeholder="Tìm kiếm theo tên, email..."
          [(ngModel)]="searchQuery"
          (ngModelChange)="onSearchChange($event)"
          style="width:300px;"
        />
      </div>

      <p-table
          [value]="users()"
          [rows]="pageSize"
          [totalRecords]="totalCount()"
          [lazy]="true"
          [loading]="loading()"
          (onLazyLoad)="loadUsers($event)"
          [paginator]="true"
          [rowHover]="true"
          styleClass="p-datatable-sm p-datatable-gridlines"
          [tableStyle]="{'min-width': '700px'}"
        >
          <ng-template pTemplate="header">
            <tr>
              <th>Họ tên</th>
              <th>Email</th>
              <th>Vai trò</th>
              <th style="text-align:center">Trạng thái</th>
              <th style="text-align:center">Thao tác</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-user>
            <tr>
              <td>{{ user.fullName }}</td>
              <td>{{ user.email }}</td>
              <td>{{ user.roles.join(', ') }}</td>
              <td style="text-align:center">
                <p-tag
                  [value]="user.isActive ? 'Hoạt động' : 'Vô hiệu'"
                  [severity]="user.isActive ? 'success' : 'danger'"
                />
              </td>
              <td style="text-align:center">
                <p-button icon="pi pi-pencil" size="small" variant="text" (onClick)="editUser(user)" />
                <p-button
                  [icon]="user.isActive ? 'pi pi-ban' : 'pi pi-check'"
                  size="small"
                  variant="text"
                  [severity]="user.isActive ? 'warn' : 'success'"
                  (onClick)="toggleActivation(user)"
                />
              </td>
            </tr>
          </ng-template>
          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="5" style="text-align:center; color:var(--text-secondary)">Không có dữ liệu</td>
            </tr>
          </ng-template>
        </p-table>
    </div>

    <p-toast />
    <p-confirmDialog />
  `,
  styles: [`
    .list-container { padding: var(--spacing-lg, 24px); }
    .list-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
    .page-title { font-size: 16px; font-weight: 600; color: var(--text-primary); margin: 0; }
    .search-bar { margin-bottom: 12px; }

  `]
})
export class UserListComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  users = signal<UserItem[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  searchQuery = '';
  pageSize = 20;
  currentPage = 1;

  ngOnInit(): void {
    this.fetchUsers();
  }

  async loadUsers(event: TableLazyLoadEvent): Promise<void> {
    this.currentPage = Math.floor((event.first ?? 0) / (event.rows ?? this.pageSize)) + 1;
    await this.fetchUsers();
  }

  onSearchChange(value: string): void {
    this.searchQuery = value;
    this.currentPage = 1;
    this.fetchUsers();
  }

  async fetchUsers(): Promise<void> {
    this.loading.set(true);
    try {
      const params = new URLSearchParams({
        page: String(this.currentPage),
        pageSize: String(this.pageSize),
        ...(this.searchQuery ? { search: this.searchQuery } : {})
      });
      const result = await firstValueFrom(
        this.http.get<PagedResult>(`${environment.apiBaseUrl}/api/users?${params}`, { withCredentials: true })
      );
      this.users.set(result.items);
      this.totalCount.set(result.totalCount);
    } catch {
      this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Không thể tải danh sách người dùng' });
    } finally {
      this.loading.set(false);
    }
  }

  createUser(): void {
    this.messageService.add({ severity: 'info', summary: 'Thông báo', detail: 'Chức năng đang phát triển' });
  }

  editUser(user: UserItem): void {
    this.messageService.add({ severity: 'info', summary: 'Thông báo', detail: `Chỉnh sửa: ${user.fullName}` });
  }

  toggleActivation(user: UserItem): void {
    this.confirmationService.confirm({
      message: user.isActive
        ? `Vô hiệu hóa tài khoản "${user.fullName}"?`
        : `Kích hoạt lại tài khoản "${user.fullName}"?`,
      header: 'Xác nhận',
      icon: 'pi pi-exclamation-triangle',
      accept: async () => {
        try {
          const action = user.isActive ? 'deactivate' : 'reactivate';
          await firstValueFrom(
            this.http.post(`${environment.apiBaseUrl}/api/users/${user.id}/${action}`, {}, { withCredentials: true })
          );
          this.messageService.add({ severity: 'success', summary: 'Thành công', detail: 'Cập nhật thành công' });
          await this.fetchUsers();
        } catch {
          this.messageService.add({ severity: 'error', summary: 'Lỗi', detail: 'Cập nhật thất bại' });
        }
      }
    });
  }
}
