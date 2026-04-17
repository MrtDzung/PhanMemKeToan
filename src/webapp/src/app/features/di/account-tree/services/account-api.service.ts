import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import {
  AccountTreeNodeDto,
  AccountDetailDto,
  AccountListItemDto,
  ImportCoaResultDto,
  CreateAccountCommand,
  UpdateAccountCommand,
  ApiResponse,
} from '../../models/account.models';

@Injectable({ providedIn: 'root' })
export class AccountApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/accounts`;

  getAccountTree(
    includeInactive = false,
    format: 'tree' | 'flat' = 'flat'
  ): Observable<ApiResponse<AccountTreeNodeDto[]>> {
    const params = new HttpParams()
      .set('includeInactive', includeInactive)
      .set('format', format);
    return this.http
      .get<ApiResponse<AccountTreeNodeDto[]>>(this.baseUrl, { params })
      .pipe(catchError((err) => throwError(() => err)));
  }

  getAccountById(id: string): Observable<ApiResponse<AccountDetailDto>> {
    return this.http
      .get<ApiResponse<AccountDetailDto>>(`${this.baseUrl}/${id}`)
      .pipe(catchError((err) => throwError(() => err)));
  }

  search(
    q: string,
    postableOnly = true,
    limit = 20
  ): Observable<ApiResponse<AccountListItemDto[]>> {
    const params = new HttpParams()
      .set('q', q)
      .set('postableOnly', postableOnly)
      .set('limit', limit);
    return this.http
      .get<ApiResponse<AccountListItemDto[]>>(`${this.baseUrl}/search`, { params })
      .pipe(catchError((err) => throwError(() => err)));
  }

  createAccount(dto: CreateAccountCommand): Observable<ApiResponse<{ id: string; rowVersion: number }>> {
    return this.http
      .post<ApiResponse<{ id: string; rowVersion: number }>>(this.baseUrl, dto)
      .pipe(catchError((err) => throwError(() => err)));
  }

  updateAccount(
    id: string,
    dto: UpdateAccountCommand
  ): Observable<ApiResponse<void>> {
    return this.http
      .put<ApiResponse<void>>(`${this.baseUrl}/${id}`, dto)
      .pipe(catchError((err) => throwError(() => err)));
  }

  deleteAccount(id: string, rowVersion: number): Observable<void> {
    const params = new HttpParams().set('rowVersion', rowVersion);
    return this.http
      .delete<void>(`${this.baseUrl}/${id}`, { params })
      .pipe(catchError((err) => throwError(() => err)));
  }

  importCoa(
    standard: 'TT99' | 'TT133',
    conflictResolution: 'skip' | 'overwrite'
  ): Observable<ApiResponse<ImportCoaResultDto>> {
    return this.http
      .post<ApiResponse<ImportCoaResultDto>>(`${this.baseUrl}/import`, {
        standard,
        conflictResolution,
      })
      .pipe(catchError((err) => throwError(() => err)));
  }
}
