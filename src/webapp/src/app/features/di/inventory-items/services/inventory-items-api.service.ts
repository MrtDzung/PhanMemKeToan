import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import {
  CategoryTreeNode,
  InventoryItemListItem,
  InventoryItemDetail,
  InventoryItemFilters,
  CreateInventoryItemDto,
  UpdateInventoryItemDto,
  PaginatedResult,
} from '../../models/master-data.models';

@Injectable({ providedIn: 'root' })
export class InventoryItemsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getList(
    filters: InventoryItemFilters,
    page: number,
    pageSize: number
  ): Observable<PaginatedResult<InventoryItemListItem>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filters.itemType != null) {
      params = params.set('itemType', filters.itemType.toString());
    }
    if (filters.search != null && filters.search.trim() !== '') {
      params = params.set('search', filters.search.trim());
    }
    if (filters.isActive != null) {
      params = params.set('isActive', filters.isActive.toString());
    }
    if (filters.categoryId != null) {
      params = params.set('categoryId', filters.categoryId);
    }

    return this.http
      .get<any>(`${this.base}/api/inventory-items`, { params })
      .pipe(map((r: any) => r.data));
  }

  getById(id: string): Observable<InventoryItemDetail> {
    return this.http
      .get<any>(`${this.base}/api/inventory-items/${id}`)
      .pipe(map((r: any) => r.data));
  }

  create(dto: CreateInventoryItemDto): Observable<{ id: string; rowVersion: number }> {
    return this.http
      .post<any>(`${this.base}/api/inventory-items`, dto)
      .pipe(map((r: any) => r.data));
  }

  update(id: string, dto: UpdateInventoryItemDto): Observable<{ id: string; rowVersion: number }> {
    return this.http
      .put<any>(`${this.base}/api/inventory-items/${id}`, dto)
      .pipe(map((r: any) => r.data));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/inventory-items/${id}`);
  }

  getCategories(): Observable<CategoryTreeNode[]> {
    return this.http
      .get<any>(`${this.base}/api/inventory-item-categories`)
      .pipe(map((r: any) => r.data));
  }
}
