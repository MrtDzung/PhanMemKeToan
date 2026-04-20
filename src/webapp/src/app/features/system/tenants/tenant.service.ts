import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TenantListItem {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  databaseMode: number;
  dbStatus: number;
  userCount: number;
  createdAt: string;
}

export interface TenantDetail {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  databaseMode: number;
  dbStatus: number;
  connectionStringEncrypted: string | null;
  cloudflareSubdomain: string | null;
  dbHost: string | null;
  databaseSchemaName: string | null;
  hasConnectionString: boolean;
  createdAt: string;
  users: TenantUserItem[];
}

export interface TenantUserItem {
  userId: string;
  email: string;
  fullName: string;
  isDefault: boolean;
  displayRole: string | null;
  joinedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CreateTenantPayload {
  code: string;
  name: string;
  databaseMode: number;
  connectionStringEncrypted?: string;
  cloudflareSubdomain?: string;
  dbHost?: string;
}

export interface UpdateTenantPayload {
  name: string;
  databaseMode: number;
  connectionStringEncrypted?: string | null;
  cloudflareSubdomain?: string | null;
  dbHost?: string | null;
}

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/tenants`;
  private readonly opts = { withCredentials: true };

  list(search?: string, page = 1, pageSize = 20): Promise<PagedResult<TenantListItem>> {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (search) params.set('search', search);
    return firstValueFrom(this.http.get<PagedResult<TenantListItem>>(`${this.base}?${params}`, this.opts));
  }

  get(id: string): Promise<TenantDetail> {
    return firstValueFrom(this.http.get<TenantDetail>(`${this.base}/${id}`, this.opts));
  }

  create(payload: CreateTenantPayload): Promise<{ id: string }> {
    return firstValueFrom(this.http.post<{ id: string }>(this.base, payload, this.opts));
  }

  update(id: string, payload: UpdateTenantPayload): Promise<void> {
    return firstValueFrom(this.http.put<void>(`${this.base}/${id}`, payload, this.opts));
  }

  activate(id: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/${id}/activate`, {}, this.opts));
  }

  deactivate(id: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/${id}/deactivate`, {}, this.opts));
  }

  grantAccess(tenantId: string, masterUserId: string, isDefault: boolean, displayRole?: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/${tenantId}/users`, { masterUserId, isDefault, displayRole }, this.opts));
  }

  revokeAccess(tenantId: string, userId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/${tenantId}/users/${userId}`, this.opts));
  }
}
