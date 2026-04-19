import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import {
  AccountObjectListItem,
  AccountObjectDetail,
  PaginatedResult,
} from '../../models/master-data.models';
import {
  CreateAccountObjectDto,
  UpdateAccountObjectDto,
} from '../models/account-object-request.models';

export interface AccountObjectListFilters {
  typeFilter?: number;
  status?: string;
  search?: string;
}

@Injectable()
export class AccountObjectsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/api/account-objects`;

  getList(
    filters: AccountObjectListFilters,
    page: number,
    pageSize: number
  ): Observable<PaginatedResult<AccountObjectListItem>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filters.typeFilter != null && filters.typeFilter !== 0) {
      params = params.set('typeFilter', filters.typeFilter.toString());
    }
    if (filters.status != null && filters.status !== '') {
      params = params.set('status', filters.status);
    }
    if (filters.search != null && filters.search.trim() !== '') {
      params = params.set('search', filters.search.trim());
    }

    return this.http
      .get<any>(this.base, { params })
      .pipe(map((r) => r.data));
  }

  getById(id: string): Observable<AccountObjectDetail> {
    return this.http
      .get<any>(`${this.base}/${id}`)
      .pipe(map((r) => r.data));
  }

  create(dto: CreateAccountObjectDto): Observable<AccountObjectDetail> {
    return this.http
      .post<any>(this.base, dto)
      .pipe(map((r) => r.data));
  }

  update(id: string, dto: UpdateAccountObjectDto): Observable<AccountObjectDetail> {
    return this.http
      .put<any>(`${this.base}/${id}`, dto)
      .pipe(map((r) => r.data));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
