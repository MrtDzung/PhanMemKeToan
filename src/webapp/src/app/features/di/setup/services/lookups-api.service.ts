import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../../../environments/environment';
import { ApiResponse } from '../../models/account.models';
import {
  CurrencyDto,
  UpsertCurrencyDto,
  UnitDto,
  UpsertUnitDto,
  WarehouseDto,
  UpsertWarehouseDto,
  DepartmentDto,
  UpsertDepartmentDto,
  ExpenseItemDto,
  UpsertExpenseItemDto,
} from '../../models/master-data.models';

@Injectable({ providedIn: 'root' })
export class LookupsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  // --- Currencies ---

  getCurrencies(): Observable<CurrencyDto[]> {
    return this.http
      .get<ApiResponse<CurrencyDto[]>>(`${this.base}/api/currencies`)
      .pipe(map((r) => r.data));
  }

  upsertCurrency(dto: UpsertCurrencyDto): Observable<CurrencyDto> {
    return this.http
      .post<ApiResponse<CurrencyDto>>(`${this.base}/api/currencies`, dto)
      .pipe(map((r) => r.data));
  }

  deleteCurrency(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/currencies/${id}`);
  }

  // --- Units ---

  getUnits(search?: string): Observable<UnitDto[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http
      .get<ApiResponse<UnitDto[]>>(`${this.base}/api/units`, { params })
      .pipe(map((r) => r.data));
  }

  upsertUnit(dto: UpsertUnitDto): Observable<UnitDto> {
    return this.http
      .post<ApiResponse<UnitDto>>(`${this.base}/api/units`, dto)
      .pipe(map((r) => r.data));
  }

  deleteUnit(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/units/${id}`);
  }

  // --- Warehouses ---

  getWarehouses(): Observable<WarehouseDto[]> {
    return this.http
      .get<ApiResponse<WarehouseDto[]>>(`${this.base}/api/warehouses`)
      .pipe(map((r) => r.data));
  }

  upsertWarehouse(dto: UpsertWarehouseDto): Observable<WarehouseDto> {
    return this.http
      .post<ApiResponse<WarehouseDto>>(`${this.base}/api/warehouses`, dto)
      .pipe(map((r) => r.data));
  }

  deleteWarehouse(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/warehouses/${id}`);
  }

  // --- Departments ---

  getDepartments(): Observable<DepartmentDto[]> {
    return this.http
      .get<ApiResponse<DepartmentDto[]>>(`${this.base}/api/departments`)
      .pipe(map((r) => r.data));
  }

  upsertDepartment(dto: UpsertDepartmentDto): Observable<DepartmentDto> {
    return this.http
      .post<ApiResponse<DepartmentDto>>(`${this.base}/api/departments`, dto)
      .pipe(map((r) => r.data));
  }

  deleteDepartment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/departments/${id}`);
  }

  // --- Expense Items ---

  getExpenseItems(): Observable<ExpenseItemDto[]> {
    return this.http
      .get<ApiResponse<ExpenseItemDto[]>>(`${this.base}/api/expense-items`)
      .pipe(map((r) => r.data));
  }

  upsertExpenseItem(dto: UpsertExpenseItemDto): Observable<ExpenseItemDto> {
    return this.http
      .post<ApiResponse<ExpenseItemDto>>(`${this.base}/api/expense-items`, dto)
      .pipe(map((r) => r.data));
  }

  deleteExpenseItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/expense-items/${id}`);
  }
}
